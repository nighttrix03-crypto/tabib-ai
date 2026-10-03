using System;

namespace TabibAI.Linux.Ui;

/// <summary>نوافذ تأكيد وتنبيه بسيطة (بديل MessageBox في ويندوز) بواجهة عربية RTL.</summary>
public static class Dialogs
{
    public static async Task<bool> ConfirmAsync(Avalonia.Controls.Window owner, string title, string message)
    {
        var dialog = Build(title, message, "متابعة", "إلغاء");
        var result = await dialog.ShowDialog<bool>(owner);
        return result;
    }

    public static async Task InfoAsync(Avalonia.Controls.Window owner, string title, string message)
    {
        var dialog = Build(title, message, "حسناً", null);
        await dialog.ShowDialog<bool>(owner);
    }

    /// <summary>نافذة خطأ (تظهر كرسالة تنبيه) — واجهة موحّدة مع InfoAsync.</summary>
    public static Task ErrorAsync(Avalonia.Controls.Window owner, string title, string message)
        => InfoAsync(owner, title, message);

    private static Avalonia.Controls.Window Build(string title, string message, string okText, string? cancelText)
    {
        var result = false;
        var window = new Avalonia.Controls.Window
        {
            Title = title,
            Width = 560,
            SizeToContent = Avalonia.Controls.SizeToContent.Height,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            FlowDirection = Avalonia.Media.FlowDirection.RightToLeft,
            Background = Avalonia.Media.Brushes.White,
            CanResize = false
        };

        var messageText = new Avalonia.Controls.TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 0, 0, 18),
            FontSize = 14
        };

        var okButton = new Avalonia.Controls.Button { Content = okText, Padding = new Avalonia.Thickness(18, 8), Background = Avalonia.Media.Brush.Parse("#137C7F"), Foreground = Avalonia.Media.Brushes.White };
        okButton.Click += (_, _) => { result = true; window.Close(); };

        var buttons = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8
        };

        if (cancelText is not null)
        {
            var cancelButton = new Avalonia.Controls.Button { Content = cancelText, Padding = new Avalonia.Thickness(18, 8) };
            cancelButton.Click += (_, _) => { result = false; window.Close(); };
            buttons.Children.Add(cancelButton);
        }
        buttons.Children.Add(okButton);

        window.Content = new Avalonia.Controls.Border
        {
            Padding = new Avalonia.Thickness(22),
            Child = new Avalonia.Controls.StackPanel { Children = { messageText, buttons } }
        };
        window.Closed += (_, _) => { };
        return window;
    }
}
