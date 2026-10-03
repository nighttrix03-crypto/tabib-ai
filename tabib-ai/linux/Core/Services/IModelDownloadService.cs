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
/// Interface for model download service.
/// </summary>
public interface IModelDownloadService
{
    Task DownloadFromHuggingFaceAsync(string repoId, string fileName, string destinationPath, IProgress<DownloadProgress> progress, CancellationToken ct = default);
    Task<bool> VerifyModelAsync(string filePath, string expectedSha256, CancellationToken ct = default);
    string GetModelsDirectory();
}