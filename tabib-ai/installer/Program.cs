using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace TabibAIInstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm());
    }
}

internal sealed class SetupForm : Form
{
    private readonly ProgressBar progress;
    private readonly Label statusLabel;
    private readonly Button installButton;
    private readonly Label modelInfoLabel;
    private readonly string installFolder;
    private bool installing;

    public SetupForm()
    {
        // UI initialization (Arabic RTL)
        Text = "تثبيت طبيب AI الذكي";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        ClientSize = new Size(700, 500);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        // Title
        var title = new Label
        {
            Text = "تثبيت طبيب AI الذكي على هذا الكمبيوتر",
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(24, 14, 24, 0),
            ForeColor = Color.FromArgb(27, 45, 56),
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(title);

        // Intro
        var intro = new Label
        {
            Text = "سيقوم المثبت بفحص مواصفات جهازك تلقائياً واختيار النموذج الطبي الأمثل لتشغيل طبيب AI دون الحاجة لإنترنت بعد التثبيت. لن يحتاج إلى Ollama أو تحميل إضافي.",
            Dock = DockStyle.Top,
            Height = 100,
            Padding = new Padding(25, 12, 25, 10),
            ForeColor = Color.FromArgb(75, 95, 103),
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(intro);
        Controls.SetChildIndex(intro, 0);

        // Model info label
        modelInfoLabel = new Label
        {
            Text = "🔍 جاري فحص مواصفات الجهاز...",
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(25, 8, 25, 8),
            ForeColor = Color.FromArgb(19, 124, 127),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(modelInfoLabel);
        Controls.SetChildIndex(modelInfoLabel, 0);

        // Progress bar
        progress = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 25,
            Style = ProgressBarStyle.Continuous
        };
        Controls.Add(progress);

        // Status label
        statusLabel = new Label
        {
            Text = "",
            Dock = DockStyle.Top,
            Height = 25,
            Padding = new Padding(25, 5, 25, 5),
            ForeColor = Color.FromArgb(27, 45, 56),
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(statusLabel);

        // Install button
        installButton = new Button
        {
            Text = "ابدأ التثبيت",
            Dock = DockStyle.Top,
            Height = 45,
            Padding = new Padding(20, 8, 20, 8),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(19, 124, 127),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        };
        installButton.Click += InstallButton_Click;
        Controls.Add(installButton);
        Controls.SetChildIndex(installButton, 0);

        // Install folder
        installFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TabibAI");
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        if (installing) return;
        installing = true;
        installButton.Enabled = false;

        try
        {
            // Step 1: Detect hardware
            ReportStatus("🔍 جاري فحص مواصفات الجهاز...");
            var specs = HardwareDetector.Detect();
            ReportStatus($"💾 المواصفات: {specs}");

            // Step 2: Select optimal model
            ReportStatus("🎯 جاري اختيار النموذج الأمثل...");
            var selection = ModelSelector.SelectOptimalModel(specs);
            ReportStatus(selection.Reason);
            modelInfoLabel.Text = $"🎯 النموذج الم選ت: {selection.SelectedModel.DisplayName}";

            // Step 3: Prepare install folder
            ReportStatus("📁 준비 폴더...");
            Directory.CreateDirectory(Path.Combine(installFolder, "Models"));

            // Step 4: Extract application
            ReportStatus("📦 جاري استخراج تطبيق طبيب AI...");
            var progressReporter = new Progress<ExtractProgress>(p =>
            {
                progress.Value = p.Percent;
                statusLabel.Text = p.Message;
            });
            var appPath = await ModelExtractor.ExtractAppAsync(installFolder, progressReporter);
            ReportStatus("✅ تم استخراج تطبيق طبيب AI");

            // Step 5: Extract selected model
            ReportStatus($"📦 جاري استخراج النموذج {selection.SelectedModel.DisplayName}...");
            var modelPath = await ModelExtractor.ExtractModelAsync(selection.SelectedModel, installFolder, progressReporter);
            ReportStatus($"✅ تم استخراج النموذج {selection.SelectedModel.DisplayName}");

            // Final
            ReportStatus("🎉 تم التثبيت بنجاح! يمكنك تشغيل طبيب AI من قائمة ابدأ أو سطح المكتب.");
            progress.Value = 100;
            installButton.Text = "إنهاء";
            installButton.Click -= InstallButton_Click;
            installButton.Click += (_, _) => Application.Exit();
        }
        catch (OperationCanceledException)
        {
            ReportStatus("❌ تم إلغاء التثبيت.");
        }
        catch (Exception ex)
        {
            ReportStatus($"❌ فشل التثبيت: {SafeError(ex.Message)}");
            installButton.Enabled = true;
            installing = false;
        }

    }
    private void ReportStatus(string message)
    {
        statusLabel.Text = message;
        // Optionally log to console
        Console.WriteLine(message);
    }

    private string SafeError(string value) => value.Length > 900 ? value[..900] + "…" : value.Replace("\r", " ").Replace("\n", " ");
}