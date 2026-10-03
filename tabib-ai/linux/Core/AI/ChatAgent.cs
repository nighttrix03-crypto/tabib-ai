using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TabibAI.Linux.Core.Llama;
using TabibAI.Linux.Core.Models;

namespace TabibAI.Linux.Core.AI;

/// <summary>
/// وكيل محادثة عام متعدد الأوضاع: محادثة حرة، طبية، برمجة، ترجمة.
/// يبني الـ prompt حسب الوضع ويبث الرد الحقيقي من النموذج.
/// </summary>
public interface IChatAgent
{
    /// <summary>يبث رد النموذج تدريجياً (tokens) حسب الوضع المحدد.</summary>
    IAsyncEnumerable<string> StreamAsync(string userInput, IReadOnlyList<ChatMessage> history, ChatMode mode, CancellationToken ct = default);

    /// <summary>يجلب الرد الكامل دفعة واحدة.</summary>
    Task<string> ChatAsync(string userInput, IReadOnlyList<ChatMessage> history, ChatMode mode, CancellationToken ct = default);

    /// <summary>يحوّل رد النموذج الوضعي الطبي (JSON) إلى نص منسق.</summary>
    string FormatMedicalResponse(string raw);

    /// <summary>يحاول تحليل JSON طبي؛ يعيد null إذا لم يكن صالحاً.</summary>
    MedicalResponse? TryParseMedical(string raw);

    /// <summary>نص المساعدة بالأوامر.</summary>
    string GetHelpText();
}

public class ChatAgent : IChatAgent
{
    private readonly ILlamaEngine _engine;
    private readonly IMedicalAgent _medicalAgent;

    public ChatAgent(ILlamaEngine engine, IMedicalAgent medicalAgent)
    {
        _engine = engine;
        _medicalAgent = medicalAgent;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string userInput,
        IReadOnlyList<ChatMessage> history,
        ChatMode mode,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var prompt = BuildPrompt(userInput, history, mode);
        await foreach (var token in _engine.StreamInferenceAsync(prompt, ct))
            yield return token;
    }

    public async Task<string> ChatAsync(string userInput, IReadOnlyList<ChatMessage> history, ChatMode mode, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(userInput, history, mode);
        return await _engine.InferAsync(prompt, ct);
    }

    // ------------------------------------------------------------------
    // بناء الـ prompt حسب الوضع
    // ------------------------------------------------------------------
    private string BuildPrompt(string userInput, IReadOnlyList<ChatMessage> history, ChatMode mode)
    {
        return mode switch
        {
            ChatMode.Medical => BuildMedicalPrompt(userInput, history),
            ChatMode.Code => BuildCodePrompt(userInput, history),
            ChatMode.Translate => BuildTranslatePrompt(userInput, history),
            _ => BuildGeneralPrompt(userInput, history),
        };
    }

    private static string BuildGeneralPrompt(string userInput, IReadOnlyList<ChatMessage> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine("أنت مساعد ذكاء اصطناعي عام اسمك 'طبيب AI'. أجب بدقة ومباشرة وبلا حشو.");
        sb.AppendLine("- إذا كتب المستخدم بالعربية أجب بالعربية الفصحى المبسطة، وإذا كتب بلغة أخرى أجب بلغته.");
        sb.AppendLine("- لا تخترع حقائق؛ إذا لا تعرف الجواب قل ذلك بصراحة.");
        sb.AppendLine("- اختم إجابتك باقتراح سؤال متعلق يمكن للمستخدم سؤاله بعد ذلك.");
        sb.AppendLine();
        sb.AppendLine("### المحادثة:");

        foreach (var msg in history.TakeLast(10))
        {
            if (string.IsNullOrWhiteSpace(msg.Content)) continue;
            sb.AppendLine(msg.IsUser
                ? $"أنت: {msg.Content.Trim()}"
                : $"المساعد: {msg.Content.Trim()}");
        }

        sb.AppendLine($"أنت: {userInput.Trim()}");
        sb.Append("المساعد:");
        return sb.ToString();
    }

    private string BuildMedicalPrompt(string userInput, IReadOnlyList<ChatMessage> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine(_medicalAgent.BuildSystemPrompt());
        sb.AppendLine("### المحادثة السابقة:");

        foreach (var msg in history.TakeLast(8))
        {
            if (string.IsNullOrWhiteSpace(msg.Content)) continue;
            sb.AppendLine(msg.IsUser
                ? $"المريض: {msg.Content.Trim()}"
                : $"الطبيب: {msg.Content.Trim()}");
        }

        sb.AppendLine($"\n### الشكوى الحالية:\nالمريض: {userInput.Trim()}");
        sb.AppendLine("\n### ردك (JSON فقط بلا أي نص آخر):");
        return sb.ToString();
    }

    private static string BuildCodePrompt(string userInput, IReadOnlyList<ChatMessage> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine("أنت مهندس برمجيات خبير. أجب باختصار ووضوح.");
        sb.AppendLine("- ضع الكود داخل كتل ``` بلغة مناسبة.");
        sb.AppendLine("- اشرح الكود في سطرين كحد أقصى قبل أو بعد الكود.");
        sb.AppendLine("- حافظ على أسلوب وأسماء ملفات المشروع إذا ذُكِرت.");
        sb.AppendLine();
        sb.AppendLine("### المحادثة:");

        foreach (var msg in history.TakeLast(8))
        {
            if (string.IsNullOrWhiteSpace(msg.Content)) continue;
            sb.AppendLine(msg.IsUser
                ? $"المستخدم: {msg.Content.Trim()}"
                : $"المساعد: {msg.Content.Trim()}");
        }

        sb.AppendLine($"\nالمستخدم: {userInput.Trim()}");
        sb.Append("المساعد:");
        return sb.ToString();
    }

    private static string BuildTranslatePrompt(string userInput, IReadOnlyList<ChatMessage> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine("أنت مترجم محترف بين العربية والإنجليزية والفرنسية. كشف لغة النص ثم ترجمه للغة المطلوبة في السؤال، أو للعربية إذا لم يُحدد.");
        sb.AppendLine("- حافظ على المعنى والنبرة والتنسيق (قوائم، عناوين، رموز).\n- أعد الترجمة فقط دون شرح، ما لم يُطلب شرح.");
        sb.AppendLine();
        sb.AppendLine("### المحادثة:");

        foreach (var msg in history.TakeLast(6))
        {
            if (string.IsNullOrWhiteSpace(msg.Content)) continue;
            sb.AppendLine(msg.IsUser
                ? $"المستخدم: {msg.Content.Trim()}"
                : $"الترجمة: {msg.Content.Trim()}");
        }

        sb.AppendLine($"\nالمستخدم: {userInput.Trim()}");
        sb.Append("الترجمة:");
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // تحليل وتنسيق الرد الطبي (JSON)
    // ------------------------------------------------------------------
    public MedicalResponse? TryParseMedical(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var match = Regex.Match(raw, @"\{.*\}", RegexOptions.Singleline);
        if (!match.Success) return null;

        try
        {
            using var doc = JsonDocument.Parse(match.Value);
            var root = doc.RootElement;
            var response = new MedicalResponse();

            if (root.TryGetProperty("differential", out var diff) && diff.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in diff.EnumerateArray())
                {
                    response.Differential.Add(new DifferentialDiagnosis
                    {
                        Condition = item.TryGetProperty("condition", out var c) ? c.GetString() ?? "" : "",
                        Probability = item.TryGetProperty("probability", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : 0,
                        Reasoning = item.TryGetProperty("reasoning", out var r) ? r.GetString() ?? "" : "",
                        ICD10Code = item.TryGetProperty("icd10", out var i) ? i.GetString() ?? "" : "",
                    });
                }
            }

            if (root.TryGetProperty("plan", out var plan) && plan.ValueKind == JsonValueKind.Array)
                response.Plan = plan.EnumerateArray().Select(x => x.GetString() ?? "").ToList();

            if (root.TryGetProperty("referral", out var referral))
                response.Referral = referral.GetString() ?? "";

            if (root.TryGetProperty("emergency_flags", out var emg) && emg.ValueKind == JsonValueKind.Array)
                response.EmergencyFlags = emg.EnumerateArray().Select(x => x.GetString() ?? "").ToList();

            if (root.TryGetProperty("followup_questions", out var fuq) && fuq.ValueKind == JsonValueKind.Array)
                response.FollowUpQuestions = fuq.EnumerateArray().Select(x => x.GetString() ?? "").ToList();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public string FormatMedicalResponse(string raw)
    {
        var response = TryParseMedical(raw);
        if (response == null)
            return string.IsNullOrWhiteSpace(raw) ? "لم يُصدر النموذج رداً. حاول مرة أخرى بصياغة أبسط." : raw.Trim();

        var sb = new StringBuilder();

        if (response.Differential.Count > 0)
        {
            sb.AppendLine("**التشخيص التفريقي (مرتبة حسب الاحتمال):**");
            foreach (var d in response.Differential)
            {
                sb.AppendLine($"• **{d.Condition}** ({d.Probability}%) — {d.Reasoning}" +
                              (string.IsNullOrEmpty(d.ICD10Code) ? "" : $" [ICD-10: {d.ICD10Code}]"));
            }
            sb.AppendLine();
        }

        if (response.Plan.Count > 0)
        {
            sb.AppendLine("**خطة الإدارة المقترحة:**");
            for (int i = 0; i < response.Plan.Count; i++)
                sb.AppendLine($"{i + 1}. {response.Plan[i]}");
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(response.Referral))
            sb.AppendLine($"**الإحالة:** {response.Referral}\n");

        if (response.EmergencyFlags.Count > 0)
        {
            sb.AppendLine("🚨 **تنبيهات طوارئ:**");
            foreach (var f in response.EmergencyFlags)
                sb.AppendLine($"⚠️ {f}");
            sb.AppendLine();
        }

        if (response.FollowUpQuestions.Count > 0)
        {
            sb.AppendLine("**أسئلة المتابعة:**");
            foreach (var q in response.FollowUpQuestions)
                sb.AppendLine($"? {q}");
            sb.AppendLine();
        }

        sb.AppendLine("_هذا مساعد ذكاء اصطناعي ولا يغني عن استشارة طبية متخصصة._");
        return sb.ToString().Trim();
    }

    public string GetHelpText() => """
💡 **أوامر المحادثة:**

• `/general` — محادثة حرة عامة (الوضع الافتراضي)
• `/medical` — الوضع الطبي (تشخيص تفريدي منظّم)
• `/code` — وضع البرمجة
• `/translate` — وضع الترجمة
• `/new` — بدء محادثة جديدة
• `/save` — حفظ المحادثة الحالية على القرص
• `/model` — معلومات النموذج الحالي
• `/help` — عرض هذه المساعدة

**أمثلة:**
• اسأل أي سؤال عام: «ما الفرق بين الإنفلونزا والكوفيد؟»
• في الوضع الطبي: «عندي ألم صدري منذ ساعتين مع ضيق نفس»
• ترجمة: «ترجم: Good morning doctor»
""";
}
