using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TabibAI.Linux.Core;
using TabibAI.Linux.Ui;

namespace TabibAI.Linux;

public partial class MainWindow : Window
{
    private static List<FilePickerFileType> BuildImageTypes() => new()
    {
        new FilePickerFileType("صور و DICOM") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif", "*.dcm", "*.ima" } },
        new FilePickerFileType("صور") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif" } },
        new FilePickerFileType("DICOM") { Patterns = new[] { "*.dcm", "*.ima" } },
        new FilePickerFileType("كل الملفات") { Patterns = new[] { "*" } }
    };

    private async void OnAddImagesClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "اختيار صور طبية",
            AllowMultiple = true,
            FileTypeFilter = BuildImageTypes()
        });
        var rejected = new List<string>();
        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) continue;
            if (imagePaths.Contains(path, StringComparer.OrdinalIgnoreCase)) continue;
            if (imagePaths.Count >= SafetyRules.MaxAttachments)
            {
                rejected.Add("الحد 4 صور أو شرائح في الجلسة. لا تعتبر ذلك بديلاً عن مراجعة سلسلة الأشعة كاملة.");
                break;
            }
            bool dicom = DicomImporter.IsDicomPath(path);
            if (!dicom && new FileInfo(path).Length > SafetyRules.MaxImageBytes)
            {
                rejected.Add($"الصورة أكبر من 12 ميغابايت: {Path.GetFileName(path)}");
                continue;
            }
            imagePaths.Add(path);
        }
        RefreshAttachments();
        if (rejected.Count > 0)
            await Dialogs.InfoAsync(this, "حد المرفقات", string.Join("\n", rejected));
    }

    private void OnRemoveImageClick(object? sender, RoutedEventArgs e)
    {
        if (selectedAttachment < 0 || selectedAttachment >= imagePaths.Count) return;
        imagePaths.RemoveAt(selectedAttachment);
        RefreshAttachments();
    }

    private async void OnImportTextClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "استيراد تقرير نصي",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("تقارير نصية") { Patterns = new[] { "*.txt", "*.csv", "*.md" } },
                new FilePickerFileType("كل الملفات") { Patterns = new[] { "*" } }
            }
        });
        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var text = await File.ReadAllTextAsync(path, System.Text.Encoding.UTF8);
            if (ResultsBox.Text?.Length > 0) ResultsBox.Text += "\n\n";
            ResultsBox.Text += $"— {Path.GetFileName(path)} —\n{text}";
        }
        catch (Exception ex)
        {
            await Dialogs.InfoAsync(this, "خطأ", "تعذّر قراءة الملف النصي. " + SafetyRules.Shorten(ex.Message));
        }
    }

    private async void OnClearClick(object? sender, RoutedEventArgs e)
    {
        if (SafetyRules.HasCaseInput(ConcernBox.Text ?? "", HistoryBox.Text ?? "", ResultsBox.Text ?? "", imagePaths.Count))
        {
            if (!await Dialogs.ConfirmAsync(this, "حالة جديدة", "سيتم مسح تفاصيل الحالة والنتيجة الحالية من النافذة. متابعة؟")) return;
        }
        AgeBox.Text = string.Empty;
        SexBox.SelectedIndex = 0;
        ConcernBox.Text = string.Empty;
        HistoryBox.Text = string.Empty;
        ResultsBox.Text = string.Empty;
        imagePaths.Clear();
        chatHistory.Clear();
        currentRecord = null;
        SetUrgentBanner(string.Empty);
        RefreshAttachments();
        AnswerBox.Text = SafetyRules.DefaultAnswer;
    }
}
