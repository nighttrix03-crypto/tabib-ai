using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace TabibAI.Linux.Core.I18n;

public class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new();
    private CultureInfo _currentCulture = new("ar");

    public CultureInfo CurrentCulture => _currentCulture;
    public event EventHandler? CultureChanged;

    public LocalizationService()
    {
        LoadResources();
    }

    private void LoadResources()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = "TabibAI.Linux.Resources.I18n.";
        
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(prefix) || !name.EndsWith(".json")) continue;
            
            var cultureCode = name[prefix.Length..^5]; // remove prefix and .json
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null) continue;

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            _resources[cultureCode] = dict;
        }

        // Fallback embedded resources
        if (!_resources.ContainsKey("ar")) _resources["ar"] = GetDefaultAr();
        if (!_resources.ContainsKey("en")) _resources["en"] = GetDefaultEn();
        if (!_resources.ContainsKey("fr")) _resources["fr"] = GetDefaultFr();
    }

    public void SetCulture(CultureInfo culture)
    {
        if (!_resources.ContainsKey(culture.TwoLetterISOLanguageName))
            culture = new CultureInfo("ar");

        _currentCulture = culture;
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public string this[string key] => GetString(key);

    public string GetString(string key, params object[] args)
    {
        var lang = _currentCulture.TwoLetterISOLanguageName;
        
        if (_resources.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
        {
            return args.Length > 0 ? string.Format(value, args) : value;
        }

        // Fallback to Arabic
        if (_resources["ar"].TryGetValue(key, out var fallback))
            return args.Length > 0 ? string.Format(fallback, args) : fallback;

        return key; // Return key if not found
    }

    private static Dictionary<string, string> GetDefaultAr() => new()
    {
        ["WelcomeMessage"] = "مرحباً بك في تبتيبي (TabibAI) - مساعدك الطبي الذكي. كيف يمكنني مساعدتك اليوم؟",
        ["Error"] = "خطأ",
        ["ResponseStopped"] = "تم إيقاف الرد.",
        ["CaseSaved"] = "تم حفظ الحالة الطبية.",
        ["NewDiagnosis"] = "تشخيص جديد",
        ["FollowUp"] = "متابعة",
        ["GenerateReport"] = "توليد تقرير",
        ["SaveCase"] = "حفظ الحالة",
        ["Settings"] = "الإعدادات",
        ["Language"] = "اللغة",
        ["Theme"] = "السمة",
        ["Model"] = "النموذج",
        ["About"] = "حول",
        ["Send"] = "إرسال",
        ["Stop"] = "إيقاف",
        ["TypeMessage"] = "اكتب رسالتك هنا...",
        ["Typing"] = "يكتب...",
        ["Emergency"] = "طوارئ",
        ["Disclaimer"] = "هذا مساعد ذكاء اصطناعي ولا يغني عن استشارة طبية متخصصة."
    };

    private static Dictionary<string, string> GetDefaultEn() => new()
    {
        ["WelcomeMessage"] = "Welcome to TabibAI - Your AI Medical Assistant. How can I help you today?",
        ["Error"] = "Error",
        ["ResponseStopped"] = "Response stopped.",
        ["CaseSaved"] = "Medical case saved.",
        ["NewDiagnosis"] = "New Diagnosis",
        ["FollowUp"] = "Follow-up",
        ["GenerateReport"] = "Generate Report",
        ["SaveCase"] = "Save Case",
        ["Settings"] = "Settings",
        ["Language"] = "Language",
        ["Theme"] = "Theme",
        ["Model"] = "Model",
        ["About"] = "About",
        ["Send"] = "Send",
        ["Stop"] = "Stop",
        ["TypeMessage"] = "Type your message here...",
        ["Typing"] = "Typing...",
        ["Emergency"] = "Emergency",
        ["Disclaimer"] = "This is an AI assistant and does not replace professional medical advice."
    };

    private static Dictionary<string, string> GetDefaultFr() => new()
    {
        ["WelcomeMessage"] = "Bienvenue sur TabibAI - Votre assistant médical IA. Comment puis-je vous aider ?",
        ["Error"] = "Erreur",
        ["ResponseStopped"] = "Réponse arrêtée.",
        ["CaseSaved"] = "Cas médical enregistré.",
        ["NewDiagnosis"] = "Nouveau diagnostic",
        ["FollowUp"] = "Suivi",
        ["GenerateReport"] = "Générer rapport",
        ["SaveCase"] = "Sauvegarder",
        ["Settings"] = "Paramètres",
        ["Language"] = "Langue",
        ["Theme"] = "Thème",
        ["Model"] = "Modèle",
        ["About"] = "À propos",
        ["Send"] = "Envoyer",
        ["Stop"] = "Arrêter",
        ["TypeMessage"] = "Tapez votre message...",
        ["Typing"] = "Écrit...",
        ["Emergency"] = "Urgence",
        ["Disclaimer"] = "Ceci est un assistant IA et ne remplace pas un avis médical professionnel."
    };
}
