using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TabibAI.Linux.Core.Services;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Hardware detector implementation for Linux, Windows, and macOS.
/// </summary>
public sealed class HardwareDetector : IHardwareDetector
{
    private HardwareInfo? _cachedInfo;
    private readonly object _lock = new();

    public HardwareInfo GetHardwareInfo()
    {
        if (_cachedInfo != null)
            return _cachedInfo;

        lock (_lock)
        {
            if (_cachedInfo != null)
                return _cachedInfo;

            var osName = GetOsName();
            var osVersion = GetOsVersion();
            var architecture = RuntimeInformation.OSArchitecture.ToString();
            var totalRam = GetTotalRamMB();
            var availableRam = GetAvailableRamMB();
            var cpuCores = GetCpuCoreCount();
            var hasAvx2 = IsAvx2Supported();
            var hasAvx512 = IsAvx512Supported();
            var gpu = GetGpuInfo();
            var cpuScore = RunCpuBenchmarkSync();

            _cachedInfo = new HardwareInfo(
                osName, osVersion, architecture,
                totalRam, availableRam, cpuCores,
                hasAvx2, hasAvx512, gpu, cpuScore);

            return _cachedInfo;
        }
    }

    public long GetTotalRamMB()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var memInfo = File.ReadAllText("/proc/meminfo");
                foreach (var line in memInfo.Split('\n'))
                {
                    if (line.StartsWith("MemTotal:"))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out long kb))
                            return kb / 1024;
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsTotalRamMB();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacTotalRamMB();
            }
        }
        catch { }
        return 8192; // Default 8GB
    }

    public long GetAvailableRamMB()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var memInfo = File.ReadAllText("/proc/meminfo");
                long availableKb = 0;
                foreach (var line in memInfo.Split('\n'))
                {
                    if (line.StartsWith("MemAvailable:"))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out long kb))
                            availableKb = kb;
                    }
                }
                if (availableKb > 0) return availableKb / 1024;

                long free = 0, buffers = 0, cached = 0;
                foreach (var line in memInfo.Split('\n'))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;
                    if (line.StartsWith("MemFree:")) long.TryParse(parts[1], out free);
                    else if (line.StartsWith("Buffers:")) long.TryParse(parts[1], out buffers);
                    else if (line.StartsWith("Cached:")) long.TryParse(parts[1], out cached);
                }
                return (free + buffers + cached) / 1024;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsAvailableRamMB();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacAvailableRamMB();
            }
        }
        catch { }
        return 4096; // Default 4GB available
    }

    public GpuInfo? GetGpuInfo()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetLinuxGpuInfo();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsGpuInfo();
        }
        catch { }
        return null;
    }

    public bool IsAvx2Supported()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var cpuInfo = File.ReadAllText("/proc/cpuinfo");
                return cpuInfo.Contains("avx2", StringComparison.OrdinalIgnoreCase);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true; // Most modern x64 CPUs support AVX2
            }
        }
        catch { }
        return false;
    }

    public bool IsAvx512Supported()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var cpuInfo = File.ReadAllText("/proc/cpuinfo");
                return cpuInfo.Contains("avx512", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { }
        return false;
    }

    public int GetCpuCoreCount() => Environment.ProcessorCount;

    private string GetOsName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                var osRelease = File.ReadAllText("/etc/os-release");
                foreach (var line in osRelease.Split('\n'))
                {
                    if (line.StartsWith("PRETTY_NAME=")) return line.Substring(13).Trim('"');
                    if (line.StartsWith("NAME=")) return line.Substring(5).Trim('"');
                }
            }
            catch { }
            return "Linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macOS";
        return "Unknown";
    }

    private string GetOsVersion()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                var osRelease = File.ReadAllText("/etc/os-release");
                foreach (var line in osRelease.Split('\n'))
                    if (line.StartsWith("VERSION_ID=")) return line.Substring(11).Trim('"');
            }
            catch { }
        }
        return Environment.OSVersion.Version.ToString();
    }

    private long GetWindowsTotalRamMB()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref memStatus)) return (long)(memStatus.ullTotalPhys / (1024 * 1024));
        }
        catch { }
        return 8192;
    }

    private long GetWindowsAvailableRamMB()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (GlobalMemoryStatusEx(ref memStatus)) return (long)(memStatus.ullAvailPhys / (1024 * 1024));
        }
        catch { }
        return 4096;
    }

    private long GetMacTotalRamMB()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo { FileName = "sysctl", Arguments = "-n hw.memsize", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
            process?.WaitForExit();
            var output = process?.StandardOutput.ReadToEnd().Trim();
            if (long.TryParse(output, out long bytes)) return bytes / (1024 * 1024);
        }
        catch { }
        return 8192;
    }

    private long GetMacAvailableRamMB()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo { FileName = "vm_stat", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
            process?.WaitForExit();
            var output = process?.StandardOutput.ReadToEnd();
            if (!string.IsNullOrEmpty(output))
            {
                long freePages = 0;
                foreach (var line in output.Split('\n'))
                    if (line.Contains("Pages free"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length == 2 && long.TryParse(parts[1].Trim().Replace(".", ""), out long pages)) freePages = pages;
                    }
                return (freePages * 4096) / (1024 * 1024);
            }
        }
        catch { }
        return 4096;
    }
private GpuInfo? GetLinuxGpuInfo()
    {
        try
        {
            var nvidia = Process.Start(new ProcessStartInfo { FileName = "nvidia-smi", Arguments = "--query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
            nvidia?.WaitForExit(5000);
            var output = nvidia?.StandardOutput.ReadToEnd().Trim();
            if (!string.IsNullOrEmpty(output))
            {
                var parts = output.Split(',');
                if (parts.Length >= 3) return new GpuInfo(parts[0].Trim(), long.Parse(parts[1].Trim()), parts[2].Trim(), true, true, false, 0, 0);
            }
            var lspci = Process.Start(new ProcessStartInfo { FileName = "lspci", Arguments = "-nn | grep -i vga", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
            lspci?.WaitForExit();
            var lspciOutput = lspci?.StandardOutput.ReadToEnd().Trim();
            if (!string.IsNullOrEmpty(lspciOutput)) return new GpuInfo(lspciOutput, 0, "", false, true, false, 0, 0);
        }
        catch { }
        return null;
    }

    private GpuInfo? GetWindowsGpuInfo()
    {
        try
        {
            var psi = new ProcessStartInfo { FileName = "powershell", Arguments = "-Command \"Get-CimInstance Win32_VideoController | Select-Object Name, AdapterRAM, DriverVersion | ConvertTo-Json\"", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            var process = Process.Start(psi);
            process?.WaitForExit(5000);
            var output = process?.StandardOutput.ReadToEnd().Trim();
            if (!string.IsNullOrEmpty(output))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(output);
                var root = doc.RootElement;
                if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var gpu = root[0];
                    return new GpuInfo(gpu.GetProperty("Name").GetString() ?? "Unknown", gpu.TryGetProperty("AdapterRAM", out var ram) && ram.ValueKind != System.Text.Json.JsonValueKind.Null ? ram.GetInt64() / (1024 * 1024) : 0, gpu.GetProperty("DriverVersion").GetString() ?? "", false, true, false, 0, 0);
                }
            }
        }
        catch { }
        return null;
    }

    private double RunCpuBenchmarkSync()
    {
        const int iterations = 1000000;
        var sw = Stopwatch.StartNew();
        double result = 0;
        for (int i = 0; i < iterations; i++) { result += Math.Sin(i) * Math.Cos(i); result += Math.Sqrt(i + 1); }
        sw.Stop();
        return iterations / sw.Elapsed.TotalSeconds / 1_000_000.0 * 100.0;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}