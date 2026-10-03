using System.Globalization;

namespace TabibAI.Linux.Core.I18n;

public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }
    event EventHandler? CultureChanged;
    void SetCulture(CultureInfo culture);
    string this[string key] { get; }
    string GetString(string key, params object[] args);
}
