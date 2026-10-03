using System;
using TabibAI.Linux.Core.Models;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Interface for hardware detection and system information.
/// </summary>
public interface IHardwareDetector
{
    /// <summary>
    /// Gets detailed hardware information.
    /// </summary>
    HardwareInfo GetHardwareInfo();

    /// <summary>
    /// Gets the total system RAM in MB.
    /// </summary>
    long GetTotalRamMB();

    /// <summary>
    /// Gets the available system RAM in MB.
    /// </summary>
    long GetAvailableRamMB();

    /// <summary>
    /// Gets GPU information (VRAM, compute capability, etc.).
    /// </summary>
    GpuInfo? GetGpuInfo();

    /// <summary>
    /// Checks if AVX2 is supported.
    /// </summary>
    bool IsAvx2Supported();

    /// <summary>
    /// Checks if AVX512 is supported.
    /// </summary>
    bool IsAvx512Supported();

    /// <summary>
    /// Gets CPU core count.
    /// </summary>
    int GetCpuCoreCount();
}

/// <summary>
/// Hardware information model.
/// </summary>
public record HardwareInfo(
    string OsName,
    string OsVersion,
    string Architecture,
    long TotalRamMB,
    long AvailableRamMB,
    int CpuCores,
    bool HasAvx2,
    bool HasAvx512,
    GpuInfo? Gpu,
    double CpuBenchmarkScore
);

/// <summary>
/// GPU information model.
/// </summary>
public record GpuInfo(
    string Name,
    long VramMB,
    string DriverVersion,
    bool IsCudaSupported,
    bool IsVulkanSupported,
    bool IsMetalSupported,
    int ComputeCapabilityMajor,
    int ComputeCapabilityMinor
);