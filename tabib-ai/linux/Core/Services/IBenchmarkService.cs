using System;
using System.Threading;
using System.Threading.Tasks;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Interface for hardware benchmarking.
/// </summary>
public interface IBenchmarkService
{
    /// <summary>
    /// Runs a quick CPU benchmark and returns a score (higher is better).
    /// </summary>
    Task<double> RunCpuBenchmarkAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs a memory bandwidth benchmark.
    /// </summary>
    Task<double> RunMemoryBenchmarkAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs inference benchmark with a small model to measure tokens/sec.
    /// </summary>
    Task<double> RunInferenceBenchmarkAsync(string modelPath, CancellationToken ct = default);

    /// <summary>
    /// Gets the last benchmark results.
    /// </summary>
    BenchmarkResult? GetLastResult();
}

/// <summary>
/// Benchmark result model.
/// </summary>
public record BenchmarkResult(
    DateTime Timestamp,
    double CpuScore,
    double MemoryBandwidthGBps,
    double InferenceTokensPerSec,
    string ModelUsed,
    TimeSpan Duration
);