using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TabibAI.Linux.Core;
using TabibAI.Linux.Ui;

namespace TabibAI.Linux;

public partial class MainWindow : Window
{
    private async void OnRefreshClick(object? sender, RoutedEventArgs e) => await CheckModelAsync();

    private async void OnAnalyzeClick(object? sender, RoutedEventArgs e) => await AnalyzeAsync();

    private async void OnAskClick(object? sender, RoutedEventArgs e) => await AskFollowUpAsync();

    private async void OnFollowUpKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await AskFollowUpAsync();
    }

    private async void OnHistoryClick(object? sender, RoutedEventArgs e)
    {
        var window = new HistoryWindow(CaseStore.LoadAll());
        await window.ShowDialog(this);
        if (window.SelectedRecord is not { } record) return;
        AgeBox.Text = record.Age;
        SexBox.SelectedIndex = record.Sex switch { "أنثى" => 1, "ذكر" => 2, _ => 0 };
        SelectCategory(ClinicalPrompts.ByKey(record.Category));
        ConcernBox.Text = record.Concern;
        HistoryBox.Text = record.History;
        ResultsBox.Text = record.Results;
        AnswerBox.Text = record.Answer;
        currentRecord = record;
        SetUrgentBanner(SafetyRules.DetectUrgentWarning(record.Concern, record.History, record.Results));
    }

    private async void OnSetupClick(object? sender, RoutedEventArgs e) => await SetupAsync();

    private async Task SetupAsync()
    {
        if (await ollama.IsModelReadyAsync())
        {
            modelReady = true;
            SetStatus("● MedGemma جاهز محلياً", true);
            await Dialogs.InfoAsync(this, "النموذج المحلي", "MedGemma جاهز محلياً ويمكن البدء بالتحليل.");
            return;
        }
        var agreed = await Dialogs.ConfirmAsync(this, "إعداد النموذج المحلي",
            "سيُستخدم Ollama على هذا الجهاز لتنزيل نموذج MedGemma (نحو 3.3 GB) من سجلات Ollama الرسمية.\n\n"
            + "MedGemma من Google HAI-DEF، ومتابعة التنزيل تعني موافقتك على شروط الاستخدام المعلنة من Google. "
            + "ستُفتح صفحة الشروط في المتصفح.\n\n"
            + "الأوزان ليست مضمنة في هذا التطبيق. هل تريد المتابعة؟");
        if (!agreed) return;
        ReportWriter.Open("https://developers.google.com/health-ai-developer-foundations/terms");
        SetStatus("⏳ جارٍ تنزيل MedGemma…", false);
        try
        {
            var progress = new Progress<string>(text => SetStatus(text, false));
            await ollama.PullAsync(progress);
        }
        catch (Exception ex)
        {
            await CheckModelAsync();
            await Dialogs.InfoAsync(this, "Ollama",
                "تعذّر تنزيل النموذج. تأكد من تثبيت Ollama وتشغيله، ومن اتصال الإنترنت.\n\n" + SafetyRules.Shorten(ex.Message));
            return;
        }
        await CheckModelAsync();
        await Dialogs.InfoAsync(this, "إعداد النموذج", modelReady
            ? "تم تنزيل MedGemma. أصبح التحليل يعمل محلياً على هذا الجهاز."
            : "لم يظهر النموذج ضمن النماذج المثبتة. تحقق من Ollama.");
    }
}
