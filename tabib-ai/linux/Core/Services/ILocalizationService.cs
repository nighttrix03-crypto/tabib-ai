using System;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Supported languages.
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
}

/// <summary>
/// Interface for localization.
/// </summary>
public interface ILocalizationService
{
    Language CurrentLanguage { get; }
    event Action<Language>? OnLanguageChanged;
    void SetLanguage(Language language);
    string GetString(string key);
    IReadOnlyList<Language> GetSupportedLanguages();
}