namespace TabibAI.Linux.Core;

/// <summary>
/// طبقة السلامة: ترجمة حرفية لقواعد التحقق وكشف علامات الخطر المستخدمة في نسخة ويندوز
/// (Program.cs: ValidateCase / DetectUrgentWarning / LimitText / SetUrgentBanner).
/// </summary>
public static class SafetyRules
{
    public const int ConcernLimit = 5000;
    public const int HistoryLimit = 5000;
    public const int ResultsLimit = 6000;
    public const int MaxAttachments = 4;
    public const long MaxImageBytes = 12L * 1024 * 1024;

    public const string LocalConsentMessage =
        "ستُعالج الحالة والصور محلياً على هذا الكمبيوتر. لا تُرسل إلى خدمة سحابية من هذا التطبيق. " +
        "تجنّب المعلومات التعريفية. هل تريد المتابعة؟";

    public const string EmergencyMessage =
        "إذا كانت الأعراض تحدث الآن أو تتدهور، اتصل بخدمات الطوارئ المحلية أو اذهب إلى أقرب قسم طوارئ. " +
        "لا تؤخر المساعدة بانتظار تحليل التطبيق. هل تريد متابعة التحليل التثقيفي المحلي بعد طلب المساعدة عند الحاجة؟";

    public const string DefaultAnswer =
        "أدخل تفاصيل الحالة، واختر مجال المساعدة، ثم اضغط «حلّل الحالة».\n\n" +
        "تذكير: مخرجات النموذج قد تكون غير دقيقة. اعرضها على مختص صحي قبل اتخاذ قرار طبي.";

    /// <summary>يعيد null عند صحة المدخلات، أو رسالة خطأ عربية للعرض.</summary>
    public static string? ValidateAge(string age)
    {
        if (string.IsNullOrWhiteSpace(age)) return null;
        if (!int.TryParse(age.Trim(), out var value) || value is < 0 or > 120)
            return "اكتب العمر كرقم بين 0 و120، أو اتركه فارغاً.";
        return null;
    }

    public static bool HasCaseInput(string concern, string history, string results, int attachmentCount) =>
        !string.IsNullOrWhiteSpace(concern) || !string.IsNullOrWhiteSpace(history)
        || !string.IsNullOrWhiteSpace(results) || attachmentCount > 0;

    public static string LimitText(string value, int limit)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= limit ? trimmed : trimmed[..limit] + "\n[تم اختصار النص الطويل]";
    }

    private static readonly (string[] Terms, string Reason)[] RedFlags =
    {
        (new[] { "ألم صدر", "الم في الصدر", "الم صدر", "ألم في الصدر", "ألم بالصدر", "الم بالصدر", "ضغط الصدر", "ثقل الصدر", "chest pain" }, "ألم أو ضغط في الصدر"),
        (new[] { "ضيق تنفس", "صعوبة التنفس", "لا أستطيع التنفس", "shortness of breath" }, "صعوبة في التنفس"),
        (new[] { "إغماء", "اغماء", "فاقد الوعي", "فقدان الوعي", "unconscious" }, "فقدان الوعي أو إغماء"),
        (new[] { "شلل", "ضعف مفاجئ", "تدلي الوجه", "تلعثم", "سكتة" }, "أعراض عصبية مفاجئة"),
        (new[] { "انتحار", "أقتل نفسي", "اقتل نفسي", "إيذاء نفسي", "suicide" }, "خطر إيذاء النفس"),
        (new[] { "نزيف شديد", "ينزف بشدة", "قيء دم", "براز أسود", "نزيف حاد", "نزيف قوي", "نزيف غزير" }, "نزيف مهم محتمل"),
        (new[] { "تشنج", "اختلاج", "seizure" }, "تشنج أو اختلاج"),
        (new[] { "حساسية شديدة", "تورم اللسان", "تورم الحلق", "anaphylaxis" }, "حساسية شديدة محتملة")
    };

    /// <summary>يعيد سبب التنبيه العاجل، أو نصاً فارغاً إذا لم تُطابق أي علامة خطر.</summary>
    /// <summary>اقتران كلمات الألم مع مواضع خطيرة (يمسك صيغا مختلفة مثل «واجع بطني»).</summary>
    private static readonly (string[] PainWords, string[] Regions, string Reason)[] CoOccurrenceFlags =
    {
        (new[] { "الم", "وجع", "واجع", "ضغط", "ثقل", "حرقة" },
         new[] { "صدر" }, "ألم أو ضغط في الصدر"),
        (new[] { "الم", "وجع", "واجع", "تصلب" },
         new[] { "بطن" }, "ألم بطني شديد محتمل"),
        (new[] { "الم", "وجع", "واجع", "اسوأ", "انفجار" },
         new[] { "راس", "رقبة", "عقب" }, "الم راسي مفاجئ او شديد محتمل")
    };

    /// <summary>تطبيع عربي: يزيل الهمزات والتشكيل الزائد لتقارب الصيغ (ألم/الم، راس/رأس).</summary>
    public static string NormalizeArabic(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var ch in value.ToLowerInvariant())
        {
            switch (ch)
            {
                case 'أ': case 'إ': case 'آ': case 'ٱ': builder.Append('ا'); break;
                case 'ء': case 'ـ': case '\u064B': case '\u064C': case '\u064D':
                case '\u064E': case '\u064F': case '\u0650': case '\u0651': case '\u0652': break;
                case 'ؤ': builder.Append('و'); break;
                case 'ئ': builder.Append('ي'); break;
                case 'ى': builder.Append('ي'); break;
                default: builder.Append(ch); break;
            }
        }
        return System.Text.RegularExpressions.Regex.Replace(builder.ToString(), "\\s+", " ");
    }

    public static string DetectUrgentWarning(string concern, string history, string results)
    {
        var raw = $"{concern} {history} {results}".ToLowerInvariant();
        var text = NormalizeArabic(raw);
        foreach (var flag in RedFlags)
        {
            if (flag.Terms.Any(term => raw.Contains(term) || text.Contains(NormalizeArabic(term))))
                return flag.Reason;
        }
        foreach (var flag in CoOccurrenceFlags)
        {
            if (flag.PainWords.Any(word => text.Contains(NormalizeArabic(word)))
                && flag.Regions.Any(region => text.Contains(NormalizeArabic(region))))
                return flag.Reason;
        }
        return string.Empty;
    }

    public static string UrgentBannerText(string reason) =>
        $"تنبيه: قد تشير المعلومات إلى {reason}. لا تنتظر التحليل إذا كانت الحالة تحدث الآن أو تتدهور؛ اطلب خدمات الطوارئ المحلية.";

    public static string Shorten(string value) =>
        value.Length > 420 ? value[..420] + "…" : value.Replace("\r", " ").Replace("\n", " ");

    public static string DiagnosisDisclaimer =>
        "هذا التطبيق ليس جهازاً طبياً ولا أداة تشخيص معتمدة، وقد يخطئ النموذج. لا تستخدم المخرجات وحدها للتشخيص أو العلاج.";
}
