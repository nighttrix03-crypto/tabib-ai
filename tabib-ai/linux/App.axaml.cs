using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TabibAI.Linux.Core.AI;
using TabibAI.Linux.Core.I18n;
using TabibAI.Linux.Core.Llama;
using TabibAI.Linux.Ui.Chat;

namespace TabibAI.Linux;

public sealed class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    private IServiceProvider? _serviceProvider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;

        var loc = _serviceProvider.GetRequiredService<ILocalizationService>();
        loc.CultureChanged += (s, e) => UpdateFlowDirection();
        UpdateFlowDirection();

        _ = InitializeModelAsync();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<AppViewModel>();
            desktop.MainWindow = mainWindow;
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<TabibAI.Linux.Core.Llama.ILlamaEngine, TabibAI.Linux.Core.Llama.LlamaEngine>();
        services.AddSingleton<ModelManager>();
        services.AddSingleton<IMedicalAgent, MedicalAgent>();
        services.AddSingleton<IChatAgent, ChatAgent>();

        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<AppViewModel>();
    }

    private async Task InitializeModelAsync()
    {
        try
        {
            var modelManager = _serviceProvider!.GetRequiredService<ModelManager>();
            var engine = _serviceProvider!.GetRequiredService<TabibAI.Linux.Core.Llama.ILlamaEngine>();

            var recommended = modelManager.RecommendModel();
            var progress = new Progress<(int percent, string status)>(p =>
                System.Diagnostics.Debug.WriteLine($"Model: {p.percent}% - {p.status}"));

            var modelPath = await modelManager.EnsureModelAsync(recommended, progress);
            await engine.InitializeAsync(modelPath, contextSize: 4096, gpuLayers: -1);

            System.Diagnostics.Debug.WriteLine($"Model loaded: {engine.CurrentModel}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Model init failed: {ex.Message}");
        }
    }

    private void UpdateFlowDirection()
    {
        var loc = _serviceProvider?.GetService<ILocalizationService>();
        if (loc != null)
        {
            var isRtl = loc.CurrentCulture.TwoLetterISOLanguageName == "ar";
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow is Window w)
                    w.FlowDirection = isRtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;
            }
        }
    }

    public static void SwitchTheme(Avalonia.Styling.ThemeVariant theme)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is Window w)
                w.RequestedThemeVariant = theme;
        }
    }
}
