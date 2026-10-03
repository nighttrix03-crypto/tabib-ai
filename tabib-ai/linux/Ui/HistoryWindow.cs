using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TabibAI.Linux.Core;

namespace TabibAI.Linux;

/// <summary>نافذة سجل الحالات المحفوظة محلياً (عرض، فتح، حذف، مسح).</summary>
public sealed class HistoryWindow : Window
{
    private readonly ListBox list = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private List<CaseRecord> records;

    public CaseRecord? SelectedRecord { get; private set; }

    public HistoryWindow(List<CaseRecord> items)
    {
        records = items;
        Title = "سجل الحالات المحلية";
        Width = 780;
        Height = 560;
        FlowDirection = FlowDirection.RightToLeft;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;
        FontFamily = new FontFamily("Noto Naskh Arabic, Noto Sans Arabic, DejaVu Sans");

        var openButton = MakeButton("فتح المحدد", true);
        var deleteButton = MakeButton("حذف المحدد", false);
        var clearButton = MakeButton("مسح كل السجل", false);
        var closeButton = MakeButton("إغلاق", false);

        openButton.Click += (_, _) =>
        {
            if (list.SelectedIndex < 0 || list.SelectedIndex >= records.Count) return;
            SelectedRecord = records[list.SelectedIndex];
            Close();
        };
        deleteButton.Click += (_, _) =>
        {
            if (list.SelectedIndex < 0 || list.SelectedIndex >= records.Count) return;
            CaseStore.Delete(records[list.SelectedIndex].Id);
            Reload();
        };
        clearButton.Click += (_, _) =>
        {
            CaseStore.Clear();
            Reload();
        };
        closeButton.Click += (_, _) => Close();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(14, 10, 14, 14),
            Children = { openButton, deleteButton, clearButton, closeButton }
        };
        var header = new TextBlock
        {
            Text = "السجل يُخزَّن محلياً فقط في مجلد بيانات المستخدم، ولا يُرسل إلى أي خادم.",
            Foreground = Brush.Parse("#687C85"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(14, 12, 14, 8)
        };
        var body = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(buttons, Dock.Bottom);
        body.Children.Add(header);
        body.Children.Add(buttons);
        body.Children.Add(new Border { Padding = new Avalonia.Thickness(14, 0), Child = list });
        Content = body;

        Reload();
    }

    private static Button MakeButton(string text, bool primary) => new()
    {
        Content = text,
        Padding = new Avalonia.Thickness(16, 8),
        Background = primary ? Brush.Parse("#137C7F") : Brushes.White,
        Foreground = primary ? Brushes.White : Brush.Parse("#1B2D38"),
        BorderBrush = Brush.Parse("#DEE8E9")
    };

    private void Reload()
    {
        records = CaseStore.LoadAll();
        list.ItemsSource = records
            .Select(r => $"{r.SavedAt:yyyy-MM-dd HH:mm} · {ClinicalPrompts.ByKey(r.Category).Label} · {Preview(r.Concern)}")
            .ToList();
        if (records.Count == 0) list.ItemsSource = new List<string> { "لا توجد حالات محفوظة بعد." };
    }

    private static string Preview(string text)
    {
        var flat = (text ?? "").Replace("\n", " ").Trim();
        return flat.Length <= 70 ? flat : flat[..70] + "…";
    }
}
