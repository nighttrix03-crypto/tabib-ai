using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LlamaCpp;
using LlamaCpp.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TabibAI.Linux.Core.Llama;

/// <summary>
/// محرك الاستدلال الحقيقي المبني على llama.cpp عبر حزمة LlamaCpp.
/// يدعم التحميل من ملفات GGUF والبث المباشر للـ tokens.
/// </summary>
public class LlamaEngine : ILlamaEngine
{
    private readonly ILogger<LlamaEngine>? _logger;
    private LLamaWeights? _weights;
    private StatelessExecutor? _executor;
    private ModelParams? _modelParams;
    private InferenceParams? _inferenceParams;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string _modelPath = "";
    private int _contextSize = 4096;
    private int _gpuLayers = -1;
    private volatile bool _initialized;
    private volatile bool _disposed;
    private double _tokensPerSecond;

    public bool IsInitialized => _initialized && !_disposed;
    public string CurrentModel => string.IsNullOrEmpty(_modelPath) ? "" : Path.GetFileName(_modelPath);

    public LlamaEngine(ILogger<LlamaEngine>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// يحمّل نموذج GGUF من القرص ويجهّز سياق الاستدلال.
    /// </summary>
    public async Task InitializeAsync(string modelPath, int contextSize = 4096, int gpuLayers = -1, CancellationToken ct = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(LlamaEngine));

        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"ملف النموذج غير موجود: {modelPath}", modelPath);

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            DisposeCore();

            _modelPath = modelPath;
            _contextSize = contextSize;
            _gpuLayers = gpuLayers;

            var mp = new ModelParams(modelPath)
            {
                ContextSize = (uint)contextSize,
                // في llama.cpp: قيمة كبيرة تعني إطفاء كل الطبقات على GPU، و0 تعني CPU فقط
                GpuLayerCount = gpuLayers >= 0 ? gpuLayers : 999,
                UseMemorymap = true,
                Threads = (uint)Math.Max(1, Environment.ProcessorCount),
                BatchSize = 512,
            };

            _logger?.LogInformation("جارٍ تحميل النموذج: {Path} (context={Ctx}, gpu={Gpu})", modelPath, contextSize, gpuLayers);
            var sw = Stopwatch.StartNew();

            // تحميل الأوزان على خيط خلفي (عملية ثقيلة قد تستغرق ثوانٍ)
            _weights = await Task.Run(() => LLamaWeights.LoadFromFile(mp), ct).ConfigureAwait(false);
            _executor = new StatelessExecutor(_weights, mp, NullLogger.Instance);

            sw.Stop();
            _modelParams = mp;
            _initialized = true;

            _logger?.LogInformation("تم تحميل النموذج في {Ms} مللي ثانية: {Model}",
                sw.ElapsedMilliseconds, Path.GetFileName(modelPath));
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// يستدلك بنموذج مُحمّل ويعيد قطع النص (tokens) تدريجياً (بث مباشر).
    /// </summary>
    public async IAsyncEnumerable<string> StreamInferenceAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!IsInitialized)
        {
            yield return "⚠️ النموذج غير جاهز بعد. جارٍ تحميل النموذج، أعد المحاولة بعد قليل.";
            yield break;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        var sw = Stopwatch.StartNew();
        long produced = 0;
        try
        {
            var executor = _executor!;
            var inferenceParams = GetOrCreateInferenceParams();
            var channel = Channel.CreateUnbounded<string>();

            // الاستدلال على خيط خلفي حتى لا يتجمد واجهة المستخدم
            _ = Task.Run(() =>
            {
                try
                {
                    foreach (var piece in executor.Infer(prompt, inferenceParams, ct))
                    {
                        if (!string.IsNullOrEmpty(piece))
                            channel.Writer.TryWrite(piece);
                    }
                }
                catch (OperationCanceledException) { /* تم الإيقاف بواسطة المستخدم */ }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "فشل الاستدلال");
                    channel.Writer.TryWrite($"\n⚠️ خطأ أثناء التوليد: {ex.Message}");
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }, CancellationToken.None);

            await foreach (var piece in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                produced++;
                yield return piece;
            }
        }
        finally
        {
            sw.Stop();
            if (produced > 0 && sw.Elapsed.TotalSeconds > 0.01)
                _tokensPerSecond = produced / sw.Elapsed.TotalSeconds;
            _gate.Release();
        }
    }

    /// <summary>
    /// يستدلك ويعيد النص الكامل دفعة واحدة.
    /// </summary>
    public async Task<string> InferAsync(string prompt, CancellationToken ct = default)
    {
        var sb = new System.Text.StringBuilder();
        await foreach (var piece in StreamInferenceAsync(prompt, ct).ConfigureAwait(false))
            sb.Append(piece);
        return sb.ToString();
    }

    public ModelInfo GetModelInfo()
    {
        long sizeBytes = 0;
        try
        {
            if (!string.IsNullOrEmpty(_modelPath) && File.Exists(_modelPath))
                sizeBytes = new FileInfo(_modelPath).Length;
        }
        catch { /* تجاهل */ }

        return new ModelInfo(
            CurrentModel,
            _modelPath,
            sizeBytes,
            DetectQuantization(_modelPath, _weights),
            _contextSize,
            _gpuLayers,
            Math.Round(_tokensPerSecond, 1));
    }

    /// <summary>
    /// يسمح بتجاوز إعدادات الاستدلال (درجة الحرارة... إلخ).
    /// </summary>
    public void SetInferenceParams(InferenceParams inferenceParams)
    {
        _inferenceParams = inferenceParams ?? throw new ArgumentNullException(nameof(inferenceParams));
    }

    private InferenceParams GetOrCreateInferenceParams()
    {
        if (_inferenceParams != null)
            return _inferenceParams;

        _inferenceParams = new InferenceParams
        {
            MaxTokens = 700,
            Temperature = 0.7f,
            TopP = 0.9f,
            TopK = 40,
            MinP = 0.05f,
            RepeatPenalty = 1.1f,
            RepeatLastTokensCount = 64,
            // إيقاف التوليد عند بدء النموذج جولة جديدة
            AntiPrompts = new[]
            {
                "### User:", "### User", "### المريض:", "المريض:",
                "<|user|>", "Human:"
            },
        };
        return _inferenceParams;
    }

    private static string DetectQuantization(string path, LLamaWeights? weights)
    {
        // محاولة قراءة الترميز من بيانات النموذج أولاً
        try
        {
            if (weights?.Metadata != null)
            {
                if (weights.Metadata.TryGetValue("general.quantization", out var q) && !string.IsNullOrEmpty(q))
                    return q.ToUpperInvariant();
            }
        }
        catch { /* تجاهل */ }

        // الاستدلال من اسم الملف
        var name = (path ?? "").ToLowerInvariant();
        if (name.Contains("q4_k_m")) return "Q4_K_M";
        if (name.Contains("q4_k_s")) return "Q4_K_S";
        if (name.Contains("q5_k_m")) return "Q5_K_M";
        if (name.Contains("q5_k_s")) return "Q5_K_S";
        if (name.Contains("q6_k")) return "Q6_K";
        if (name.Contains("q8_0")) return "Q8_0";
        if (name.Contains("f16")) return "F16";
        if (name.Contains("f32")) return "F32";
        return "Unknown";
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        try { DisposeCore(); } catch { /* تجاهل */ }
        try { _gate.Dispose(); } catch { /* تجاهل */ }
        GC.SuppressFinalize(this);
    }

    private void DisposeCore()
    {
        _initialized = false;
        try
        {
            // StatelessExecutor يمتلك سياقه الداخلي
            if (_executor is IDisposable disposableExecutor)
                disposableExecutor.Dispose();
            _executor = null;
            _weights?.Dispose();
            _weights = null;
            _modelParams = null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "خطأ أثناء تفريغ الموارد");
        }
    }

    ~LlamaEngine()
    {
        try { DisposeCore(); } catch { /* تجاهل */ }
    }
}
