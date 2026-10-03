using System.Diagnostics;

namespace TabibAI.Linux.Core;

/// <summary>أوضاع سطر الأوامر: فحص Ollama وطلب استشارة واحدة بلا واجهة (للتحقق والاختبار).</summary>
public static class Cli
{
    public static void PrintHelp()
    {
        Console.WriteLine("طبيب AI — نسخة لينكس");
        Console.WriteLine();
        Console.WriteLine("  TabibAI                 تشغيل الواجهة الرسومية");
        Console.WriteLine("  TabibAI --selftest      اختبار ذاتي لقواعد السلامة وترميز الصور ومسار DICOM");
        Console.WriteLine("  TabibAI --check         فحص حالة Ollama ووجود نموذج MedGemma");
        Console.WriteLine("  TabibAI --ask \"نص\"     إرسال حالة واحدة إلى النموذج المحلي وطباعة الرد");
        Console.WriteLine("        --category labs   تحديد مجال المساعدة (الافتراضي general)");
        Console.WriteLine("        --age 45          العمر");
        Console.WriteLine("        --sex أنثى        الجنس البيولوجي");
        Console.WriteLine();
        Console.WriteLine("متغيرات البيئة: TABIB_OLLAMA_URL (الافتراضي http://localhost:11434)");
    }

    public static async Task<int> CheckAsync()
    {
        var client = new OllamaClient();
        Console.WriteLine("عنوان Ollama: " + client.Url);
        var ready = await client.IsModelReadyAsync();
        Console.WriteLine(ready
            ? "● MedGemma جاهز محلياً"
            : "○ النموذج المحلي غير جاهز: شغّل Ollama ثم نفّذ: ollama pull " + OllamaClient.MedicalModel);
        return ready ? 0 : 1;
    }

    public static async Task<int> AskAsync(string[] args)
    {
        string category = "general", age = "", sex = "";
        var text = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--category" when i + 1 < args.Length:
                    category = args[++i];
                    break;
                case "--age" when i + 1 < args.Length:
                    age = args[++i];
                    break;
                case "--sex" when i + 1 < args.Length:
                    sex = args[++i];
                    break;
                default:
                    text.Add(args[i]);
                    break;
            }
        }
        var concern = string.Join(' ', text).Trim();
        var ageError = SafetyRules.ValidateAge(age);
        if (ageError is not null)
        {
            Console.Error.WriteLine("خطأ: " + ageError);
            return 2;
        }
        if (string.IsNullOrWhiteSpace(concern))
        {
            Console.Error.WriteLine("اكتب نص الحالة: TabibAI --ask \"ألم في البطن منذ يومين\"");
            return 2;
        }
        var reason = SafetyRules.DetectUrgentWarning(concern, "", "");
        if (reason.Length > 0)
        {
            Console.WriteLine("⚠ " + SafetyRules.UrgentBannerText(reason));
            Console.WriteLine(SafetyRules.EmergencyMessage);
            Console.WriteLine();
        }
        var selected = ClinicalPrompts.ByKey(category);
        var prompt = ClinicalPrompts.ComposeCasePrompt(selected, age, sex, concern, "", "", 0);
        var client = new OllamaClient();
        var turns = new List<ChatTurn>
        {
            new("system", SystemPrompts.For(selected.Key, 0), new List<string>()),
            new("user", prompt, new List<string>())
        };
        var watch = Stopwatch.StartNew();
        try
        {
            var answer = await client.ChatAsync(turns);
            watch.Stop();
            Console.WriteLine($"=== رد {OllamaClient.MedicalModel} ({watch.Elapsed.TotalSeconds:F1} ثانية) ===");
            Console.WriteLine(answer);
            Console.WriteLine();
            Console.WriteLine(SafetyRules.DiagnosisDisclaimer);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("تعذّر التحليل المحلي: " + SafetyRules.Shorten(ex.Message));
            return 1;
        }
    }
}
