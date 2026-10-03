using Avalonia.Controls;
using TabibAI.Linux.Core;
using TabibAI.Linux.Ui;

namespace TabibAI.Linux;

public partial class MainWindow : Window
{
    private string SelectedSex() =>
        SexBox.SelectedIndex <= 0 ? "" : (SexBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

    /// <summary>يقرأ الصور ويحوّل شرائح DICOM إلى PNG بصيغة base64 للنموذج.</summary>
    private (List<string> Images, List<string> Errors) GetImageData()
    {
        var images = new List<string>();
        var errors = new List<string>();
        foreach (var path in imagePaths.Take(SafetyRules.MaxAttachments))
        {
            try
            {
                byte[] bytes = DicomImporter.IsDicomPath(path)
                    ? DicomImporter.ToPng(path)
                    : File.ReadAllBytes(path);
                images.Add(Convert.ToBase64String(bytes));
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(path)}: {SafetyRules.Shorten(ex.Message)}");
            }
        }
        return (images, errors);
    }

    private async Task AnalyzeAsync()
    {
        if (busy) return;
        if (!modelReady)
        {
            await Dialogs.InfoAsync(this, "إعداد النموذج المحلي",
                "النموذج المحلي غير جاهز. اضغط «إعداد النموذج» لتنزيل MedGemma عبر Ollama، ثم أعد المحاولة.");
            return;
        }

        var concern = ConcernBox.Text ?? "";
        var history = HistoryBox.Text ?? "";
        var results = ResultsBox.Text ?? "";
        if (!SafetyRules.HasCaseInput(concern, history, results, imagePaths.Count))
        {
            await Dialogs.InfoAsync(this, "بيانات الحالة", "أضف وصفاً أو نتيجة فحص أو صورة قبل التحليل.");
            return;
        }
        var ageError = SafetyRules.ValidateAge(AgeBox.Text ?? "");
        if (ageError is not null)
        {
            await Dialogs.InfoAsync(this, "العمر", ageError);
            return;
        }

        var urgent = SafetyRules.DetectUrgentWarning(concern, history, results);
        SetUrgentBanner(urgent);
        if (urgent.Length > 0)
        {
            var proceed = await Dialogs.ConfirmAsync(this, "تنبيه عاجل",
                $"قد تتضمن المعلومات علامة خطر: {urgent}\n\n{SafetyRules.EmergencyMessage}");
            if (!proceed) return;
        }
        if (!await Dialogs.ConfirmAsync(this, "موافقة التحليل المحلي", SafetyRules.LocalConsentMessage)) return;

        SetBusy(true);
        AnswerBox.Text = "يجري التحليل محلياً…";
        try
        {
            var (images, errors) = GetImageData();
            var prompt = ClinicalPrompts.ComposeCasePrompt(category, AgeBox.Text ?? "", SelectedSex(),
                concern, history, results, images.Count);
            chatHistory.Clear();
            chatHistory.Add(new ChatTurn("user", prompt, images));

            var turns = new List<ChatTurn> { new("system", SystemPrompts.For(category.Key, images.Count), new List<string>()) };
            turns.AddRange(chatHistory);

            var answer = await ollama.ChatAsync(turns);
            chatHistory.Add(new ChatTurn("assistant", answer, new List<string>()));
            AnswerBox.Text = errors.Count == 0
                ? answer
                : answer + "\n\n— ملاحظة على المرفقات —\n" + string.Join("\n", errors);

            currentRecord = new CaseRecord
            {
                Category = category.Key,
                Age = AgeBox.Text ?? "",
                Sex = SelectedSex(),
                Concern = concern,
                History = history,
                Results = results,
                Answer = answer,
                ImagePaths = imagePaths.ToList(),
                Turns = chatHistory.Select(t => new CaseTurn { Role = t.Role, Content = t.Content }).ToList()
            };
            CaseStore.Save(currentRecord);
        }
        catch (Exception ex)
        {
            AnswerBox.Text = "تعذّر التحليل المحلي. تحقق من تشغيل Ollama ومن وجود MedGemma.\n\n" + SafetyRules.Shorten(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AskFollowUpAsync()
    {
        if (busy) return;
        var question = (FollowUpBox.Text ?? "").Trim();
        if (question.Length == 0) return;
        if (chatHistory.Count == 0)
        {
            await Dialogs.InfoAsync(this, "المحادثة", "حلّل الحالة أولاً لبدء محادثة متابعة.");
            return;
        }
        FollowUpBox.Text = string.Empty;
        SetBusy(true);
        try
        {
            chatHistory.Add(new ChatTurn("user", question, new List<string>()));
            var turns = new List<ChatTurn> { new("system", SystemPrompts.For(category.Key, imagePaths.Count), new List<string>()) };
            turns.AddRange(chatHistory.TakeLast(12));
            var answer = await ollama.ChatAsync(turns);
            chatHistory.Add(new ChatTurn("assistant", answer, new List<string>()));
            AnswerBox.Text = AnswerBox.Text + $"\n\n— سؤالك —\n{question}\n\n— متابعة MedGemma —\n{answer}";
            if (currentRecord is not null)
            {
                currentRecord.Answer = AnswerBox.Text;
                currentRecord.Turns = chatHistory.Select(t => new CaseTurn { Role = t.Role, Content = t.Content }).ToList();
                CaseStore.Save(currentRecord);
            }
        }
        catch (Exception ex)
        {
            AnswerBox.Text = AnswerBox.Text + "\n\nتعذّرت المتابعة: " + SafetyRules.Shorten(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }
}
