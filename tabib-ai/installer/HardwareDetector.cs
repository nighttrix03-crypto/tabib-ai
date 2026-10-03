using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace TabibAIInstaller;

/// <summary>
/// مواصفات الجهاز المكتشفة
/// </summary>
public sealed class HardwareSpecs
{
    public long TotalRAMBytes { get; set; }
    public long AvailableRAMBytes { get; set; }
    public double TotalRAM_GB => TotalRAMBytes / (1024.0 * 1024 * 1024);
    public double AvailableRAM_GB => AvailableRAMBytes / (1024.0 * 1024 * 1024);
    public bool HasDedicatedGPU { get; set; }
    public string GPUName { get; set; } = "";
    public long GPU_VRAM_Bytes { get; set; }
    public double GPU_VRAM_GB => GPU_VRAM_Bytes / (1024.0 * 1024 * 1024);
    public long FreeDiskSpaceBytes { get; set; }
    public double FreeDiskSpace_GB => FreeDiskSpaceBytes / (1024.0 * 1024 * 1024);
    public int CPU_Cores { get; set; }
    public string CPU_Name { get; set; } = "";
    public bool Is64BitOS { get; set; }
    public string OS_Version { get; set; } = "";

    public override string ToString()
    {
        return $"RAM: {TotalRAM_GB:F1} GB (متاح: {AvailableRAM_GB:F1} GB) | " +
               $"GPU: {(HasDedicatedGPU ? GPUName + $" ({GPU_VRAM_GB:F1} GB VRAM)" : "لا يوجد GPU مخصص")} | " +
               $"قرص: {FreeDiskSpace_GB:F1} GB متاح | " +
               $"CPU: {CPU_Cores} أنوية ({CPU_Name}) | " +
               $"OS: {OS_Version} {(Is64BitOS ? "x64" : "x86")}";
    }
}

/// <summary>
/// كاشف مواصفات العتاد للويندوز
/// </summary>
public static class HardwareDetector
{
    public static HardwareSpecs Detect()
    {
        var specs = new HardwareSpecs();

        try
        {
            // 1. الذاكرة (RAM)
            DetectRAM(specs);

            // 2. المعالج
            DetectCPU(specs);

            // 3. GPU و VRAM
            DetectGPU(specs);

            // 4. مساحة القرص
            DetectDiskSpace(specs);

            // 5. معلومات النظام
            specs.Is64BitOS = Environment.Is64BitOperatingSystem;
            specs.OS_Version = Environment.OSVersion.Version.ToString();
        }
        catch (Exception ex)
        {
            // قيم افتراضية آمنة في حالة الفشل
            specs.TotalRAMBytes = 8L * 1024 * 1024 * 1024; // 8 GB
            specs.AvailableRAMBytes = 4L * 1024 * 1024 * 1024;
            specs.FreeDiskSpaceBytes = 10L * 1024 * 1024 * 1024; // 10 GB
            specs.CPU_Cores = Environment.ProcessorCount;
            Console.WriteLine($"⚠️ تحذير في كشف العتاد: {ex.Message}");
        }

        return specs;
    }

    private static void DetectRAM(HardwareSpecs specs)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory, FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                {
                    specs.TotalRAMBytes = Convert.ToInt64(obj["TotalPhysicalMemory"]) * 1024; // KB to bytes
                    specs.AvailableRAMBytes = Convert.ToInt64(obj["FreePhysicalMemory"]) * 1024;
                    break;
                }
            }
            catch
            {
                // Fallback using GC
                specs.TotalRAMBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                specs.AvailableRAMBytes = specs.TotalRAMBytes / 2; // تقدير متحفظ
            }
        }
    }

    private static void DetectCPU(HardwareSpecs specs)
    {
        specs.CPU_Cores = Environment.ProcessorCount;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    specs.CPU_Name = obj["Name"]?.ToString() ?? "Unknown CPU";
                    break;
                }
            }
            catch
            {
                specs.CPU_Name = RuntimeInformation.ProcessArchitecture.ToString();
            }
        }
    }

    private static void DetectGPU(HardwareSpecs specs)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController WHERE AdapterRAM > 0");
                foreach (var obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString() ?? "";
                    var vram = obj["AdapterRAM"] != null ? Convert.ToInt64(obj["AdapterRAM"]) : 0;

                    // تجاهل كروت العرض الأساسية (Microsoft Basic Display Adapter)
                    if (!name.Contains("Microsoft Basic", StringComparison.OrdinalIgnoreCase) &&
                        !name.Contains("Remote Desktop", StringComparison.OrdinalIgnoreCase) &&
                        vram > 64 * 1024 * 1024) // أكثر من 64 MB
                    {
                        specs.HasDedicatedGPU = true;
                        specs.GPUName = name;
                        specs.GPU_VRAM_Bytes = vram;
                        break;
                    }
                }
            }
            catch
            {
                // تجاهل أخطاء كشف GPU
            }
        }
    }

    private static void DetectDiskSpace(HardwareSpecs specs)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\");
            specs.FreeDiskSpaceBytes = drive.AvailableFreeSpace;
        }
        catch
        {
            specs.FreeDiskSpaceBytes = 10L * 1024 * 1024 * 1024; // 10 GB افتراضي
        }
    }
}