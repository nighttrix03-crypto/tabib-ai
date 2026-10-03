using FellowOakDicom;

namespace TabibAI.Linux.Core;

/// <summary>
/// استيراد شريحة DICOM منفردة وتحويلها إلى PNG للفحص البصري.
/// الشريحة الواحدة ليست سلسلة CT/MRI كاملة، ولا تُستخدم لاستنتاجات سلسلة كاملة.
/// </summary>
public static class DicomImporter
{
    public static bool IsDicomPath(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".dcm", StringComparison.OrdinalIgnoreCase) || ext.Equals(".ima", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>يعيد PNG كـ bytes، أو يرمي استثناءً برسالة عربية واضحة.</summary>
    public static byte[] ToPng(string path)
    {
        var file = DicomFile.Open(path);
        var dataset = file.Dataset;

        int rows = dataset.GetSingleValueOrDefault(DicomTag.Rows, 0);
        int columns = dataset.GetSingleValueOrDefault(DicomTag.Columns, 0);
        int bits = dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, 16);
        int samples = dataset.GetSingleValueOrDefault(DicomTag.SamplesPerPixel, 1);
        string photometric = dataset.GetSingleValueOrDefault(DicomTag.PhotometricInterpretation, "MONOCHROME2");
        if (rows <= 0 || columns <= 0) throw new InvalidOperationException("ملف DICOM لا يحتوي أبعاد صورة صالحة.");

        var pixelItem = dataset.GetDicomItem<DicomItem>(DicomTag.PixelData);
        if (pixelItem is DicomFragmentSequence)
            throw new InvalidOperationException("هذه الشريحة مضغوطة (JPEG/JPEG2000) ولا يمكن عرضها هنا. حوّلها إلى PNG من برنامج الأشعة أولاً.");

        if (samples >= 3)
        {
            var rgb = dataset.GetValues<byte>(DicomTag.PixelData);
            int pixels = rows * columns * 3;
            if (rgb.Length < pixels) throw new InvalidOperationException("بيانات البكسل الملونة أقصر من المتوقع.");
            return Png.EncodeRgb8(columns, rows, Normalize(rgb[..pixels]));
        }

        var raw = bits <= 8 ? ToGray8(dataset.GetValues<byte>(DicomTag.PixelData))
                            : ToGray8(dataset.GetValues<ushort>(DicomTag.PixelData), dataset);
        if (raw.Length < rows * columns) throw new InvalidOperationException("بيانات البكسل أقصر من الأبعاد المعلنة.");
        if (photometric.Equals("MONOCHROME1", StringComparison.OrdinalIgnoreCase))
            for (int i = 0; i < rows * columns; i++) raw[i] = (byte)(255 - raw[i]);
        return Png.EncodeGray8(columns, rows, raw[..(rows * columns)]);
    }

    private static byte[] ToGray8(byte[] values)
    {
        var result = new byte[values.Length];
        Buffer.BlockCopy(values, 0, result, 0, values.Length);
        return result;
    }

    /// <summary>يطبّق نافذة العرض (WindowCenter/Width) أو المدى الفعلي عند غيابها.</summary>
    private static byte[] ToGray8(ushort[] values, DicomDataset dataset)
    {
        double slope = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
        double intercept = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);
        var stored = new double[values.Length];
        for (int i = 0; i < values.Length; i++) stored[i] = values[i] * slope + intercept;

        double center = dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, double.NaN);
        double width = dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, double.NaN);
        if (double.IsNaN(center) || double.IsNaN(width) || width <= 0)
        {
            var min = stored.Min();
            var max = stored.Max();
            if (max - min < 1e-6) max = min + 1;
            center = (max + min) / 2;
            width = max - min;
        }
        double low = center - width / 2;
        double high = center + width / 2;
        var result = new byte[values.Length];
        for (int i = 0; i < stored.Length; i++)
        {
            double scaled = (stored[i] - low) / (high - low) * 255.0;
            result[i] = (byte)Math.Clamp(scaled, 0, 255);
        }
        return result;
    }

    private static byte[] Normalize(byte[] values)
    {
        byte min = values.Min();
        byte max = values.Max();
        if (max == min) return values;
        var result = new byte[values.Length];
        for (int i = 0; i < values.Length; i++)
            result[i] = (byte)((values[i] - min) * 255 / (max - min));
        return result;
    }

    /// <summary>ينشئ ملف DICOM تجريبياً (اختبار ذاتي) — يُستخدم في --selftest فقط.</summary>
    public static string WriteSyntheticSlice(string path, int rows = 64, int columns = 64)
    {
        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
            { DicomTag.SOPInstanceUID, DicomUID.Generate() },
            { DicomTag.PatientName, "TEST^SELFTEST" },
            { DicomTag.Modality, "OT" },
            { DicomTag.Rows, (ushort)rows },
            { DicomTag.Columns, (ushort)columns },
            { DicomTag.BitsAllocated, (ushort)16 },
            { DicomTag.BitsStored, (ushort)16 },
            { DicomTag.HighBit, (ushort)15 },
            { DicomTag.PixelRepresentation, (ushort)0 },
            { DicomTag.SamplesPerPixel, (ushort)1 },
            { DicomTag.PhotometricInterpretation, "MONOCHROME2" },
            { DicomTag.WindowCenter, "1000" },
            { DicomTag.WindowWidth, "2000" }
        };
        var pixels = new ushort[rows * columns];
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                pixels[y * columns + x] = (ushort)(((x + y) % 2 == 0 ? 200 : 900) + x * 5);
        dataset.Add(new DicomOtherWord(DicomTag.PixelData, pixels));
        new DicomFile(dataset).Save(path);
        return path;
    }
}
