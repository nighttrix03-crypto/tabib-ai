using Microsoft.Extensions.DependencyInjection;
using MediatR;
using TabibAI.Linux.Core.Services;
using TabibAI.Linux.Ui.Chat;

namespace TabibAI.Linux.Core.DI;

/// <summary>
/// Extension methods for registering TabibAI services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all TabibAI core services, MediatR handlers, and infrastructure.
    /// </summary>
    public static IServiceCollection AddTabibAICore(this IServiceCollection services)
    {
        // MediatR - registers all IRequestHandler, INotificationHandler in the assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));

        // Core Services
        services.AddSingleton<IModelManager, ModelManager>();
        services.AddSingleton<IHardwareDetector, HardwareDetector>();
        services.AddSingleton<IBenchmarkService, BenchmarkService>();
        services.AddSingleton<IOllamaService, OllamaService>();
        services.AddSingleton<ILlamaCppEngine, LlamaCppEngine>();
        services.AddSingleton<IModelDownloadService, ModelDownloadService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<ICaseStore, CaseStore>();
        services.AddSingleton<ISafetyRules, SafetyRules>();
        services.AddSingleton<IReportWriter, ReportWriter>();
        services.AddSingleton<IDicomImporter, DicomImporter>();

        // Settings
        services.Configure<ModelSettings>(options =>
        {
            options.ModelsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TabibAI", "models");
        });

        return services;
    }

    /// <summary>
    /// Registers UI-related services.
    /// </summary>
    public static IServiceCollection AddTabibAIUI(this IServiceCollection services)
    {
        services.AddTransient<MainWindow>();
        services.AddTransient<AppSelectionWindow>();
        return services;
    }
}

/// <summary>
/// Configuration settings for model management.
/// </summary>
public class ModelSettings
{
    public string ModelsDirectory { get; set; } = "";
    public string PreferredBackend { get; set; } = "auto"; // "auto", "ollama", "llamacpp"
    public bool AutoDownloadModels { get; set; } = true;
    public int MaxConcurrentDownloads { get; set; } = 1;
}