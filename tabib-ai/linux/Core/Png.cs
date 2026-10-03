using System.IO.Compression;

namespace TabibAI.Linux.Core;

/// <summary>
/// مُرمّز PNG مختصر بلا اعتماديات خارجية (توقيع + IHDR + IDAT + IEND).
/// يُستخدم لتحويل شرائح DICOM إلى صور يمكن للنموذج قراءتها بصرياً.
/// </summary>
public static class Png
{
    public static byte[] EncodeGray8(int width, int height, byte[] pixels) =>
        Encode(width, height, colorType: 0, pixels, 1);

    public static byte[] EncodeRgb8(int width, int height, byte[] rgb) =>
        Encode(width, height, colorType: 2, rgb, 3);

    private static byte[] Encode(int width, int height, byte colorType, byte[] pixels, int channels)
    {
        if (width <= 0 || height <= 0) throw new ArgumentException("أبعاد الصورة غير صالحة.");
        if (pixels.Length < width * height * channels) throw new ArgumentException("بيانات الصورة أقصر من الأبعاد المعلنة.");

        int rawStride = width * channels + 1;
        var raw = new byte[rawStride * height];
        for (int y = 0; y < height; y++)
        {
            raw[y * rawStride] = 0; // filter type: None
            Buffer.BlockCopy(pixels, y * width * channels, raw, y * rawStride + 1, width * channels);
        }

        using var compressed = new MemoryStream();
        compressed.WriteByte(0x78);      // zlib: CMF
        compressed.WriteByte(0x01);      // zlib: FLG (no dictionary, fastest)
        using (var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(raw, 0, raw.Length);
        var adler = Adler32(raw);
        compressed.WriteByte((byte)(adler >> 24));
        compressed.WriteByte((byte)(adler >> 16));
        compressed.WriteByte((byte)(adler >> 8));
        compressed.WriteByte((byte)adler);

        using var png = new MemoryStream();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var ihdr = new byte[13];
        WriteBigEndian(ihdr, 0, width);
        WriteBigEndian(ihdr, 4, height);
        ihdr[8] = 8;          // bit depth
        ihdr[9] = colorType;  // 0 = gray, 2 = RGB
        ihdr[10] = 0;         // compression
        ihdr[11] = 0;         // filter
        ihdr[12] = 0;         // interlace
        WriteChunk(png, "IHDR", ihdr);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        WriteBigEndian(length, 0, data.Length);
        stream.Write(length);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        uint crc = Crc32(typeBytes);
        crc = Crc32(data, crc);
        var crcBytes = new byte[4];
        WriteBigEndian(crcBytes, 0, (int)crc);
        stream.Write(crcBytes);
    }

    private static void WriteBigEndian(byte[] target, int offset, int value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    private static uint Adler32(byte[] data)
    {
        const uint mod = 65521;
        uint a = 1, b = 0;
        foreach (var value in data)
        {
            a = (a + value) % mod;
            b = (b + a) % mod;
        }
        return (b << 16) | a;
    }

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }

    private static uint Crc32(byte[] data, uint seed = 0xFFFFFFFFu)
    {
        uint c = seed;
        foreach (var value in data)
            c = CrcTable[(c ^ value) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }
}
