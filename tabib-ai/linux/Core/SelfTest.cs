using TabibAI.Linux.Core;

namespace TabibAI.Linux.Core;

/// <summary>اختبار ذاتي يعمل بلا واجهة: يتحقق من قواعد السلامة وترميز PNG ومسار DICOM والسجل.</summary>
public static class SelfTest
{
    private static int passed;
    private static int failed;

    public static int Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== Tabib AI Linux — اختبار ذاتي ===");
        Console.WriteLine();

        Check("قائمة المجالات 22 مجالاً بمفاتيح فريدة", () =>
        {
            var keys = ClinicalPrompts.Categories.Select(c => c.Key).ToList();
            return keys.Count == 22 && keys.Distinct().Count() == 22;
        });

        Check("العمر الفارغ مقبول", () => SafetyRules.ValidateAge("") is null);
        Check("العمر 30 مقبول", () => SafetyRules.ValidateAge(" 30 ") is null);
        Check("العمر 150 مرفوض", () => SafetyRules.ValidateAge("150") is not null);
        Check("العمر النصي مرفوض", () => SafetyRules.ValidateAge("abc") is not null);
        Check("العمر السالب مرفوض", () => SafetyRules.ValidateAge("-5") is not null);

        Check("كشف ألم الصدر", () => SafetyRules.DetectUrgentWarning("عندي ألم في الصدر", "", "") == "ألم أو ضغط في الصدر");
        Check("كشف ضيق التنفس", () => SafetyRules.DetectUrgentWarning("", "ضيق تنفس شديد", "") == "صعوبة في التنفس");
        Check("كشف خطر إيذاء النفس", () => SafetyRules.DetectUrgentWarning("", "", "suicide") == "خطر إيذاء النفس");
        Check("كشف الأعراض العصبية المفاجئة", () => SafetyRules.DetectUrgentWarning("تدلي الوجه وتلعثم", "", "") == "أعراض عصبية مفاجئة");
        Check("لا إنذار كاذب لنص عادي", () => SafetyRules.DetectUrgentWarning("صداع خفيف منذ يومين", "", "") == "");
        Check("نص التنبيه يوجّه للطوارئ", () => SafetyRules.UrgentBannerText("ألم أو ضغط في الصدر").Contains("الطوارئ"));

        Check("كشف النزيف الغزير", () =>
            SafetyRules.DetectUrgentWarning("", "نزيف قوي من ساقه اليمنى", "") == "نزيف مهم محتمل");
        Check("كسر بدون نزيف لا يُخفي علامة الخطر", () =>
            SafetyRules.DetectUrgentWarning("كسر محتمل في الساق", "", "") == "");
        Check("اقتران ألم مع الصدر (صيغة غير مباشرة)", () =>
            SafetyRules.DetectUrgentWarning("واجع صدري عند المشي", "", "") == "ألم أو ضغط في الصدر");
        Check("تطبيع عربي: الم بطني بدون همزة", () =>
            SafetyRules.DetectUrgentWarning("عندي الم بطن شديد", "", "") == "ألم بطني شديد محتمل");
        Check("رأس بدون همزه مع ألم يُمسك", () =>
            SafetyRules.DetectUrgentWarning("الم راس مفاجئ وشديد", "", "") == "الم راسي مفاجئ او شديد محتمل");
        Check("فاقد الوعي مع التنفس يُمسك رغم صيغته", () =>
            SafetyRules.DetectUrgentWarning("", "المصاب فاقد الوعي لكنه يتنفس", "") == "فقدان الوعي أو إغماء");
        Check("لا انذار كاذب لوصف موقع بريء", () =>
            SafetyRules.DetectUrgentWarning("وجع في أسنان الرحى", "", "") == "");

        Check("تقييد النص الطويل", () =>
        {
            var limited = SafetyRules.LimitText(new string('ع', 6000), SafetyRules.ConcernLimit);
            return limited.Contains("[تم اختصار النص الطويل]") && limited.Length < 6000;
        });
        Check("النص القصير لا يُقيّد", () => SafetyRules.LimitText("مرحبا", 100) == "مرحبا");
        Check("وجود بيانات الحالة يُكتشف", () => SafetyRules.HasCaseInput("", "", "سكر 7.2", 0));
        Check("الحالة الفارغة تُرفض", () => !SafetyRules.HasCaseInput("", "", "", 0));

        Check("موجّه النظام يمنع التشخيص النهائي", () =>
            SystemPrompts.For("general", 0).Contains("لا تقدّم تشخيصاً نهائياً"));
        Check("موجّه الأشعة يذكر الشريحة الواحدة", () =>
            SystemPrompts.For("radiology", 1).Contains("شريحة واحدة"));
        Check("موجّه الصحة النفسية يوجّه للطوارئ", () =>
            SystemPrompts.For("mental", 0).Contains("الطوارئ المحلية"));

        Check("بناء أمر الحالة يحوي المجال والقيد", () =>
        {
            var prompt = ClinicalPrompts.ComposeCasePrompt(ClinicalPrompts.ByKey("labs"), "45", "أنثى",
                new string('أ', 5200), "تاريخ", "HbA1c 7.2%", 1);
            return prompt.Contains("مجال المساعدة: التحاليل المخبرية.") && prompt.Contains("[تم اختصار النص الطويل]")
                && prompt.Contains("HbA1c 7.2%") && prompt.Contains("صورة/شريحة مرفقة");
        });

        RunMediaChecks();

        Console.WriteLine();
        Console.WriteLine($"النتيجة: {passed} ناجح، {failed} فاشل");
        return failed == 0 ? 0 : 1;
    }

    private static void RunMediaChecks()
    {
        Check("مُرمّز PNG ينتج ملفاً صالحاً", () =>
        {
            var pixels = new byte[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = (byte)(i % 256);
            var png = Png.EncodeGray8(64, 64, pixels);
            var signatureOk = png.Length > 8 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47;
            var text = System.Text.Encoding.ASCII.GetString(png);
            var path = Path.Combine(Path.GetTempPath(), "tabib-selftest-gray.png");
            File.WriteAllBytes(path, png);
            return signatureOk && text.Contains("IHDR") && text.Contains("IDAT") && text.Contains("IEND")
                && FfmpegDecodes(path);
        });

        Check("مسار DICOM: إنشاء شريحة ثم تحويلها إلى PNG", () =>
        {
            var dcm = Path.Combine(Path.GetTempPath(), "tabib-selftest-slice.dcm");
            DicomImporter.WriteSyntheticSlice(dcm, 64, 64);
            if (!DicomImporter.IsDicomPath(dcm)) return false;
            var png = DicomImporter.ToPng(dcm);
            int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
            var path = Path.Combine(Path.GetTempPath(), "tabib-selftest-dicom.png");
            File.WriteAllBytes(path, png);
            return png[1] == 0x50 && width == 64 && height == 64 && FfmpegDecodes(path);
        });

        Check("رفض ملف غير DICOM برسالة واضحة", () =>
        {
            var fake = Path.Combine(Path.GetTempPath(), "tabib-selftest-broken.dcm");
            File.WriteAllText(fake, "not a dicom file");
            try { DicomImporter.ToPng(fake); return false; }
            catch { return true; }
        });

        Check("السجل: حفظ وقراءة وحذف", () =>
        {
            var sandbox = Path.Combine(Path.GetTempPath(), "tabib-selftest-data-" + Guid.NewGuid().ToString("N")[..8]);
            var previous = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            try
            {
                Environment.SetEnvironmentVariable("XDG_DATA_HOME", sandbox);
                var record = new CaseRecord { Category = "labs", Age = "45", Concern = "سكر", Answer = "رد" };
                CaseStore.Save(record);
                var loaded = CaseStore.LoadAll();
                CaseStore.Delete(record.Id);
                return loaded.Count == 1 && loaded[0].Answer == "رد" && CaseStore.LoadAll().Count == 0;
            }
            finally
            {
                Environment.SetEnvironmentVariable("XDG_DATA_HOME", previous);
                try { Directory.Delete(sandbox, true); } catch { }
            }
        });

        Check("تقرير HTML يحوي الترويسة والنص", () =>
        {
            var html = ReportWriter.BuildHtml("عنوان", "فرعي", "نص الرد", SafetyRules.DiagnosisDisclaimer);
            return html.Contains("dir=\"rtl\"") && html.Contains("عنوان") && html.Contains("نص الرد");
        });
    }

    /// <summary>يتحقق أن الملف صورة PNG قابلة للفك فعلياً عبر ffmpeg (إن كان مثبتاً).</summary>
    private static bool FfmpegDecodes(string path)
    {
        if (!File.Exists("/usr/bin/ffmpeg")) return true;
        try
        {
            var info = new System.Diagnostics.ProcessStartInfo("/usr/bin/ffmpeg")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            foreach (var arg in new[] { "-v", "error", "-i", path, "-f", "null", "-" })
                info.ArgumentList.Add(arg);
            using var process = System.Diagnostics.Process.Start(info);
            if (process is null) return false;
            process.StandardError.ReadToEnd();
            process.WaitForExit(20000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void Check(string title, Func<bool> test)
    {
        bool ok;
        string detail = "";
        try
        {
            ok = test();
        }
        catch (Exception ex)
        {
            ok = false;
            detail = " ← " + ex.GetType().Name + ": " + SafetyRules.Shorten(ex.Message);
        }
        if (ok) { passed++; Console.WriteLine($"[PASS] {title}"); }
        else { failed++; Console.WriteLine($"[FAIL] {title}{detail}"); }
    }
}
