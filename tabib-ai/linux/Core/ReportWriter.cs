using System.Diagnostics;
using System.Net;
using System.Text;

namespace TabibAI.Linux.Core;

/// <summary>
/// تصدير التقرير على لينكس: HTML جاهز للطباعة، وPDF عبر LibreOffice headless إن كان مثبتاً
/// (نقل لـ ReportExporter.cs في نسخة ويندوز الذي يستخدم Edge headless).
/// </summary>
public static class ReportWriter
{
    private const string Template = """
        <!doctype html>
        <html lang="ar" dir="rtl">
        <head>
        <meta charset="utf-8">
        <title>__TITLE__</title>
        <style>
            body { font-family: "Noto Naskh Arabic", "DejaVu Sans", Tahoma, Arial, sans-serif; color: #1b2d38; margin: 40px 48px; line-height: 1.75; }
            h1 { font-size: 21px; color: #137c7f; margin: 0 0 6px; }
            .sub { color: #687c85; font-size: 13px; margin-bottom: 22px; border-bottom: 1px solid #dee8e9; padding-bottom: 14px; }
            .card { border: 1px solid #dee8e9; border-radius: 10px; padding: 22px 24px; }
            pre { white-space: pre-wrap; word-break: break-word; font-family: inherit; margin: 0; font-size: 14px; }
            .footer { margin-top: 22px; font-size: 12px; color: #845d1f; background: #fff7e5; border: 1px solid #f0e0bd; padding: 12px 14px; border-radius: 8px; }
        </style>
        </head>
        <body>
        <h1>__TITLE__</h1>
        <div class="sub">__SUBTITLE__</div>
        <div class="card"><pre>__BODY__</pre></div>
        <div class="footer">__FOOTER__</div>
        </body>
        </html>
        """;

    public static string BuildHtml(string title, string subtitle, string body, string footer)
    {
        return Template
            .Replace("__TITLE__", WebUtility.HtmlEncode(title))
            .Replace("__SUBTITLE__", WebUtility.HtmlEncode(subtitle))
            .Replace("__BODY__", WebUtility.HtmlEncode(body))
            .Replace("__FOOTER__", WebUtility.HtmlEncode(footer));
    }

    /// <summary>يكتب التقرير النصي بترميز UTF-8 مع علامة ترتيب البايت.</summary>
    public static void WriteText(string path, string content) =>
        File.WriteAllText(path, content, new UTF8Encoding(true));

    public static void WriteHtml(string path, string html) =>
        File.WriteAllText(path, html, new UTF8Encoding(false));

    /// <summary>يحوّل HTML إلى PDF باستخدام LibreOffice headless. يعيد false عند عدم توفره.</summary>
    public static async Task<bool> TryHtmlToPdfAsync(string htmlPath, string pdfPath)
    {
        var soffice = new[] { "/usr/bin/soffice", "/usr/bin/libreoffice" }.FirstOrDefault(File.Exists);
        if (soffice is null) return false;
        var outDir = Path.GetDirectoryName(pdfPath);
        if (string.IsNullOrEmpty(outDir)) return false;
        try
        {
            if (File.Exists(pdfPath)) File.Delete(pdfPath);
            var info = new ProcessStartInfo(soffice)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            info.ArgumentList.Add("--headless");
            info.ArgumentList.Add("--norestore");
            info.ArgumentList.Add("--convert-to");
            info.ArgumentList.Add("pdf");
            info.ArgumentList.Add("--outdir");
            info.ArgumentList.Add(outDir);
            info.ArgumentList.Add(htmlPath);
            using var process = Process.Start(info);
            if (process is null) return false;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return false;
            }
            var produced = Path.Combine(outDir, Path.GetFileNameWithoutExtension(htmlPath) + ".pdf");
            if (produced != pdfPath && File.Exists(produced)) File.Move(produced, pdfPath, overwrite: true);
            return process.ExitCode == 0 && File.Exists(pdfPath) && new FileInfo(pdfPath).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>يفتح ملفاً أو رابطاً بالتطبيق الافتراضي على سطح المكتب.</summary>
    public static void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo("xdg-open", target) { UseShellExecute = false }); }
        catch { /* لا يوجد متصفح أو مدير ملفات */ }
    }
}
