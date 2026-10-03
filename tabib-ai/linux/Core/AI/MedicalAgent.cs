using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TabibAI.Linux.Core.Models;
using TabibAI.Linux.Core.Llama;

namespace TabibAI.Linux.Core.AI;

public class MedicalAgent : IMedicalAgent
{
    private readonly ILlamaEngine _engine;
    private readonly string _systemPrompt;

    public MedicalAgent(ILlamaEngine engine)
    {
        _engine = engine;
        _systemPrompt = BuildSystemPrompt();
    }

    public string BuildSystemPrompt() => """
أنت طبيب مساعد ذكي (AI Medical Assistant) تتكلم العربية الفصحى.

**دورك:**
- تستمع للشكوى الرئيسية وتاريخ المرض
- تطرح أسئلة متابعة مركزة (أعراض، مدة، شدة، محفزات، عوامل خطر)
- تضع تشخيصًا تفريقيًا مرتبة حسب الاحتمال مع تبرير
- تقترح خطة إدارة أولية (تحاليل، تصوير، إحالة، إسعافات)
- تكتشف علامات الطوارئ وتحيل فورًا
- لا تصف أدوية أبدًا - تحيل للطبيب/الصيدلي

**تنسيق الإخراج (JSON صارم):**
```json
{
  "differential": [
    {"condition": "اسم التشخيص", "probability": 70, "reasoning": "السبب", "icd10": "K00.0"}
  ],
  "plan": ["خطوة 1", "خطوة 2"],
  "referral": "التخصص المطلوب أو فارغ",
  "emergency_flags": ["علم طوارئ 1"],
  "followup_questions": ["سؤال متابعة 1"]
}
```

**قواعد السلامة (مطلقة):**
1. أي عرض طوارئ (ألم صدري ضاغط، ضيق تنفس حاد، نزيف شديد، غيبوبة، حمى >39 مع توعك، صداع مفاجئ شديد، ضعف مفاجئ) → emergency_flags + referral: "طوارئ"
2. لا تذكر أسماء أدوية بجرعات
3. دائماً اختم: "هذا مساعد ذكاء اصطناعي ولا يغني عن استشارة طبية متخصصة."
4. إذا لم تكفِ المعلومات → اطرح أسئلة متابعة محددة

**أسلوبك:** مهني، متعاطف، مختصر، منظم.
""";

    public async Task<MedicalResponse> ProcessAsync(string userInput, IReadOnlyList<ChatMessage> history, CancellationToken ct = default)
    {
        var context = BuildContext(userInput, history);
        var fullPrompt = _systemPrompt + "\n\n" + context;
        var rawResponse = await _engine.InferAsync(fullPrompt, ct);
        return ParseResponse(rawResponse);
    }

    private string BuildContext(string userInput, IReadOnlyList<ChatMessage> history)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("### المحادثة السابقة:");
        foreach (var msg in history.TakeLast(6))
        {
            sb.AppendLine(msg.IsUser ? $"المريض: {msg.Content}" : $"الطبيب: {msg.Content}");
        }
        sb.AppendLine($"\n### الشكوى الحالية:\nالمريض: {userInput}");
        sb.AppendLine("\n### ردك (JSON فقط):");
        return sb.ToString();
    }

    private MedicalResponse ParseResponse(string raw)
    {
        var response = new MedicalResponse();
        
        var jsonMatch = Regex.Match(raw, @"\{.*\}", RegexOptions.Singleline);
        if (!jsonMatch.Success)
            return GetFallbackResponse(raw);

        try
        {
            var doc = JsonDocument.Parse(jsonMatch.Value);
            var root = doc.RootElement;

            if (root.TryGetProperty("differential", out var diff) && diff.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in diff.EnumerateArray())
                {
                    response.Differential.Add(new DifferentialDiagnosis
                    {
                        Condition = item.GetProperty("condition").GetString() ?? "",
                        Probability = item.GetProperty("probability").GetInt32(),
                        Reasoning = item.GetProperty("reasoning").GetString() ?? "",
                        ICD10Code = item.TryGetProperty("icd10", out var icd) ? icd.GetString() ?? "" : ""
                    });
                }
            }

            if (root.TryGetProperty("plan", out var plan) && plan.ValueKind == JsonValueKind.Array)
                response.Plan = plan.EnumerateArray().Select(x => x.GetString() ?? "").ToList();

            if (root.TryGetProperty("referral", out var refProp))
                response.Referral = refProp.GetString() ?? "";

            if (root.TryGetProperty("emergency_flags", out var emg) && emg.ValueKind == JsonValueKind.Array)
                response.EmergencyFlags = emg.EnumerateArray().Select(x => x.GetString() ?? "").ToList();

            if (root.TryGetProperty("followup_questions", out var fuq) && fuq.ValueKind == JsonValueKind.Array)
                response.FollowUpQuestions = fuq.EnumerateArray().Select(x => x.GetString() ?? "").ToList();

            EnhanceSafety(response, raw);
        }
        catch
        {
            return GetFallbackResponse(raw);
        }

        return response;
    }

    private void EnhanceSafety(MedicalResponse response, string rawText)
    {
        var emergencyKeywords = new[]
        {
            "ألم صدري", "ضيق تنفس", "نزيف", "غيبوبة", "إغماء", "شلل", "اختلال نطق",
            "صداع مفاجئ", "حمى عالية", "تقيؤ دم", "براز أسود", "حمل", "نزيف مهبلي",
            "chest pain", "shortness of breath", "bleeding", "unconscious", "stroke"
        };

        var lower = rawText.ToLower();
        foreach (var kw in emergencyKeywords)
        {
            if (lower.Contains(kw) && !response.EmergencyFlags.Any(f => f.Contains(kw, StringComparison.OrdinalIgnoreCase)))
            {
                response.EmergencyFlags.Add($"عرض طوارئ محتمل: {kw}");
                if (string.IsNullOrEmpty(response.Referral) || !response.Referral.Contains("طوارئ"))
                    response.Referral = "طوارئ - قسم الطوارئ فوراً";
            }
        }

        var drugKeywords = new[] { "جرعة", "مجم", "ملجم", "قرص", "حبة", "حقنة", "مرهم", "قطرة" };
        foreach (var dk in drugKeywords)
        {
            if (lower.Contains(dk))
            {
                response.Plan.Insert(0, "⚠️ لا يتم وصف أدوية عبر هذا النظام. راجع طبيب/صيدلي للعلاج الدوائي.");
                break;
            }
        }
    }

    private MedicalResponse GetFallbackResponse(string raw)
    {
        return new MedicalResponse
        {
            Differential = new List<DifferentialDiagnosis>
            {
                new() { Condition = "يحتاج تقييم طبي", Probability = 100, Reasoning = "تعذر تحليل الرد الآلي، يرجى مراجعة طبيب", ICD10Code = "Z00.0" }
            },
            Plan = new List<string> { "مراجعة طبيب مختص للتقييم الكامل", "إحضار أي تقارير أو تحاليل سابقة" },
            Referral = "طب عام / طوارئ حسب الشدة",
            EmergencyFlags = new List<string>(),
            FollowUpQuestions = new List<string> { "منذ متى بدأت الأعراض؟", "هل هناك أمراض مزمنة؟", "هل تتناول أدوية حالياً؟" }
        };
    }

    public string GenerateReport(MedicalCase caseData)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📋 **تقرير طبي - {caseData.CreatedAt:yyyy/MM/dd HH:mm}**");
        sb.AppendLine($"**الشكوى الرئيسية:** {caseData.ChiefComplaint}");
        sb.AppendLine();
        sb.AppendLine("**التشخيص التفريقي:**");
        foreach (var d in caseData.DifferentialDiagnosis)
            sb.AppendLine($"• {d.Condition} ({d.Probability}%) - {d.Reasoning} [{d.ICD10Code}]");
        sb.AppendLine();
        sb.AppendLine("**خطة الإدارة:**");
        foreach (var p in caseData.Plan)
            sb.AppendLine($"• {p}");
        if (!string.IsNullOrEmpty(caseData.Referral))
            sb.AppendLine($"\n**الإحالة:** {caseData.Referral}");
        if (caseData.EmergencyFlags.Count > 0)
            sb.AppendLine($"\n🚨 **تنبيهات طوارئ:** {string.Join(", ", caseData.EmergencyFlags)}");
        sb.AppendLine("\n---\n*تقرير آلي بواسطة TabibAI - لا يغني عن استشارة طبية*");
        return sb.ToString();
    }
}
