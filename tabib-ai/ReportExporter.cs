using System.Diagnostics;
using System.Net;
using System.Text;

namespace TabibAI;

/// <summary>تصدير التقرير: HTML مهيّأ للطباعة، وPDF عبر Microsoft Edge headless (موجود في كل ويندوز).</summary>
internal static class ReportExporter
{
    private const string Template = """
        <!doctype html>
        <html lang="ar" dir="rtl">
        <head>
        <meta charset="utf-8">
        <title>__TITLE__</title>
        <style>
            body { font-family: "Segoe UI", Tahoma, Arial, sans-serif; color: #1b2d38; margin: 40px 48px; line-height: 1.75; }
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

    /// <summary>يولّد PDF من صفحة HTML عبر msedge --headless --print-to-pdf. يعيد false عند الفشل.</summary>
    public static async Task<bool> TryPrintToPdfAsync(string htmlPath, string pdfPath)
    {
        var edge = FindEdge();
        if (edge is null) return false;
        try
        {
            if (File.Exists(pdfPath)) File.Delete(pdfPath);
            var info = new ProcessStartInfo(edge)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            info.ArgumentList.Add("--headless");
            info.ArgumentList.Add("--disable-gpu");
            info.ArgumentList.Add("--no-first-run");
            info.ArgumentList.Add("--no-pdf-header-footer");
            info.ArgumentList.Add("--virtual-time-budget=10000");
            info.ArgumentList.Add("--print-to-pdf=" + pdfPath);
            info.ArgumentList.Add(new Uri(htmlPath).AbsoluteUri);
            using var process = Process.Start(info);
            if (process is null) return false;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return false;
            }
            return process.ExitCode == 0 && File.Exists(pdfPath) && new FileInfo(pdfPath).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindEdge()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "Microsoft-Edge", "msedge.exe")
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
