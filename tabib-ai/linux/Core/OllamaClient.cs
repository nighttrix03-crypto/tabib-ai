using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TabibAI.Linux.Core;

public sealed record ChatTurn(string Role, string Content, List<string> Images);

/// <summary>
/// عميل Ollama المحلي — نفس النقاط والمعاملات المستخدمة في نسخة ويندوز
/// (Program.cs: /api/chat مع temperature 0.15 وnum_ctx 8192 وkeep_alive 10m، و/api/tags، و/api/pull).
/// </summary>
public sealed class OllamaClient
{
    public const string DefaultUrl = "http://localhost:11434";
    public const string MedicalModel = "medgemma1.5:latest";

    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(30) };

    public string Url { get; }

    // العمود الفقري لنظام اختيار النماذج: اسم النموذج الذي سيتم استخدامه حاليًا
    // يستمده من ModelManager في البداية؛ يمكن تجاوزه لاحقًا.
    private string currentModel = MedicalModel;

    public OllamaClient(string? url = null) => Url = (url ?? Environment.GetEnvironmentVariable("TABIB_OLLAMA_URL") ?? DefaultUrl).TrimEnd('/');

    /// <summary>
    /// يحدد اسم النموذج الحالي.
    /// </summary>
    public string GetCurrentModel() => currentModel;

    /// <summary>
    /// يحدد اسم النموذج (مفاتيح أمثال "medgemma1.5"، "gemma:2b"، إلخ). يقوم بتحويله إلى الإصدار الآمن عبر ModelManager.GetSafeModelName.
    /// </summary>
    public bool SetModel(string modelKey)
    {
        try
        {
            currentModel = ModelManager.GetSafeModelName(modelKey);
            return true;
        }
        catch
        {
            currentModel = MedicalModel;
            return false;
        }
    }

    /// <summary>يتحقق من وجود النموذج الحالي في Ollama عبر /api/tags.</summary>
    public async Task<bool> IsModelReadyAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync(Url + "/api/tags", ct);
            if (!response.IsSuccessStatusCode) return false;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("models").EnumerateArray().Any(m =>
                m.GetProperty("name").GetString()?.StartsWith(currentModel, StringComparison.OrdinalIgnoreCase) == true);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>يرسل المحادثة ويعيد نص الرد باستخدام النموذج الحالي. يرمي استثناءً برسالة مختصرة عند الفشل.</summary>
    public async Task<string> ChatAsync(IReadOnlyList<ChatTurn> turns, CancellationToken ct = default)
    {
        var messages = new List<Dictionary<string, object?>>();
        foreach (var turn in turns)
        {
            var message = new Dictionary<string, object?> { ["role"] = turn.Role, ["content"] = turn.Content };
            if (turn.Images.Count > 0) message["images"] = turn.Images;
            messages.Add(message);
        }
        var body = new Dictionary<string, object?>
        {
            ["model"] = currentModel,
            ["messages"] = messages,
            ["stream"] = false,
            ["keep_alive"] = "10m",
            ["options"] = new Dictionary<string, object?> { ["temperature"] = 0.15, ["num_ctx"] = 8192 }
        };
        using var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync(Url + "/api/chat", content, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {SafetyRules.Shorten(raw)}");
        using var json = JsonDocument.Parse(raw);
        return json.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "لم يصل نص في الرد.";
    }

    /// <summary>ينزّل النموذج من Ollama مع إبلاغ التقدم كنص عربي جاهز للعرض.</summary>
    public async Task PullAsync(IProgress<string> progress, CancellationToken ct = default)
    {
        var body = new Dictionary<string, object?> { ["name"] = currentModel, ["stream"] = true };
        using var request = new HttpRequestMessage(HttpMethod.Post, Url + "/api/pull")
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(SafetyRules.Shorten(await response.Content.ReadAsStringAsync(ct)));
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            try
            {
                using var evt = JsonDocument.Parse(line);
                var root = evt.RootElement;
                string state = root.TryGetProperty("status", out var status) ? status.GetString() ?? "جارٍ التنزيل" : "جارٍ التنزيل";
                if (root.TryGetProperty("total", out var total) && total.TryGetInt64(out long all) && all > 0
                    && root.TryGetProperty("completed", out var completed) && completed.TryGetInt64(out long done))
                    progress.Report($"⏳ {state} · {Math.Clamp(done * 100 / all, 0, 100)}%");
                else
                    progress.Report("⏳ " + state);
            }
            catch (JsonException) { }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
}
