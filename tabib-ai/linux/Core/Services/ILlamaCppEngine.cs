using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Llama.cpp model parameters.
/// </summary>
public record LlamaModelParams(
    int ContextLength = 8192,
    int GpuLayers = -1, // -1 = all
    int Threads = 0,    // 0 = auto
    float Temperature = 0.15f,
    float TopP = 0.95f,
    int TopK = 40,
    float RepeatPenalty = 1.1f,
    bool UseMmap = true,
    bool UseMlock = false,
    bool FlashAttention = true
);

/// <summary>
/// Interface for llama.cpp engine.
/// </summary>
public interface ILlamaCppEngine : IDisposable
{
    string CurrentModel { get; }
    bool IsLoaded { get; }
    event Action<string>? OnTokenReceived;
    event Action? OnStreamCompleted;
    event Action<string>? OnError;

    Task<bool> LoadModelAsync(string modelPath, LlamaModelParams? parameters = null, CancellationToken ct = default);
    Task StartStreamAsync(IReadOnlyList<ChatTurn> messages, CancellationToken ct = default);
    void StopStream();
}