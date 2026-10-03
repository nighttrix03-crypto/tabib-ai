using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Chat turn for Ollama.
/// </summary>
public record ChatTurn(string Role, string Content, List<string> Images);

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
/// Interface for Ollama service.
/// </summary>
public interface IOllamaService
{
    string BaseUrl { get; }
    string CurrentModel { get; }

    Task<bool> IsServerRunningAsync(CancellationToken ct = default);
    Task<bool> IsModelReadyAsync(string modelName, CancellationToken ct = default);
    Task<string> ChatAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct = default);
    Task StartStreamAsync(IReadOnlyList<ChatTurn> messages, Action<string> onToken, CancellationToken ct = default);
    Task PullModelAsync(string modelName, IProgress<DownloadProgress> progress, CancellationToken ct = default);
    Task<List<string>> ListModelsAsync(CancellationToken ct = default);
    bool SetModel(string modelName);
}