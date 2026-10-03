using System.Collections.Generic;
using System.Threading;

namespace TabibAI.Linux.Core.Llama;

public interface ILlamaEngine : IDisposable
{
    bool IsInitialized { get; }
    string CurrentModel { get; }
    Task InitializeAsync(string modelPath, int contextSize = 4096, int gpuLayers = -1, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamInferenceAsync(string prompt, CancellationToken ct = default);
    Task<string> InferAsync(string prompt, CancellationToken ct = default);
    ModelInfo GetModelInfo();
}

public record ModelInfo(
    string Name,
    string Path,
    long SizeBytes,
    string Quantization,
    int ContextSize,
    int GPULayers,
    double TokensPerSecond
);
