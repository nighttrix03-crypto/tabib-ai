using System.Text;

namespace TabibAI.Linux.Core;

public sealed record Category(string Key, string Label, string Description);

/// <summary>
/// نصوص المجالات الطبية وبناء أوامر النموذج — منقولة حرفياً من نسخة ويندوز
/// (Program.cs: categoryLabels / categoryDescriptions / ComposeCasePrompt / SystemPrompt).
/// </summary>
public static class ClinicalPrompts
{
    public static readonly IReadOnlyList<Category> Categories = new List<Category>
    {
        new("general", "مساعد طبي عام", "نظّم المعلومات الطبية واستكشف الأسئلة التي تستحق مناقشتها مع المختص."),
        new("radiology", "الأشعة والصور", "ارفق صورة واضحة بصيغة PNG أو JPG أو WEBP لقراءة بصرية تمهيدية."),
        new("labs", "التحاليل المخبرية", "أضف نتيجة الفحص مع الوحدات والمدى المرجعي المطبوع في التقرير."),
        new("nutrition", "التغذية", "ناقش الاحتياجات والعادات الغذائية بصورة تثقيفية وآمنة."),
        new("medication", "معلومات الأدوية", "اطلب شرحاً عاماً للمعلومات الدوائية والتداخلات المحتملة."),
        new("emergency", "علامات الخطر والطوارئ", "راجع علامات الخطر؛ عند وجود حالة طارئة اتصل بالإسعاف المحلي فوراً."),
        new("internal", "الباطنة والأمراض المزمنة", "ناقش الأعراض والأمراض المزمنة والمعلومات التي تفيد الطبيب الباطني."),
        new("cardiology", "القلب والدورة الدموية", "ناقش أعراض القلب والقياسات والفحوصات مع التأكيد على علامات الطوارئ."),
        new("neurology", "الأعصاب والدماغ", "ناقش الأعراض العصبية والفحوصات وما يستدعي تقييماً عاجلاً."),
        new("pediatrics", "طب الأطفال", "معلومات تثقيفية للأطفال؛ اطلب العمر والوزن عند أهميتهما ولا تقترح جرعات."),
        new("women", "صحة المرأة والحمل", "ناقش صحة المرأة والحمل مع مراعاة عمر الحمل وعلامات الخطر."),
        new("dermatology", "الجلدية", "ناقش وصف الطفح أو الصورة الجلدية وحدود التقييم البصري."),
        new("mental", "الصحة النفسية", "ناقش الصحة النفسية بلغة داعمة؛ اسأل عن السلامة عند وجود خطر إيذاء النفس."),
        new("surgery", "الجراحة والعظام", "ناقش الإصابات والأعراض العضلية الهيكلية والأسئلة قبل وبعد الجراحة."),
        new("ophthalmology", "طب العيون", "ناقش أعراض العين؛ الألم الشديد أو فقدان البصر المفاجئ يتطلب تقييماً عاجلاً."),
        new("ent", "الأنف والأذن والحنجرة", "ناقش أعراض الأنف والأذن والحنجرة، ويمكن إرفاق صورة واضحة للحلق أو الأذن."),
        new("dental", "صحة الفم والأسنان", "ناقش مشكلات الأسنان والفم؛ توجّه لطبيب الأسنان عند الحاجة العملية."),
        new("endocrine", "الغدد والسكري", "ناقش أمراض الغدد والسكري والوزن استناداً إلى القيم المقدَّمة دون تعديل أي دواء."),
        new("gastro", "الجهاز الهضمي", "ناقش الأعراض الهضمية والكبدية ونقص الوزن غير المقصود مع إبراز علامات الخطر."),
        new("renal", "الكلى والمسالك", "ناقش أعراض المسالك البولية والكلى مع علامات العدوى الحادة أو احتباس البول."),
        new("rheumatology", "الروماتيزم والمفاصل", "ناقش آلام المفاصل والظهر والعضلات والإصابات وما يستدعي تصويراً أو تقييماً طبياً."),
        new("geriatrics", "طب كبار السن", "راعِ تعدد الأمراض والأدوية متعددة عند كبار السن وما يستدعي مراجعة الطبيب.")
    };

    public static Category ByKey(string key) =>
        Categories.FirstOrDefault(c => c.Key == key) ?? Categories[0];

    public static string ComposeCasePrompt(Category category, string age, string sex,
        string concern, string history, string results, int attachmentCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"مجال المساعدة: {category.Label}.");
        if (!string.IsNullOrWhiteSpace(age)) sb.AppendLine($"العمر: {age.Trim()}.");
        if (!string.IsNullOrWhiteSpace(sex)) sb.AppendLine($"الجنس البيولوجي: {sex}.");
        AppendSection(sb, "سبب الاستشارة والأعراض", SafetyRules.LimitText(concern, SafetyRules.ConcernLimit));
        AppendSection(sb, "التاريخ والسياق الصحي", SafetyRules.LimitText(history, SafetyRules.HistoryLimit));
        AppendSection(sb, "الفحوصات والقياسات والملاحظات", SafetyRules.LimitText(results, SafetyRules.ResultsLimit));
        if (attachmentCount > 0)
            sb.AppendLine($"يوجد {Math.Min(attachmentCount, SafetyRules.MaxAttachments)} صورة/شريحة مرفقة للفحص البصري. إن كانت ملفات DICOM فهي شرائح منفردة وليست سلسلة تصوير كاملة.");
        sb.AppendLine();
        sb.AppendLine("أجب باللغة العربية الواضحة. اذكر ما تدعمه المعلومات وما لا يمكن استنتاجه منها. لا تملأ الفراغات بافتراضات.");
        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string title, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.AppendLine($"{title}:");
        sb.AppendLine(value.Trim());
    }
}
