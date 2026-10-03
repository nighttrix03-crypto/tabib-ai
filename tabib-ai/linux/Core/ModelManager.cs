using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace TabibAI.Linux.Core;

/// <summary>
/// إدارة اللغات وترجمة نصوص واجهة المستخدم.
/// </summary>
public static class LanguageManager
{
    /// <summary>
    /// رموز اللغات المدعومة.
    /// </summary>
    public enum Language
    {
        Arabic,
        English,
        French,
        Spanish,
        German,
        Portuguese,
        Russian,
        Chinese
    };

    /// <summary>
    /// خريطة رموز اللغات إلى أسماء العرض الكاملة.
    /// </summary>
    private static readonly Dictionary<Language, string> LanguageNames = new()
    {
        { Language.Arabic, "العربية" },
        { Language.English, "English" },
        { Language.French, "Français" },
        { Language.Spanish, "Español" },
        { Language.German, "Deutsch" },
        { Language.Portuguese, "Português" },
        { Language.Russian, "Русский" },
        { Language.Chinese, "中文" }
    };

    /// <summary>
    /// اللغات المدعومة (للتحديد متعدد اللغات).
    /// </summary>
    public static IReadOnlyList<Language> SupportedLanguages => LanguageNames.Keys.ToList();

    /// <summary>
    /// يحصل على اسم اللغة المعروض.
    /// </summary>
    public static string GetLanguageDisplayName(Language language) => LanguageNames[language];

    /// <summary>
    /// يحصل على رمز العلم لكل لغة.
    /// </summary>
    public static string GetLanguageFlag(Language language) => language switch
    {
        Language.Arabic => "🇮🇶",
        Language.English => "🇺🇸",
        Language.French => "🇫🇷",
        Language.Spanish => "🇪🇸",
        Language.German => "🇩🇪",
        Language.Portuguese => "🇵🇹",
        Language.Russian => "🇷🇺",
        Language.Chinese => "🇨🇳",
        _ => "🌐"
    };
}

/// <summary>
/// دعم النصوص المترجمة لنصوص واجهة المستخدم.
/// </summary>
public static class Translations
{
    /// <summary>
    /// النص الافتراضي (الإنجليزية) للنسخة الأصلية.
    /// </summary>
    public const string DefaultLanguage = "English";

    /// <summary>
    /// خريطة مفتاح النص إلى النصوص المترجمة لكل لغة.
    /// تم إنشاء العديد من مفاتيح النصوص بشكل رمزي؛ يتم توفير نسخ نموذجية للبدء.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string>> TranslationMap = new()
    {
        {
            "app_title", new Dictionary<string, string>
            {
                { "Arabic", "طبيب AI — نسخة لينكس" },
                { "English", "Tabib AI — Linux Version" },
                { "French", "Tabib AI — Version Linux" },
                { "Spanish", "Tabib AI — Versión Linux" },
                { "German", "Tabib AI — Linux-Version" },
                { "Portuguese", "Tabib AI — Versão Linux" },
                { "Russian", "Tabib AI — Версия для Linux" },
                { "Chinese", "Tabib AI — Linux 版" }
            }
        },
        {
            "app_subtitle", new Dictionary<string, string>
            {
                { "Arabic", "مساعد معلومات صحية يعمل محلياً على جهازك عبر Ollama + MedGemma. لا يُرسل شيء إلى أي خادم." },
                { "English", "Health information assistant working locally on your device via Ollama + MedGemma. Nothing is sent to any server." },
                { "French", "Assistant d'information santé fonctionnant localement sur votre appareil via Ollama + MedGemma. Rien n'est envoyé à aucun serveur." },
                { "Spanish", "Asistente de información de salud funcionando localmente en tu dispositivo a través de Ollama + MedGemma. Nada se envía a ningún servidor." },
                { "German", "Gesundheitsinformations-Assistent, der lokal über Ollama + MedGemma auf Ihrem Gerät läuft. Nichts wird an Server gesendet." },
                { "Portuguese", "Assistente de informações de saúde funcionando localmente no seu dispositivo através do Ollama + MedGemma. Nada é enviado a servidores." },
                { "Russian", "Ассистент по предоставлению медицинской информации, работающий локально на вашем устройстве через Ollama + MedGemma. Ничего не отправляется на сервер." },
                { "Chinese", "健康信息助手通过Ollama + MedGemma在本地运行在您的设备上。不会向任何服务器发送任何信息。" }
            }
        },
        {
            "device_info_label", new Dictionary<string, string>
            {
                { "Arabic", "معلومات الجهاز" },
                { "English", "Device Information" },
                { "French", "Informations sur l'appareil" },
                { "Spanish", "Información del dispositivo" },
                { "German", "Geräteinformationen" },
                { "Portuguese", "Informação do dispositivo" },
                { "Russian", "Информация об устройстве" },
                { "Chinese", "设备信息" }
            }
        },
        {
            "language_selection", new Dictionary<string, string>
            {
                { "Arabic", "اختيار اللغة" },
                { "English", "Language Selection" },
                { "French", "Sélection de la langue" },
                { "Spanish", "Selección de idioma" },
                { "German", "Sprachauswahl" },
                { "Portuguese", "Seleção de idioma" },
                { "Russian", "Выбор языка" },
                { "Chinese", "语言选择" }
            }
        },
        {
            "select_app_title", new Dictionary<string, string>
            {
                { "Arabic", "اختر التطبيق الطبي الذي تريد استخدامه" },
                { "English", "Choose the medical app you want to use" },
                { "French", "Choisissez l'application médicale que vous souhaitez utiliser" },
                { "Spanish", "Elige la aplicación médica que deseas usar" },
                { "German", "Wählen Sie die medizinische Anwendung aus, die Sie verwenden möchten" },
                { "Portuguese", "Escolha o aplicativo médico que deseja usar" },
                { "Russian", "Выберите медицинское приложение, которое хотите использовать" },
                { "Chinese", "选择您要使用的医疗应用" }
            }
        },
        {
            "cancel", new Dictionary<string, string>
            {
                { "Arabic", "إلغاء" },
                { "English", "Cancel" },
                { "French", "Annuler" },
                { "Spanish", "Cancelar" },
                { "German", "Abbrechen" },
                { "Portuguese", "Cancelar" },
                { "Russian", "Отмена" },
                { "Chinese", "取消" }
            }
        }
    };

    /// <summary>
    /// يحصل على النص المترجم لمفتاح معين ولغة معينة.
    /// </summary>
    public static string GetTranslation(string key, LanguageManager.Language language)
    {
        if (TranslationMap.TryGetValue(key, out var translations) &&
            translations.TryGetValue(language.ToString(), out var translation))
        {
            return translation;
        }
        return TranslationMap[key][DefaultLanguage.ToString()];
    }
}

/// <summary>
/// نظام اختيار النموذج الذكي الذي يختار النموذج الأمثل حسب نوع التطبيق وقدرات الجهاز
/// ينفذ خريطة التطبيقات إلى النماذج المثلى وقواعد تحديد مستوى الجهاز من السياق.
/// </summary>
public sealed class ModelManager
{
    // خريطة التطبيقات إلى النماذج المثلى (من السياق)
    private static readonly Dictionary<string, string> AppModelMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "xray_analysis", "qwen:0.5b" },
        { "mri_scan", "qwen:0.5b" },
        { "ct_analysis", "gemma3:270m" },
        { "lab_analysis", "gemma3:270m" },
        { "ecg_analysis", "gemma3:270m" },
        { "primary_care", "gemma:2b" },
        { "general_checkup", "gemma:2b" },
        { "specialized_diagnosis", "medgemma1.5" },
        { "emergency_care", "medgemma1.5" },
        { "pediatrics", "gemma:2b" }, // أو gemma3:270m للأطفال
        { "psychology", "qwen:0.5b" }, // تعليمي للاستشارات
        { "nutrition", "gemma3:270m" },  // موثوق للمعلومات الغذائية
        { "dermatology", "gemma3:270m" }, // موثوق لوصف الطفح الجلدي
    };

    // تعريفات مستوى النموذج من السياق
    public enum ModelLayer
    {
        Layer1, // medgemma1.5 (الأفضل للتشخيص الطبي، ≥8 جيجابايت رام)
        Layer2, // gemma:2b، gemma3:270m (متوازن/مبتدئ، 2-8 جيجابايت رام)
        Layer3, // qwen:0.5b (أساسي، أي جهاز تقريبًا)
    }

    // معلومات مستوى النموذج
    private static readonly Dictionary<string, (string Name, double SizeGB, int MinRAMMB, ModelLayer Layer)> ModelInfo = new()
    {
        { "medgemma1.5", ("medgemma1.5:latest", 3.3, 8000, ModelLayer.Layer1) },
        { "gemma:2b", ("gemma:2b", 1.3, 4000, ModelLayer.Layer2) },
        { "gemma3:270m", ("gemma3:270m", 0.2, 2000, ModelLayer.Layer2) },
        { "qwen:0.5b", ("qwen:0.5b", 0.4, 512, ModelLayer.Layer3) },
    };

    /// <summary>
    /// يختار النموذج الأمثل لنوع التطبيق المعطى وقدرات الجهاز.
    /// </summary>
    public static string GetModelForApp(string appType, int deviceRAM)
    {
        // خريطة التطبيقات إلى النماذج المثلى
        string baseModel = AppModelMap.TryGetValue(appType, out var optimalModel) ? optimalModel : "gemma:2b";

        // احترام حد الجهاز
        if (deviceRAM >= 8000) // 8 جيجابايت+
        {
            // الأفضل للتشخيص المتخصص
            return AppModelMap.TryGetValue(appType, out var specializedModel) ? specializedModel : "medgemma1.5";
        }
        else if (deviceRAM >= 4000) // 4-8 جيجابايت
        {
            if (baseModel == "medgemma1.5") // لا يمكن تشغيله
                return "gemma:2b"; // توازن ممتاز
            return baseModel;
        }
        else if (deviceRAM >= 2000) // 2-4 جيجابايت
        {
            if (baseModel == "medgemma1.5" || baseModel == "gemma:2b") // لا يمكن تشغيله
                return "gemma3:270m"; // جيد للتشخيص الأولي
            return baseModel;
        }
        else // أقل من 2 جيجابايت
        {
            return "qwen:0.5b"; // يعمل دائمًا
        }
    }

    /// <summary>
    /// يختار النموذج الأمثل بناءً على قدرات الجهاز فقط (للعودة التلقائية).
    /// </summary>
    public static string SelectOptimalModelByDevice(int availableRAM)
    {
        foreach (var model in ModelInfo.Values.OrderBy(m => m.Layer))
        {
            if (availableRAM >= model.MinRAMMB)
                return model.Name;
        }
        return ModelInfo["qwen:0.5b"].Name;
    }

    /// <summary>
    /// يحصل على اسم النموذج الآمن لتحميله (بدون الطرفيات).
    /// </summary>
    public static string GetSafeModelName(string modelKey) => ModelInfo.TryGetValue(modelKey, out var info) ? info.Name : "gemma:2b";

    /// <summary>
    /// يحصل على متطلبات RAM لتحميل النموذج (بالجيجابايت).
    /// </summary>
    public static int GetModelRAMRequirements(string modelKey) => ModelInfo.TryGetValue(modelKey, out var info) ? info.MinRAMMB : 4000;

    /// <summary>
    /// يحصل على حجم النموذج (بالجيجابايت).
    /// </summary>
    public static double GetModelSizeGB(string modelKey) => ModelInfo.TryGetValue(modelKey, out var info) ? info.SizeGB : 1.3;

    /// <summary>
    /// يحصل على جميع مفاتيح النماذج المتاحة.
    /// </summary>
    public static IReadOnlyList<string> GetAvailableModelKeys()
    {
        return ModelInfo.Keys.ToList();
    }

    /// <summary>
    /// يحصل على اسم النموذج المعروض لنوع التطبيق.
    /// </summary>
    public static string GetAppModelDisplayName(string appType)
    {
        var modelKey = AppModelMap.TryGetValue(appType, out var model) ? model : "gemma:2b";
        return ModelInfo.TryGetValue(modelKey, out var info) ? info.Name : "gemma:2b:latest";
    }

    /// <summary>
    /// يحصل على متطلبات RAM لتطبيق معين (بالجيجابايت).
    /// </summary>
    public static int GetAppRAMRequirements(string appType)
    {
        var modelKey = AppModelMap.TryGetValue(appType, out var model) ? model : "gemma:2b";
        return GetModelRAMRequirements(modelKey);
    }

    /// <summary>
    /// يحدد ما إذا كان الجهاز يمكنه تشغيل النموذج المختار.
    /// </summary>
    public static bool IsDeviceCompatible(string appType, int deviceRAM)
    {
        var modelKey = AppModelMap.TryGetValue(appType, out var model) ? model : "gemma:2b";
        return deviceRAM >= GetModelRAMRequirements(modelKey);
    }

    /// <summary>
    /// يحدد نظام التشغيل والمعلومات حول الجهاز.
    /// </summary>
    public static (string Os, int TotalRAMMB, int AvailableRAMMB) GetDeviceInfo()
    {
        string os = Environment.OSVersion.Platform.ToString();
        int totalRAM = 0;
        int availableRAM = 0;

        try
        {
            if (OperatingSystem.IsLinux())
            {
                // قراءة معلومات الذاكرة من /proc/meminfo على لينكس
                if (File.Exists("/proc/meminfo"))
                {
                    var meminfo = File.ReadAllText("/proc/meminfo");
                    var lines = meminfo.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("MemTotal:"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim().Split(' ')[0], out var memTotalKB))
                            {
                                totalRAM = memTotalKB / 1024; // تحويل إلى ميجابايت
                            }
                        }
                        else if (line.Contains("MemAvailable:") && totalRAM > 0)
                        {
                            var parts = line.Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim().Split(' ')[0], out var memAvailableKB))
                            {
                                availableRAM = memAvailableKB / 1024; // تحويل إلى ميجابايت
                            }
                        }
                    }
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                // macOS: hw.memsize عبر sysctlbyname (libc) — تقريب المتاح بثلثي الكلي
                if (SysctlHwMemsize(out var memSize))
                {
                    totalRAM = (int)(memSize / (1024UL * 1024UL));
                    availableRAM = totalRAM * 2 / 3;
                }
            }
            else
            {
                // Windows: GlobalMemoryStatusEx (PerformanceCounter غير متاح في .NET الحديث)
                var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
                if (GlobalMemoryStatusEx(ref status))
                {
                    totalRAM = (int)(status.TotalPhys / (1024 * 1024));
                    availableRAM = (int)(status.AvailPhys / (1024 * 1024));
                }
            }
        }
        catch
        {
            // إذا فشل أي شيء، استخدم القيم الافتراضية
            totalRAM = 8192; // 8 جيجابايت افتراضيًا
            availableRAM = 4096; // 4 جيجابايت متاح افتراضيًا
        }

        return (os, totalRAM, availableRAM);
    }

    /// <summary>
    /// يحصل على خريطة تطبيقات الطب إلى النماذج مع وصف المجال ومتطلبات التشغيل
    /// (تُستخدم من نافذة اختيار التطبيق).
    /// </summary>
    public static Dictionary<string, (string ModelKey, string ModelName, double SizeGB, int RamReq, string Description, string Layer)>
        GetAppModelMapWithDomainInstructions()
    {
        var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "xray_analysis", "تحليل الأشعة السينية (مراجعة تعليمية للصورة الواحدة)" },
            { "mri_scan", "مراجعة صور الرنين المغناطيسي" },
            { "ct_analysis", "مراجعة صور الأشعة المقطعية" },
            { "lab_analysis", "تفسير نتائج المختبر" },
            { "ecg_analysis", "شرح تخطيط القلب" },
            { "primary_care", "رعاية أولية عامة ومعلومات صحية" },
            { "general_checkup", "فحص عام واستفسارات صحية يومية" },
            { "specialized_diagnosis", "حالات متخصصة — يتطلب تقييم مختص" },
            { "emergency_care", "إرشادات الطوارئ والإسعاف الأولي خطوة بخطوة" },
            { "pediatrics", "طب الأطفال" },
            { "psychology", "الصحة النفسية (تثقيفي غير تشخيصي)" },
            { "nutrition", "التغذية الصحية" },
            { "dermatology", "معلومات الأمراض الجلدية" },
        };

        var result = new Dictionary<string, (string ModelKey, string ModelName, double SizeGB, int RamReq, string Description, string Layer)>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var app in AppModelMap)
        {
            var info = ModelInfo.TryGetValue(app.Value, out var found) ? found : ModelInfo["gemma:2b"];
            result[app.Key] = (app.Value, info.Name, info.SizeGB, info.MinRAMMB,
                descriptions.TryGetValue(app.Key, out var desc) ? desc : app.Key, info.Layer.ToString());
        }
        return result;
    }

    [DllImport("libc", EntryPoint = "sysctlbyname")]
    private static extern int SysctlByNameRaw(string name, out ulong value, IntPtr oldlenp, IntPtr newp, IntPtr newlenp);

    private static bool SysctlHwMemsize(out ulong bytes)
    {
        bytes = 0;
        try
        {
            if (SysctlByNameRaw("hw.memsize", out bytes, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) == 0 && bytes > 0)
                return true;
        }
        catch { /* DllNotFound على غير macOS */ }
        return false;
    }

    [DllImport("kernel32.dll")]


    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}