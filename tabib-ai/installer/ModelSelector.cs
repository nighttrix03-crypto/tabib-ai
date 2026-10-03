using System;
using System.Collections.Generic;

namespace TabibAIInstaller;

/// <summary>
/// نموذج مدمج في المثبت
/// </summary>
public sealed record EmbeddedModel(
    string Key,
    string DisplayName,
    string FileName,
    double SizeGB,
    int MinRAM_MB,
    int MinVRAM_MB,
    bool RequiresGPU,
    string Description,
    int Priority // 1 = الأفضل، 4 = الأدنى
);

/// <summary>
/// نتيجة اختيار النموذج
/// </summary>
public sealed record ModelSelectionResult(
    EmbeddedModel SelectedModel,
    string Reason,
    bool IsOptimal,
    List<EmbeddedModel> AllCompatibleModels
);

/// <summary>
/// مُختار النموذج الأمثل بناءً على مواصفات الجهاز
/// </summary>
public static class ModelSelector
{
    // قائمة النماذج المضمنة في المثبت (مرتبة حسب الأفضلية)
    private static readonly EmbeddedModel[] AvailableModels = new[]
    {
        // الطبقة 1: الأفضل للتشخيص الطبي (أجهزة قوية)
        new EmbeddedModel(
            Key: "medgemma1.5",
            DisplayName: "MedGemma 1.5 (الأفضل للتشخيص)",
            FileName: "medgemma1.5.gguf",
            SizeGB: 3.3,
            MinRAM_MB: 8000,
            MinVRAM_MB: 6000,
            RequiresGPU: true,
            Description: "نموذج طبي متقدم للتشخيص الدقيق - يحتاج GPU قوي و 8+ GB RAM",
            Priority: 1
        ),

        // الطبقة 2: توازن ممتاز (أجهزة متوسطة)
        new EmbeddedModel(
            Key: "gemma2-2b",
            DisplayName: "Gemma 2B (متوازن وسريع)",
            FileName: "gemma2-2b.gguf",
            SizeGB: 1.6,
            MinRAM_MB: 4000,
            MinVRAM_MB: 2000,
            RequiresGPU: false,
            Description: "نموذج متوازن للعمل العام - يعمل على CPU أو GPU مع 4+ GB RAM",
            Priority: 2
        ),

        // الطبقة 3: خفيف للأجهزة المحدودة
        new EmbeddedModel(
            Key: "gemma3-270m",
            DisplayName: "Gemma 3 270M (خفيف جداً)",
            FileName: "gemma3-270m.gguf",
            SizeGB: 0.27,
            MinRAM_MB: 2000,
            MinVRAM_MB: 0,
            RequiresGPU: false,
            Description: "نموذج فائق الخفة للأجهزة القديمة - يعمل على 2+ GB RAM بدون GPU",
            Priority: 3
        ),

        // الطبقة 4: الحد الأدنى للتشغيل
        new EmbeddedModel(
            Key: "qwen-0.5b",
            DisplayName: "Qwen 0.5B (الحد الأدنى)",
            FileName: "qwen-0.5b.gguf",
            SizeGB: 0.4,
            MinRAM_MB: 512,
            MinVRAM_MB: 0,
            RequiresGPU: false,
            Description: "نموذج أساسي للإرشادات فقط - يعمل على أي جهاز تقريباً",
            Priority: 4
        )
    };

    /// <summary>
    /// اختيار النموذج الأمثل للجهاز
    /// </summary>
    public static ModelSelectionResult SelectOptimalModel(HardwareSpecs specs)
    {
        // Consider only models that are actually embedded in the installer
        var embeddedModels = AvailableModels.Where(m => IsModelEmbedded(m.FileName)).ToList();
        if (!embeddedModels.Any())
        {
            throw new InvalidOperationException("لا توجد نماذج طبية مضمنة في المثبت. تأكد من إضافة at least one .gguf file كمورد مدمج.");
        }

        var compatible = new List<EmbeddedModel>();
        EmbeddedModel? bestMatch = null;
        string reason = "";

        foreach (var model in embeddedModels)
        {
            bool ramOk = specs.TotalRAM_GB * 1024 >= model.MinRAM_MB;
            bool vramOk = !model.RequiresGPU || (specs.HasDedicatedGPU && specs.GPU_VRAM_GB * 1024 >= model.MinVRAM_MB);
            bool diskOk = specs.FreeDiskSpace_GB >= model.SizeGB + 1; // +1 GB هامش أمان

            if (ramOk && vramOk && diskOk)
            {
                compatible.Add(model);

                // أول نموذج متوافق هو الأفضل (لأن القائمة مرتبة حسب الأولوية)
                if (bestMatch == null)
                {
                    bestMatch = model;
                    reason = BuildReason(specs, model);
                }
            }
        }

        // إذا لم يتوافق أي نموذج، استخدم أخف نموذج مدمج كملاذ أخير
        if (bestMatch == null)
        {
            // Find the lightest embedded model (lowest Priority)
            var lightest = embeddedModels.OrderBy(m => m.Priority).LastOrDefault(); // Actually Priority 4 is highest number, but we want lowest priority number? Wait: Priority 1 is best, 4 is worst. So lightest/highest priority number.
            // We want the model with highest Priority number (i.e., lowest requirement) as fallback.
            var fallback = embeddedModels.OrderByDescending(m => m.Priority).FirstOrDefault();
            if (fallback == null) fallback = embeddedModels.First(); // safety
            bestMatch = fallback;
            reason = $"⚠️ الجهاز أقل من المواصفات الدنيا لجميع النماذج المضمنة - سيتم استخدام {bestMatch.DisplayName} كحل أخير. " +
                     $"الجهاز: {specs.TotalRAM_GB:F1} GB RAM، GPU: {(specs.HasDedicatedGPU ? specs.GPU_VRAM_GB : 0):F1} GB VRAM، قرص: {specs.FreeDiskSpace_GB:F1} GB";
        }

        return new ModelSelectionResult(
            SelectedModel: bestMatch,
            Reason: reason,
            IsOptimal: compatible.Count > 0 && bestMatch == compatible[0],
            AllCompatibleModels: compatible
        );
    }

    private static string BuildReason(HardwareSpecs specs, EmbeddedModel model)
    {
        var parts = new List<string>();

        if (model.Priority == 1)
        {
            parts.Add("🎯 جهاز قوي - تم اختيار أفضل نموذج طبي (MedGemma 1.5)");
        }
        else if (model.Priority == 2)
        {
            parts.Add("⚖️ جهاز متوسط - تم اختيار نموذج متوازن (Gemma 2B)");
        }
        else if (model.Priority == 3)
        {
            parts.Add("📱 جهاز محدود - تم اختيار نموذج خفيف (Gemma 3 270M)");
        }
        else
        {
            parts.Add("🔧 جهاز ضعيف - تم اختيار نموذج أساسي (Qwen 0.5B)");
        }

        parts.Add($"المتطلبات: {model.MinRAM_MB / 1024:F1} GB RAM" +
                  (model.RequiresGPU ? $"، {model.MinVRAM_MB / 1024:F1} GB VRAM" : "") +
                  $"، {model.SizeGB:F1} GB مساحة");

        parts.Add($"الجهاز: {specs.TotalRAM_GB:F1} GB RAM" +
                  (specs.HasDedicatedGPU ? $"، {specs.GPU_VRAM_GB:F1} GB VRAM ({specs.GPUName})" : "، لا يوجد GPU مخصص") +
                  $"، {specs.FreeDiskSpace_GB:F1} GB قرص");

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// الحصول على جميع النماذج المتاحة
    /// </summary>
    public static IReadOnlyList<EmbeddedModel> GetAllModels() => AvailableModels;

    /// <summary>
    /// التحقق من وجود ملف النموذج في الموارد
    /// </summary>
    public static bool IsModelEmbedded(string fileName)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var resourceName = $"TabibAIInstaller.Models.{fileName}";
        return assembly.GetManifestResourceInfo(resourceName) != null;
    }
}