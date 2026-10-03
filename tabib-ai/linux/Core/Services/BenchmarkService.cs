using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TabibAI.Linux.Core.Services;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Hardware benchmarking service implementation.
/// </summary>
public sealed class BenchmarkService : IBenchmarkService
{
    private BenchmarkResult? _lastResult;
    private readonly IHardwareDetector _hardwareDetector;

    public BenchmarkService(IHardwareDetector hardwareDetector)
    {
        _hardwareDetector = hardwareDetector;
    }

    public async Task<double> RunCpuBenchmarkAsync(CancellationToken ct = default)
    {
        const int iterations = 5_000_000;
        var sw = Stopwatch.StartNew();
        double result = 0;

        await Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                if (ct.IsCancellationRequested) break;
                result += Math.Sin(i) * Math.Cos(i);
                result += Math.Sqrt(i + 1);
            }
        }, ct);

        sw.Stop();
        var score = iterations / sw.Elapsed.TotalSeconds / 1_000_000.0 * 100.0;
        return score;
    }

    public async Task<double> RunMemoryBenchmarkAsync(CancellationToken ct = default)
    {
        const int arraySize = 100_000_000; // ~800MB
        var array = new byte[arraySize];
        var sw = Stopwatch.StartNew();

        await Task.Run(() =>
        {
            // Sequential write
            for (int i = 0; i < arraySize; i += 4096)
            {
                if (ct.IsCancellationRequested) break;
                array[i] = 0xFF;
            }
            // Sequential read
            long sum = 0;
            for (int i = 0; i < arraySize; i += 4096)
            {
                if (ct.IsCancellationRequested) break;
                sum += array[i];
            }
        }, ct);

        sw.Stop();
        var gb = arraySize / (1024.0 * 1024.0 * 1024.0);
        var bandwidth = gb / sw.Elapsed.TotalSeconds;
        return bandwidth;
    }

    public async Task<double> RunInferenceBenchmarkAsync(string modelPath, CancellationToken ct = default)
    {
        // This would use llama.cpp to run a quick inference benchmark
        // For now, return a simulated score based on CPU
        await Task.Delay(100, ct);
        var cpuScore = await RunCpuBenchmarkAsync(ct);
        return cpuScore * 10; // Rough tokens/sec estimate
    }

    public BenchmarkResult? GetLastResult() => _lastResult;

    public async Task<BenchmarkResult> RunFullBenchmarkAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var cpuScore = await RunCpuBenchmarkAsync(ct);
        var memoryBandwidth = await RunMemoryBenchmarkAsync(ct);
        var inferenceScore = await RunInferenceBenchmarkAsync("", ct);
        var duration = DateTime.UtcNow - startTime;

        _lastResult = new BenchmarkResult(
            DateTime.UtcNow,
            cpuScore,
            memoryBandwidth,
            inferenceScore,
            "benchmark",
            duration);

        return _lastResult;
    }
}