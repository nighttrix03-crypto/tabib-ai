using System;
using System.Threading;
using System.Threading.Tasks;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Download progress model.
/// </summary>
public record DownloadProgress(
    string ModelKey,
    long BytesDownloaded,
    long TotalBytes,
    double Percentage,
    string Status
);

/// <summary>
/// Interface for model management (selection, download, switching).
/// </summary>
public interface IModelManager
{
    /// <summary>
    /// Gets all available model definitions.
    /// </summary>
    IReadOnlyList<ModelInfo> GetAvailableModels();

    /// <summary>
    /// Gets the recommended model for the current hardware.
    /// </summary>
    ModelInfo GetRecommendedModel(HardwareInfo hardware);

    /// <summary>
    /// Gets the recommended model for a specific use case and hardware.
    /// </summary>
    ModelInfo GetRecommendedModelForUseCase(ModelUseCase useCase, HardwareInfo hardware);

    /// <summary>
    /// Gets the currently selected model.
    /// </summary>
    ModelInfo? GetCurrentModel();

    /// <summary>
    /// Sets the current model.
    /// </summary>
    Task<bool> SetCurrentModelAsync(string modelKey, CancellationToken ct = default);

    /// <summary>
    /// Checks if a model is downloaded and ready.
    /// </summary>
    Task<bool> IsModelReadyAsync(string modelKey, CancellationToken ct = default);

    /// <summary>
    /// Downloads a model with progress reporting.
    /// </summary>
    Task DownloadModelAsync(string modelKey, IProgress<DownloadProgress> progress, CancellationToken ct = default);

    /// <summary>
    /// Gets the local path for a model.
    /// </summary>
    string GetModelPath(string modelKey);

    /// <summary>
    /// Event fired when model changes.
    /// </summary>
    event Action<ModelInfo>? OnModelChanged;
}