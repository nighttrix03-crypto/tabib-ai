using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TabibAI.Linux.Core.Llama;

public class ModelManager
{
    private readonly string _modelsDir;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromHours(2) };

    public ModelManager()
    {
        _modelsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TabibAI", "Models");
        Directory.CreateDirectory(_modelsDir);
    }

    public async Task<string> EnsureModelAsync(ModelType type, IProgress<(int percent, string status)>? progress = null, CancellationToken ct = default)
    {
        var (fileName, url, sizeGB) = type switch
        {
            ModelType.Gemma2_2B => ("gemma-2-2b-it-Q4_K_M.gguf", 
                "https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf", 1.6),
            ModelType.Meditron7B => ("meditron-7b-Q4_K_M.gguf",
                "https://huggingface.co/mlabonne/Meditron-7B-GGUF/resolve/main/meditron-7b-Q4_K_M.gguf", 4.5),
            ModelType.Llama3_8B => ("llama-3-8b-instruct-Q4_K_M.gguf",
                "https://huggingface.co/bartowski/Meta-Llama-3-8B-Instruct-GGUF/resolve/main/Meta-Llama-3-8B-Instruct-Q4_K_M.gguf", 4.8),
            _ => throw new ArgumentException("Unknown model type")
        };

        var path = Path.Combine(_modelsDir, fileName);
        if (File.Exists(path) && new FileInfo(path).Length > sizeGB * 1024 * 1024 * 1024 * 0.9)
            return path;

        progress?.Report((0, $"Downloading {fileName}..."));
        
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? -1L;
        var read = 0L;
        var buffer = new byte[81920];

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var count = await stream.ReadAsync(buffer, ct);
            if (count == 0) break;
            await fs.WriteAsync(buffer.AsMemory(0, count), ct);
            read += count;
            if (total > 0)
                progress?.Report(((int)(read * 100 / total), $"Downloading: {read / 1024 / 1024} / {total / 1024 / 1024} MB"));
        }

        progress?.Report((100, "Download complete"));
        return path;
    }

    public ModelType RecommendModel()
    {
        var ramGB = GetTotalRAM_GB();
        var vramGB = GetVRAM_GB();
        
        if (vramGB >= 8 || ramGB >= 16) return ModelType.Meditron7B;
        if (ramGB >= 8) return ModelType.Gemma2_2B;
        return ModelType.Gemma2_2B; // fallback
    }

    private static long GetTotalRAM_GB()
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                var info = File.ReadAllText("/proc/meminfo");
                var line = info.Split('\n').First(l => l.StartsWith("MemTotal:"));
                var kb = long.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
                return kb / 1024 / 1024;
            }
            return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024 / 1024;
        }
        catch { return 8; }
    }

    private static long GetVRAM_GB()
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                var output = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=memory.total --format=csv,noheader,nounits",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                })?.StandardOutput.ReadToEnd();
                if (long.TryParse(output?.Trim(), out var mb)) return mb / 1024;
            }
        }
        catch { }
        return 0;
    }
}

public enum ModelType { Gemma2_2B, Meditron7B, Llama3_8B }
