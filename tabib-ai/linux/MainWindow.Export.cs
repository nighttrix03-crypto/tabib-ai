using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TabibAI.Linux.Core;
using TabibAI.Linux.Ui;

namespace TabibAI.Linux;

public partial class MainWindow : Window
{
    private async void OnSaveTextClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AnswerBox.Text)) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "حفظ نتيجة المراجعة",
            SuggestedFileName = $"TabibAI-{DateTime.Now:yyyy-MM-dd-HHmm}.txt",
            DefaultExtension = "txt",
            FileTypeChoices = new List<FilePickerFileType> { new FilePickerFileType("تقرير نصي") { Patterns = new[] { "*.txt" } } }
        });
        var path = file?.TryGetLocalPath();
        if (string.IsNullOrEmpty(path)) return;
        ReportWriter.WriteText(path, AnswerBox.Text ?? "");
        await Dialogs.InfoAsync(this, "حفظ التقرير", "تم حفظ التقرير في:\n" + path);
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AnswerBox.Text)) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "تصدير التقرير",
            SuggestedFileName = $"TabibAI-{DateTime.Now:yyyy-MM-dd-HHmm}.html",
            DefaultExtension = "html",
            FileTypeChoices = new List<FilePickerFileType> { new FilePickerFileType("صفحة HTML") { Patterns = new[] { "*.html" } } }
        });
        var path = file?.TryGetLocalPath();
        if (string.IsNullOrEmpty(path)) return;
        var html = ReportWriter.BuildHtml("تقرير طبيب AI — " + category.Label,
            $"أُنشئ محلياً في {DateTime.Now:yyyy-MM-dd HH:mm}",
            AnswerBox.Text ?? "", SafetyRules.DiagnosisDisclaimer);
        ReportWriter.WriteHtml(path, html);
        var pdfPath = Path.ChangeExtension(path, ".pdf");
        var pdf = await ReportWriter.TryHtmlToPdfAsync(path, pdfPath);
        await Dialogs.InfoAsync(this, "تصدير التقرير",
            pdf ? "تم إنشاء HTML وPDF:\n" + path + "\n" + pdfPath
                : "تم إنشاء HTML:\n" + path + "\n\nلم يُنشأ PDF (يتطلب LibreOffice مثبتاً).");
        ReportWriter.Open(pdf ? pdfPath : path);
    }
}
