using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using TabibAI.Linux.Core.AI;
using TabibAI.Linux.Core.I18n;
using TabibAI.Linux.Core.Llama;
using TabibAI.Linux.Core.Models;

namespace TabibAI.Linux.Ui.Chat;

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly ILlamaEngine _llamaEngine;
    private readonly IMedicalAgent _medicalAgent;
    private readonly IChatAgent _chatAgent;
    private readonly ILocalizationService _loc;
    private string _inputText = "";
    private bool _isStreaming;
    private ChatMode _mode = ChatMode.General;
    private System.Threading.CancellationTokenSource? _cts;

    private static readonly string HistoryDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tabib-ai");
    private static readonly string HistoryFile = Path.Combine(HistoryDir, "chat-history.json");

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ScrollToBottomRequested;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<MedicalCase> Cases { get; } = new();

    public string InputText
    {
        get => _inputText;
        set
        {
            _inputText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSend));
            (SendCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool IsStreaming
    {
        get => _isStreaming;
        private set
        {
            _isStreaming = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSend));
            OnPropertyChanged(nameof(IsNotStreaming));
        }
    }

    public bool IsNotStreaming => !IsStreaming;
    public bool CanSend => !IsStreaming && !string.IsNullOrWhiteSpace(InputText);

    /// <summary>الوضع الحالي للمحادثة: عام / طبي / برمجة / ترجمة.</summary>
    public ChatMode Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModeDisplay));
            }
        }
    }

    public string ModeDisplay => Mode switch
    {
        ChatMode.Medical => "🩺 وضع التشخيص الطبي",
        ChatMode.Code => "💻 وضع البرمجة",
        ChatMode.Translate => "🌐 وضع الترجمة",
        _ => "💬 محادثة عامة",
    };

    public ICommand SendCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand NewDiagnosisCommand { get; }
    public ICommand FollowUpCommand { get; }
    public ICommand GenerateReportCommand { get; }
    public ICommand SaveCaseCommand { get; }
    public ICommand LoadCaseCommand { get; }
    public ICommand DeleteCaseCommand { get; }
    public ICommand SetGeneralModeCommand { get; }
    public ICommand SetMedicalModeCommand { get; }
    public ICommand SetCodeModeCommand { get; }
    public ICommand SetTranslateModeCommand { get; }

    public ChatViewModel(
        ILlamaEngine llamaEngine,
        IMedicalAgent medicalAgent,
        IChatAgent chatAgent,
        ILocalizationService loc)
    {
        _llamaEngine = llamaEngine;
        _medicalAgent = medicalAgent;
        _chatAgent = chatAgent;
        _loc = loc;

        SendCommand = new RelayCommand(async () => await SendAsync(), () => CanSend);
        StopCommand = new RelayCommand(Stop, () => IsStreaming);
        NewDiagnosisCommand = new RelayCommand(NewDiagnosis);
        FollowUpCommand = new RelayCommand(FollowUp);
        GenerateReportCommand = new RelayCommand(GenerateReport);
        SaveCaseCommand = new RelayCommand(() => _ = SaveHistoryAsync());
        LoadCaseCommand = new RelayCommand<MedicalCase>(LoadCase);
        DeleteCaseCommand = new RelayCommand<MedicalCase>(DeleteCase);
        SetGeneralModeCommand = new RelayCommand(() => Mode = ChatMode.General);
        SetMedicalModeCommand = new RelayCommand(() => Mode = ChatMode.Medical);
        SetCodeModeCommand = new RelayCommand(() => Mode = ChatMode.Code);
        SetTranslateModeCommand = new RelayCommand(() => Mode = ChatMode.Translate);

        _ = LoadHistoryAsync();
        if (Messages.Count == 0)
            AddAssistantMessage(GetWelcomeMessage());
    }

    private string GetWelcomeMessage() =>
        _loc["WelcomeMessage"] + "\n\n💡 المحادثة العامة جاهزة. اكتب سؤالك مباشرة، أو جرّب `/help` لعرض الأوامر، أو `/medical` للوضع الطبي.";

    // ------------------------------------------------------------------
    // الإرسال مع البث الحقيقي من النموذج
    // ------------------------------------------------------------------
    private async Task SendAsync()
    {
        var userText = InputText.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        InputText = "";

        // الأوامر الموضعية تُنفذ محلياً بدون نموذج
        if (userText.StartsWith('/'))
        {
            await HandleCommandAsync(userText);
            return;
        }

        AddUserMessage(userText);

        if (!_llamaEngine.IsInitialized)
        {
            AddAssistantMessage("⚠️ النموذج غير مهيأ بعد (جارٍ التحميل أو فشل التحميل). انتظر قليلاً ثم أعد الإرسال، وراجع الإعدادات لتحميل نموذج.");
            return;
        }

        IsStreaming = true;
        _cts = new System.Threading.CancellationTokenSource();

        var assistantMsg = new ChatMessage
        {
            IsUser = false,
            Content = "",
            IsStreaming = true,
            Mode = Mode,
            Timestamp = DateTime.Now,
        };
        Messages.Add(assistantMsg);
        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);

        var sb = new StringBuilder();
        var history = new List<ChatMessage>(Messages);
        history.RemoveAt(history.Count - 1); // استبعاد رسالة الرد الفارغة

        try
        {
            await foreach (var token in _chatAgent.StreamAsync(userText, history, Mode, _cts.Token))
            {
                sb.Append(token);
                assistantMsg.Content = sb.ToString();
                ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
            }

            var full = sb.ToString();

            // في الوضع الطبي: حوّل JSON إلى عرض منسق + استخرج الحالة
            if (Mode == ChatMode.Medical && full.TrimStart().StartsWith('{'))
            {
                var medical = _chatAgent.TryParseMedical(full);
                if (medical != null)
                {
                    Cases.Add(new MedicalCase
                    {
                        Id = Guid.NewGuid(),
                        ChiefComplaint = userText,
                        DifferentialDiagnosis = medical.Differential,
                        Plan = medical.Plan,
                        Referral = medical.Referral,
                        EmergencyFlags = medical.EmergencyFlags,
                        FollowUpQuestions = medical.FollowUpQuestions,
                        CreatedAt = DateTime.Now,
                        Messages = new List<ChatMessage>(Messages),
                    });
                    assistantMsg.Content = _chatAgent.FormatMedicalResponse(full);
                }
            }

            if (string.IsNullOrWhiteSpace(assistantMsg.Content))
                assistantMsg.Content = "(لم يُصدر النموذج رداً — جرّب إعادة الصياغة أو زيادة سياق السؤال)";
        }
        catch (OperationCanceledException)
        {
            if (sb.Length == 0)
                assistantMsg.Content = "⏹️ تم إيقاف التوليد.";
            else
                assistantMsg.Content = sb.ToString() + "\n\n⏹️ (تم الإيقاف)";
        }
        catch (Exception ex)
        {
            assistantMsg.Content = $"⚠️ خطأ أثناء التوليد: {ex.Message}";
        }
        finally
        {
            assistantMsg.IsStreaming = false;
            assistantMsg.IsComplete = true;
            IsStreaming = false;
            _cts?.Dispose();
            _cts = null;
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
            _ = SaveHistoryAsync();
        }
    }

    private async Task HandleCommandAsync(string commandLine)
    {
        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cmd = parts[0].ToLowerInvariant();

        switch (cmd)
        {
            case "/help":
            case "/مساعدة":
                AddAssistantMessage(_chatAgent.GetHelpText());
                break;

            case "/new":
            case "/clear":
            case "/جديد":
                NewDiagnosis();
                break;

            case "/general":
                Mode = ChatMode.General;
                AddAssistantMessage("💬 تم التبديل إلى **المحادثة العامة**. اسألني ما شئت.");
                break;

            case "/medical":
            case "/طبيب":
                Mode = ChatMode.Medical;
                AddAssistantMessage("🩺 تم تفعيل **الوضع الطبي**. صف الشكوى وسنُخرج تشخيصاً تفريدياً منظّماً (تذكير: لا يغني عن الطبيب).\n\nللعودة للمحادثة العامة: `/general`");
                break;

            case "/code":
                Mode = ChatMode.Code;
                AddAssistantMessage("💻 تم تفعيل **وضع البرمجة**. اكتب سؤالك أو الصق الكود.");
                break;

            case "/translate":
                Mode = ChatMode.Translate;
                AddAssistantMessage("🌐 تم تفعيل **وضع الترجمة**. أرسل النص المطلوب ترجمته.");
                break;

            case "/save":
                await SaveHistoryAsync();
                AddAssistantMessage($"💾 تم حفظ المحادثة في `{HistoryFile}`");
                break;

            case "/model":
                var info = _llamaEngine.GetModelInfo();
                AddAssistantMessage(
                    $"🤖 **النموذج الحالي**\n• الاسم: {info.Name}\n• الحالة: {(_llamaEngine.IsInitialized ? "✅ جاهز" : "⚠️ غير مهيأ")}" +
                    $"\n• الحجم: {info.SizeBytes / (1024.0 * 1024 * 1024):F2} GB" +
                    $"\n• الترميز: {info.Quantization}" +
                    $"\n• سياق السياق: {info.ContextSize} token" +
                    $"\n• GPU layers: {info.GPULayers}" +
                    $"\n• السرعة: {info.TokensPerSecond} token/ث");
                break;

            default:
                AddAssistantMessage($"❓ أمر غير معروف: `{cmd}`\n\n{_chatAgent.GetHelpText()}");
                break;
        }

        await Task.CompletedTask;
    }

    private void Stop()
    {
        try { _cts?.Cancel(); } catch { /* تجاهل */ }
    }

    private void AddUserMessage(string text)
    {
        Messages.Add(new ChatMessage { IsUser = true, Content = text, Timestamp = DateTime.Now, Mode = Mode });
        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
    }

    private void AddAssistantMessage(string text)
    {
        Messages.Add(new ChatMessage { IsUser = false, Content = text, Timestamp = DateTime.Now, Mode = Mode });
        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
    }

    // ------------------------------------------------------------------
    // أوامر سريعة
    // ------------------------------------------------------------------
    private void NewDiagnosis()
    {
        Messages.Clear();
        Mode = ChatMode.General;
        AddAssistantMessage(GetWelcomeMessage());
        _ = SaveHistoryAsync();
    }

    private void FollowUp()
    {
        if (Cases.Count > 0)
        {
            var lastCase = Cases[^1];
            if (lastCase.FollowUpQuestions.Count > 0)
            {
                InputText = lastCase.FollowUpQuestions[0];
                SendCommand.Execute(null);
            }
        }
        else
        {
            AddAssistantMessage("ℹ️ لا توجد حالات سابقة بعد. ابدأ تشخيصاً جديداً أولًا.");
        }
    }

    private void GenerateReport()
    {
        if (Cases.Count > 0)
        {
            var lastCase = Cases[^1];
            var report = _medicalAgent.GenerateReport(lastCase);
            AddAssistantMessage(report);
        }
        else
        {
            AddAssistantMessage("ℹ️ لا توجد حالة لإصدار تقرير. استخدم `/medical` ثم صف الشكوى.");
        }
    }

    private void LoadCase(MedicalCase? caseData)
    {
        if (caseData == null) return;
        Messages.Clear();
        foreach (var msg in caseData.Messages)
            Messages.Add(msg);
        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteCase(MedicalCase? caseData)
    {
        if (caseData != null)
            Cases.Remove(caseData);
    }

    // ------------------------------------------------------------------
    // حفظ/استرجاع المحادثة
    // ------------------------------------------------------------------
    private async Task SaveHistoryAsync()
    {
        try
        {
            Directory.CreateDirectory(HistoryDir);
            var snapshot = new HistorySnapshot
            {
                SavedAt = DateTime.Now,
                Mode = Mode,
                Messages = new List<ChatMessage>(Messages),
            };
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(HistoryFile, json);
        }
        catch
        {
            // الحفظ اختياري؛ لا نعطّل المحادثة بسببه
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            if (!File.Exists(HistoryFile)) return;

            var json = await File.ReadAllTextAsync(HistoryFile);
            var snapshot = JsonSerializer.Deserialize<HistorySnapshot>(json);
            if (snapshot?.Messages == null || snapshot.Messages.Count == 0) return;

            foreach (var msg in snapshot.Messages)
                Messages.Add(msg);
            Mode = snapshot.Mode;
        }
        catch
        {
            // ملف تالف → نبدأ محادثة جديدة
            Messages.Clear();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal class HistorySnapshot
{
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public ChatMode Mode { get; set; } = ChatMode.General;
    public List<ChatMessage> Messages { get; set; } = new();
}

public class RelayCommand : ICommand
{
    private readonly Func<Task>? _executeAsync;
    private readonly Action? _executeSync;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public RelayCommand(Action executeSync, Func<bool>? canExecute = null)
    {
        _executeSync = executeSync;
        _canExecute = canExecute;
    }

    public RelayCommand(Action<object?> executeSync, Func<bool>? canExecute = null)
    {
        _executeSync = () => executeSync(null);
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public event EventHandler? CanExecuteChanged;

    public async void Execute(object? parameter)
    {
        if (_executeAsync != null) await _executeAsync();
        else _executeSync?.Invoke();
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public event EventHandler? CanExecuteChanged;

    public void Execute(object? parameter)
    {
        if (parameter is T t)
            _execute(t);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
