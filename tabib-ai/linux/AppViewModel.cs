using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using TabibAI.Linux.Core.I18n;
using TabibAI.Linux.Core.Llama;
using TabibAI.Linux.Ui.Chat;

namespace TabibAI.Linux;

public class AppViewModel : INotifyPropertyChanged
{
    private readonly ILocalizationService _loc;
    private readonly ModelManager _modelManager;
    private ThemeVariant _theme = ThemeVariant.Light;
    private ModelType _selectedModel = ModelType.Gemma2_2B;
    private bool _isModelDownloading;
    private int _modelDownloadProgress;
    private string _modelStatus = "جاهز";

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppViewModel(ILocalizationService loc, ModelManager modelManager)
    {
        _loc = loc;
        _modelManager = modelManager;
        
        // Initialize ChatViewModel from DI
        ChatViewModel = App.Services!.GetRequiredService<ChatViewModel>();
    }

    public ChatViewModel ChatViewModel { get; }

    public ThemeVariant CurrentTheme
    {
        get => _theme;
        set { _theme = value; OnPropertyChanged(); App.SwitchTheme(value); }
    }

    public IReadOnlyList<ThemeOption> AvailableThemes { get; } = new[]
    {
        new ThemeOption("☀️ فاتح (Light)", ThemeVariant.Light),
        new ThemeOption("🌙 داكن (Dark)", ThemeVariant.Dark),
    };

    public ThemeOption? SelectedTheme
    {
        get => AvailableThemes.FirstOrDefault(t => t.Variant == _theme) ?? AvailableThemes.First();
        // Defensive null coalescing: FirstOrDefault(t => t.Variant == _theme) ?? AvailableThemes.First()
        // This ensures AvailableThemes.First() is called only when FirstOrDefault returns null, preventing potential null reference errors
        set
        {
            if (value == null) return;
            _theme = value.Variant;
            OnPropertyChanged();
            App.SwitchTheme(value.Variant);
        }
    }

    public IReadOnlyList<CultureInfo> AvailableCultures { get; } = new[] { new CultureInfo("ar"), new CultureInfo("en"), new CultureInfo("fr") };

    public CultureInfo CurrentCulture
    {
        get => _loc.CurrentCulture;
        set { _loc.SetCulture(value); OnPropertyChanged(); }
    }

    public IReadOnlyList<ModelOption> AvailableModels { get; } = new[]
    {
        new ModelOption("Gemma2-2B — 1.6 GB (خفيف)", ModelType.Gemma2_2B),
        new ModelOption("Meditron-7B — 4 GB (طبي)", ModelType.Meditron7B),
        new ModelOption("Llama3-8B — 4.7 GB (قوي)", ModelType.Llama3_8B),
    };

    public ModelOption? SelectedModelOption
    {
        get => AvailableModels.FirstOrDefault(m => m.Value == _selectedModel) ?? AvailableModels.FirstOrDefault() ?? new ModelOption("Default", ModelType.Gemma2_2B);
        // Defensive null coalescing: FirstOrDefault(m => m.Value == _selectedModel) ?? AvailableModels.FirstOrDefault() ?? new ModelOption("Default", ModelType.Gemma2_2B)
        // This ensures that if AvailableModels is empty, we provide a robust fallback that prevents IndexOutOfRangeException while preserving the original model's behavior
        set
        {
            if (value == null) return;
            _selectedModel = value.Value;
            OnPropertyChanged();
        }
    }

    public ModelType SelectedModel
    {
        get => _selectedModel;
        set { _selectedModel = value; OnPropertyChanged(); }
    }

    public bool IsModelDownloading
    {
        get => _isModelDownloading;
        set { _isModelDownloading = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanDownloadModel)); }
    }

    public int ModelDownloadProgress
    {
        get => _modelDownloadProgress;
        set { _modelDownloadProgress = value; OnPropertyChanged(); }
    }

    public string ModelStatus
    {
        get => _modelStatus;
        set { _modelStatus = value; OnPropertyChanged(); }
    }

    public bool CanDownloadModel => !IsModelDownloading;

    public System.Windows.Input.ICommand DownloadModelCommand => new RelayCommand(async () => await DownloadModelAsync());

    private async Task DownloadModelAsync()
    {
        IsModelDownloading = true;
        ModelDownloadProgress = 0;
        ModelStatus = _loc["Downloading..."];

        try
        {
            var progress = new Progress<(int percent, string status)>(p =>
            {
                ModelDownloadProgress = p.percent;
                ModelStatus = p.status;
            });

            var path = await _modelManager.EnsureModelAsync(SelectedModel, progress);
            ModelStatus = _loc["ModelReady"] + $": {Path.GetFileName(path)}";

            // Reinitialize engine with new model
            var engine = App.Services!.GetRequiredService<ILlamaEngine>();
            await engine.InitializeAsync(path, contextSize: 4096, gpuLayers: -1);
        }
        catch (Exception ex)
        {
            ModelStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsModelDownloading = false;
        }
    }

    public string SystemInfo
    {
        get
        {
            var ram = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024 / 1024;
            return $"OS: {Environment.OSVersion}\n.NET: {Environment.Version}\nRAM: ~{ram} GB\nCPU: {Environment.ProcessorCount} cores";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public class RelayCommand : System.Windows.Input.ICommand
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

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public event EventHandler? CanExecuteChanged;

    public async void Execute(object? parameter)
    {
        if (_executeAsync != null) await _executeAsync();
        else _executeSync?.Invoke();
    }
}

/// <summary>خيار سمة للمعرض (اسم عربي + السمة).</summary>
public sealed record ThemeOption(string DisplayName, ThemeVariant Variant);

/// <summary>خيار نموذج ذكاء اصطناعي (اسم معروض + النوع).</summary>
public sealed record ModelOption(string DisplayName, ModelType Value);
