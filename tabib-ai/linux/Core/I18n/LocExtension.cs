using System;
using Avalonia.Markup.Xaml;

namespace TabibAI.Linux.Core.I18n;

public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key)) return Key;

        // Simple fallback - in production use proper DI
        return Key;
    }
}
