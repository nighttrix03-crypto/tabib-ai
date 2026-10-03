using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Desktop;

namespace TabibAI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private static readonly Color Ink = Color.FromArgb(27, 45, 56);
    private static readonly Color Muted = Color.FromArgb(104, 124, 132);
    private static readonly Color Canvas = Color.FromArgb(241, 246, 247);
    private static readonly Color Card = Color.White;
    private static readonly Color Teal = Color.FromArgb(19, 124, 127);
    private static readonly Color TealPale = Color.FromArgb(224, 243, 241);
    private static readonly Color Border = Color.FromArgb(222, 232, 233);
    private static readonly Color OrangePale = Color.FromArgb(255, 247, 229);
    private static readonly Color OrangeInk = Color.FromArgb(132, 93, 31);

    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(30) };
    private const string OllamaUrl = "http://localhost:11434";
    private const string MedicalModel = "medgemma1.5:latest";
    internal static readonly Dictionary<string, string> categoryLabels = new()
    {
        ["general"] = "مساعد طبي عام",
        ["radiology"] = "الأشعة والصور",
        ["labs"] = "التحاليل المخبرية",
        ["nutrition"] = "التغذية",
        ["medication"] = "معلومات الأدوية",
        ["emergency"] = "علامات الخطر والطوارئ",
        ["internal"] = "الباطنة والأمراض المزمنة",
        ["cardiology"] = "القلب والدورة الدموية",
        ["neurology"] = "الأعصاب والدماغ",
        ["pediatrics"] = "طب الأطفال",
        ["women"] = "صحة المرأة والحمل",
        ["dermatology"] = "الجلدية",
        ["mental"] = "الصحة النفسية",
        ["surgery"] = "الجراحة والعظام",
        ["ophthalmology"] = "طب العيون",
        ["ent"] = "الأنف والأذن والحنجرة",
        ["dental"] = "صحة الفم والأسنان",
        ["endocrine"] = "الغدد والسكري",
        ["gastro"] = "الجهاز الهضمي",
        ["renal"] = "الكلى والمسالك",
        ["rheumatology"] = "الروماتيزم والمفاصل",
        ["geriatrics"] = "طب كبار السن"
    };
    private readonly Dictionary<string, string> categoryDescriptions = new()
    {
        ["general"] = "نظّم المعلومات الطبية واستكشف الأسئلة التي تستحق مناقشتها مع المختص.",
        ["radiology"] = "ارفق صورة واضحة بصيغة PNG أو JPG أو WEBP لقراءة بصرية تمهيدية.",
        ["labs"] = "أضف نتيجة الفحص مع الوحدات والمدى المرجعي المطبوع في التقرير.",
        ["nutrition"] = "ناقش الاحتياجات والعادات الغذائية بصورة تثقيفية وآمنة.",
        ["medication"] = "اطلب شرحاً عاماً للمعلومات الدوائية والتداخلات المحتملة.",
        ["emergency"] = "راجع علامات الخطر؛ عند وجود حالة طارئة اتصل بالإسعاف المحلي فوراً.",
        ["internal"] = "ناقش الأعراض والأمراض المزمنة والمعلومات التي تفيد الطبيب الباطني.",
        ["cardiology"] = "ناقش أعراض القلب والقياسات والفحوصات مع التأكيد على علامات الطوارئ.",
        ["neurology"] = "ناقش الأعراض العصبية والفحوصات وما يستدعي تقييماً عاجلاً.",
        ["pediatrics"] = "معلومات تثقيفية للأطفال؛ اطلب العمر والوزن عند أهميتهما ولا تقترح جرعات.",
        ["women"] = "ناقش صحة المرأة والحمل مع مراعاة عمر الحمل وعلامات الخطر.",
        ["dermatology"] = "ناقش وصف الطفح أو الصورة الجلدية وحدود التقييم البصري.",
        ["mental"] = "ناقش الصحة النفسية بلغة داعمة؛ اسأل عن السلامة عند وجود خطر إيذاء النفس.",
        ["surgery"] = "ناقش الإصابات والأعراض العضلية الهيكلية والأسئلة قبل وبعد الجراحة.",
        ["gastro"] = "ناقش الأعراض الهضمية والكبدية ونقص الوزن غير المقصود مع إبراز علامات الخطر.",
        ["ent"] = "ناقش أعراض الأنف والأذن والحنجرة، ويمكن إرفاق صورة واضحة للحلق أو الأذن.",
        ["eye"] = "ناقش أعراض العين؛ الألم الشديد أو فقدان البصر المفاجئ يتطلب تقييماً عاجلاً.",
        ["ortho"] = "ناقش آلام المفاصل والظهر والعضلات والإصابات وما يستدعي تصويراً أو تقييماً طبياً.",
        ["urology"] = "ناقش أعراض المسالك البولية والكلى مع علامات العدوى الحادة أو احتباس البول.",
        ["dental"] = "ناقش مشكلات الأسنان والفم؛ توجّه لطبيب الأسنان عند الحاجة العملية.",
        ["endocrine"] = "ناقش أمراض الغدد والسكري والوزن استناداً إلى القيم المقدَّمة دون تعديل أي دواء.",
        ["allergy"] = "ناقش الحساسية والأعراض الموسمية؛ التحسس الشديد يستدعي الطوارئ فوراً.",
        ["elderly"] = "راعِ تعدد الأمراض والأدوية متعددة عند كبار السن وما يستدعي مراجعة الطبيب."
    };
    private readonly Dictionary<string, Button> categoryButtons = new();
    private readonly List<string> imagePaths = new();
    private readonly List<(string Role, string Content, List<string> Images)> chatHistory = new();
    private readonly TextBox followUpBox = new();
    private readonly Button askButton = new();
    private readonly Label connectionStatus = new();
    private readonly Label sectionTitle = new();
    private readonly Label sectionDescription = new();
    private readonly Label attachmentCount = new();
    private readonly ListBox attachmentList = new();
    private readonly TextBox ageBox = new();
    private readonly ComboBox sexBox = new();
    private readonly TextBox concernBox = new();
    private readonly TextBox historyBox = new();
    private readonly TextBox resultsBox = new();
    private readonly RichTextBox answerBox = new();
    private readonly Button analyzeButton = new();
    private readonly Button addImageButton = new();
    private readonly Label urgentBanner = new();
    private string category = "general";
    private bool modelReady;
    private bool busy;
    private CaseRecord? currentRecord;

    public MainForm()
    {
        Text = "Tabib AI | المساعد الطبي";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1080, 720);
        ClientSize = new Size(1440, 900);
        BackColor = Canvas;
        ForeColor = Ink;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        DoubleBuffered = true;
        new DicomSetupBuilder().RegisterServices(s => s.AddFellowOakDicom().AddImageManager<WinFormsImageManager>()).Build();

        BuildInterface();
        Shown += async (_, _) => await CheckModelAsync();
        FormClosing += (_, _) => http.Dispose();
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Canvas,
            Padding = new Padding(20, 16, 20, 16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildBody(), 0, 1);
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Canvas, Padding = new Padding(0, 0, 0, 12) };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(18, 10, 18, 10) };
        card.Paint += (_, e) => DrawBorder(card, e.Graphics);
        header.Controls.Add(card);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Card,
            Padding = new Padding(0, 5, 0, 0)
        };
        var icon = new Label
        {
            Text = "✚",
            AutoSize = false,
            Size = new Size(42, 42),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = TealPale,
            ForeColor = Teal,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold)
        };
        var titleStack = new Panel { Size = new Size(275, 46), BackColor = Card, Margin = new Padding(10, 0, 4, 0) };
        var title = new Label { Text = "طبيب AI", AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Ink, Location = new Point(0, 0) };
        var subtitle = new Label { Text = "مساعد صحي ذكي · جلسة محلية", AutoSize = true, Font = new Font("Segoe UI", 8.5F), ForeColor = Muted, Location = new Point(1, 27) };
        titleStack.Controls.Add(title);
        titleStack.Controls.Add(subtitle);
        right.Controls.Add(icon);
        right.Controls.Add(titleStack);
        card.Controls.Add(right);

        var left = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Card,
            Padding = new Padding(0, 7, 0, 0)
        };
        var settingsButton = MakeButton("إعداد النموذج المحلي", false, 170, 38);
        settingsButton.Click += async (_, _) => await OpenSetupAsync();
        left.Controls.Add(settingsButton);
        connectionStatus.AutoSize = false;
        connectionStatus.Size = new Size(250, 38);
        connectionStatus.TextAlign = ContentAlignment.MiddleCenter;
        connectionStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        connectionStatus.BackColor = Color.FromArgb(245, 248, 248);
        connectionStatus.ForeColor = Muted;
        connectionStatus.Margin = new Padding(0, 0, 9, 0);
        left.Controls.Add(connectionStatus);
        card.Controls.Add(left);

        return header;
    }

    private Control BuildBody()
    {
        var body = new Panel { Dock = DockStyle.Fill, BackColor = Canvas };
        var nav = BuildNavigation();
        nav.Dock = DockStyle.Right;
        nav.Width = 246;
        nav.Margin = new Padding(12, 0, 0, 0);
        var workspace = BuildWorkspace();
        workspace.Dock = DockStyle.Fill;
        body.Controls.Add(workspace);
        body.Controls.Add(nav);
        return body;
    }

    private Control BuildNavigation()
    {
        var nav = new Panel { BackColor = Card, Padding = new Padding(14), Margin = new Padding(12, 0, 0, 0) };
        nav.Paint += (_, e) => DrawBorder(nav, e.Graphics);
        var cap = new Label
        {
            Text = "مجالات المساعدة",
            Dock = DockStyle.Top,
            Height = 44,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        nav.Controls.Add(cap);

        var items = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 520,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Card
        };
        foreach (var entry in categoryLabels)
        {
            var button = new Button
            {
                Text = "   " + entry.Value,
                Tag = entry.Key,
                Width = 210,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                BackColor = Card,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 9.3F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 7),
                Padding = new Padding(8, 0, 8, 0)
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => SetCategory((string)button.Tag!);
            categoryButtons[entry.Key] = button;
            items.Controls.Add(button);
        }
        nav.Controls.Add(items);

        var note = new Label
        {
            Text = "تنبيه سلامة\n\nهذه الأداة للمعلومات والمناقشة فقط. لا تُصدر تشخيصاً نهائياً ولا تصف علاجاً.\n\nعند خطر مباشر أو تدهور سريع، اطلب خدمات الطوارئ المحلية.",
            Dock = DockStyle.Bottom,
            Height = 190,
            BackColor = OrangePale,
            ForeColor = OrangeInk,
            Padding = new Padding(12),
            TextAlign = ContentAlignment.TopRight,
            Font = new Font("Segoe UI", 9F),
            RightToLeft = RightToLeft.Yes
        };
        nav.Controls.Add(note);
        return nav;
    }

    private Control BuildWorkspace()
    {
        var workspace = new Panel { Dock = DockStyle.Fill, BackColor = Canvas, Padding = new Padding(12, 0, 0, 0) };
        var response = BuildResponsePanel();
        response.Dock = DockStyle.Right;
        response.Width = 460;
        var input = BuildInputPanel();
        input.Dock = DockStyle.Fill;
        workspace.Controls.Add(input);
        workspace.Controls.Add(response);
        return workspace;
    }

    private Control BuildInputPanel()
    {
        var shell = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(20, 18, 20, 16), Margin = new Padding(0, 0, 12, 0) };
        shell.Paint += (_, e) => DrawBorder(shell, e.Graphics);
        var heading = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Card };
        sectionTitle.Text = categoryLabels[category];
        sectionTitle.AutoSize = false;
        sectionTitle.Dock = DockStyle.Top;
        sectionTitle.Height = 33;
        sectionTitle.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        sectionTitle.ForeColor = Ink;
        sectionTitle.TextAlign = ContentAlignment.MiddleRight;
        sectionDescription.Text = categoryDescriptions[category];
        sectionDescription.Dock = DockStyle.Fill;
        sectionDescription.ForeColor = Muted;
        sectionDescription.Font = new Font("Segoe UI", 8.7F);
        sectionDescription.TextAlign = ContentAlignment.MiddleRight;
        heading.Controls.Add(sectionDescription);
        heading.Controls.Add(sectionTitle);
        shell.Controls.Add(heading);

        var actionBar = new Panel { Dock = DockStyle.Bottom, Height = 82, BackColor = Card, Padding = new Padding(0, 10, 0, 0) };
        var privacy = new Label
        {
            Text = "لا تكتب اسم المريض أو رقم هويته. تُحفظ الحالات محلياً في سجل على هذا الجهاز فقط، ويمكنك حذفها في أي وقت.",
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8F)
        };
        var actionButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Card
        };
        analyzeButton.Text = "حلّل الحالة";
        analyzeButton.Width = 145;
        analyzeButton.Height = 38;
        analyzeButton.BackColor = Teal;
        analyzeButton.ForeColor = Color.White;
        analyzeButton.FlatStyle = FlatStyle.Flat;
        analyzeButton.FlatAppearance.BorderSize = 0;
        analyzeButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        analyzeButton.Cursor = Cursors.Hand;
        analyzeButton.Click += async (_, _) => await AnalyzeAsync();
        var clear = MakeButton("حالة جديدة", false, 112, 38);
        clear.Click += (_, _) => ClearCase();
        actionButtons.Controls.Add(analyzeButton);
        actionButtons.Controls.Add(clear);
        actionBar.Controls.Add(actionButtons);
        actionBar.Controls.Add(privacy);
        shell.Controls.Add(actionBar);

        var scroll = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Card,
            Padding = new Padding(0, 8, 8, 8)
        };
        scroll.SizeChanged += (_, _) => ResizeInputRows(scroll);
        var basic = BuildBasicFields();
        scroll.Controls.Add(basic);
        scroll.Controls.Add(BuildField("سبب الاستشارة أو الأعراض", concernBox, 92, "ما الشكوى؟ متى بدأت؟ وما الذي تغيّر؟"));
        scroll.Controls.Add(BuildField("التاريخ والسياق الصحي", historyBox, 90, "العمر التقريبي، أمراض معروفة، أدوية أو حساسية ذات صلة، وعوامل سياقية."));
        scroll.Controls.Add(BuildField("الفحوصات والقياسات والملاحظات", resultsBox, 132, "الصق النتائج كما وردت، مع الوحدة والمدى المرجعي. أزل أي اسم أو رقم تعريفي."));
        scroll.Controls.Add(BuildAttachments());
        shell.Controls.Add(scroll);
        shell.Controls.SetChildIndex(scroll, 0);
        SetCategory(category);
        return shell;
    }

    private Control BuildBasicFields()
    {
        var row = new Panel { Height = 75, Width = 690, BackColor = Card, Margin = new Padding(0, 0, 0, 8) };
        var ageField = new Panel { Width = 150, Height = 70, Dock = DockStyle.Right, BackColor = Card };
        var ageLabel = new Label { Text = "العمر", Dock = DockStyle.Top, Height = 24, ForeColor = Muted, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 8.5F) };
        ageBox.BorderStyle = BorderStyle.FixedSingle;
        ageBox.Font = new Font("Segoe UI", 10F);
        ageBox.TextAlign = HorizontalAlignment.Right;
        ageBox.Dock = DockStyle.Bottom;
        ageBox.Height = 36;
        ageBox.MaxLength = 3;
        ageBox.PlaceholderText = "اختياري";
        ageField.Controls.Add(ageBox);
        ageField.Controls.Add(ageLabel);
        var sexField = new Panel { Width = 230, Height = 70, Dock = DockStyle.Right, BackColor = Card, Margin = new Padding(0, 0, 10, 0) };
        var sexLabel = new Label { Text = "الجنس البيولوجي (اختياري)", Dock = DockStyle.Top, Height = 24, ForeColor = Muted, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 8.5F) };
        sexBox.DropDownStyle = ComboBoxStyle.DropDownList;
        sexBox.Items.AddRange(new object[] { "غير محدد", "أنثى", "ذكر" });
        sexBox.SelectedIndex = 0;
        sexBox.Font = new Font("Segoe UI", 9F);
        sexBox.Dock = DockStyle.Bottom;
        sexBox.Height = 36;
        sexField.Controls.Add(sexBox);
        sexField.Controls.Add(sexLabel);
        row.Controls.Add(sexField);
        row.Controls.Add(ageField);
        var help = new Label
        {
            Text = "من دون اسم أو رقم ملف",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomRight,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8F),
            Padding = new Padding(0, 0, 7, 7)
        };
        row.Controls.Add(help);
        return row;
    }

    private Control BuildField(string labelText, TextBox box, int height, string placeholder)
    {
        var section = new Panel { Height = height + 36, Width = 690, BackColor = Card, Margin = new Padding(0, 0, 0, 8) };
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Ink,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        box.Multiline = true;
        box.AcceptsReturn = true;
        box.ScrollBars = ScrollBars.Vertical;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = Color.FromArgb(250, 252, 252);
        box.ForeColor = Ink;
        box.Font = new Font("Segoe UI", 9.5F);
        box.RightToLeft = RightToLeft.Yes;
        box.TextAlign = HorizontalAlignment.Right;
        box.Dock = DockStyle.Fill;
        box.PlaceholderText = placeholder;
        section.Controls.Add(box);
        section.Controls.Add(label);
        return section;
    }

    private Control BuildAttachments()
    {
        var section = new Panel { Height = 176, Width = 690, BackColor = Card, Margin = new Padding(0, 0, 0, 8) };
        var label = new Label
        {
            Text = "المرفقات",
            Dock = DockStyle.Top,
            Height = 27,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Ink,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 39,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Card
        };
        addImageButton.Text = "إرفاق صورة / DICOM";
        addImageButton.Size = new Size(156, 34);
        addImageButton.Click += (_, _) => AddImages();
        StyleButton(addImageButton, false);
        var addText = MakeButton("استيراد تقرير نصي", false, 150, 34);
        addText.Click += (_, _) => ImportTextReport();
        var remove = MakeButton("إزالة صورة", false, 108, 34);
        remove.Click += (_, _) => RemoveImage();
        bottom.Controls.Add(addImageButton);
        bottom.Controls.Add(addText);
        bottom.Controls.Add(remove);
        attachmentList.Dock = DockStyle.Fill;
        attachmentList.BorderStyle = BorderStyle.FixedSingle;
        attachmentList.BackColor = Color.FromArgb(250, 252, 252);
        attachmentList.ForeColor = Muted;
        attachmentList.Font = new Font("Segoe UI", 8.5F);
        attachmentList.HorizontalScrollbar = true;
        attachmentCount.Dock = DockStyle.Bottom;
        attachmentCount.Height = 21;
        attachmentCount.Text = "الصور لا تُرفع إلا عند ضغط «حلّل الحالة» وبعد موافقتك.";
        attachmentCount.TextAlign = ContentAlignment.MiddleRight;
        attachmentCount.ForeColor = Muted;
        attachmentCount.Font = new Font("Segoe UI", 7.7F);
        section.Controls.Add(attachmentList);
        section.Controls.Add(attachmentCount);
        section.Controls.Add(bottom);
        section.Controls.Add(label);
        return section;
    }

    private Control BuildResponsePanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(18, 16, 18, 16), Margin = new Padding(12, 0, 0, 0) };
        panel.Paint += (_, e) => DrawBorder(panel, e.Graphics);
        var heading = new Panel { Dock = DockStyle.Top, Height = 118, BackColor = Card };
        var title = new Label { Text = "نتيجة المراجعة", Dock = DockStyle.Top, Height = 31, ForeColor = Ink, Font = new Font("Segoe UI", 14F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
        var sub = new Label { Text = "تظهر هنا بعد إرسال الحالة بموافقتك", Dock = DockStyle.Top, Height = 28, ForeColor = Muted, Font = new Font("Segoe UI", 8.5F), TextAlign = ContentAlignment.MiddleRight };
        urgentBanner.Dock = DockStyle.Bottom;
        urgentBanner.Height = 52;
        urgentBanner.Visible = false;
        urgentBanner.BackColor = OrangePale;
        urgentBanner.ForeColor = OrangeInk;
        urgentBanner.Padding = new Padding(9, 5, 9, 5);
        urgentBanner.Font = new Font("Segoe UI", 8.3F, FontStyle.Bold);
        urgentBanner.TextAlign = ContentAlignment.MiddleRight;
        heading.Controls.Add(sub);
        heading.Controls.Add(title);
        heading.Controls.Add(urgentBanner);
        panel.Controls.Add(heading);

        var tools = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Card, Padding = new Padding(0, 5, 0, 0) };
        var copy = MakeButton("نسخ النتيجة", false, 120, 32);
        copy.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(answerBox.Text)) Clipboard.SetText(answerBox.Text);
        };
        var save = MakeButton("حفظ التقرير", false, 116, 32);
        save.Click += (_, _) => SaveReport();
        tools.Controls.Add(copy);
        tools.Controls.Add(save);
        panel.Controls.Add(tools);

        var chat = new Panel { Dock = DockStyle.Bottom, Height = 92, BackColor = Card, Padding = new Padding(0, 7, 0, 0) };
        askButton.Text = "اسأل";
        askButton.Size = new Size(82, 34);
        StyleButton(askButton, true);
        askButton.Click += async (_, _) => await AskFollowUpAsync();
        followUpBox.Multiline = true;
        followUpBox.AcceptsReturn = true;
        followUpBox.ScrollBars = ScrollBars.Vertical;
        followUpBox.RightToLeft = RightToLeft.Yes;
        followUpBox.TextAlign = HorizontalAlignment.Right;
        followUpBox.PlaceholderText = "اسأل متابعة تفصيلية عن الحالة…";
        followUpBox.Dock = DockStyle.Fill;
        followUpBox.Font = new Font("Segoe UI", 9F);
        var chatRow = new Panel { Dock = DockStyle.Fill, BackColor = Card };
        chatRow.Controls.Add(followUpBox);
        chatRow.Controls.Add(askButton);
        askButton.Dock = DockStyle.Left;
        chat.Controls.Add(chatRow);
        var chatCaption = new Label { Text = "محادثة متابعة (تبقى في الذاكرة إلى أن تبدأ حالة جديدة)", Dock = DockStyle.Top, Height = 23, TextAlign = ContentAlignment.MiddleRight, ForeColor = Muted, Font = new Font("Segoe UI", 8F) };
        chat.Controls.Add(chatCaption);
        panel.Controls.Add(chat);

        answerBox.Dock = DockStyle.Fill;
        answerBox.ReadOnly = true;
        answerBox.BorderStyle = BorderStyle.None;
        answerBox.BackColor = Card;
        answerBox.ForeColor = Ink;
        answerBox.Font = new Font("Segoe UI", 10F);
        answerBox.RightToLeft = RightToLeft.Yes;
        answerBox.DetectUrls = true;
        answerBox.Text = "أدخل تفاصيل الحالة، واختر مجال المساعدة، ثم اضغط «حلّل الحالة».\n\nتذكير: مخرجات النموذج قد تكون غير دقيقة. اعرضها على مختص صحي قبل اتخاذ قرار طبي.";
        panel.Controls.Add(answerBox);
        panel.Controls.SetChildIndex(answerBox, 0);
        return panel;
    }

    private void SetCategory(string newCategory)
    {
        category = newCategory;
        if (categoryLabels.TryGetValue(newCategory, out var label)) sectionTitle.Text = label;
        if (categoryDescriptions.TryGetValue(newCategory, out var description)) sectionDescription.Text = description;
        foreach (var entry in categoryButtons)
        {
            bool active = entry.Key == category;
            entry.Value.BackColor = active ? TealPale : Card;
            entry.Value.ForeColor = active ? Teal : Muted;
        }
    }

    private void ResizeInputRows(FlowLayoutPanel flow)
    {
        int width = Math.Max(410, flow.ClientSize.Width - 30);
        foreach (Control control in flow.Controls)
        {
            control.Width = width;
            if (control == attachmentList) continue;
        }
    }

    private async Task AnalyzeAsync()
    {
        if (busy) return;
        if (!modelReady) { await OpenSetupAsync(); if (!modelReady) return; }
        if (string.IsNullOrWhiteSpace(concernBox.Text) && string.IsNullOrWhiteSpace(historyBox.Text) && string.IsNullOrWhiteSpace(resultsBox.Text) && imagePaths.Count == 0)
        { MessageBox.Show(this, "أضف وصفاً أو نتيجة فحص أو صورة قبل التحليل.", "بيانات الحالة", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (!ValidateCase()) return;
        var urgentReason = DetectUrgentWarning();
        SetUrgentBanner(urgentReason);
        if (!string.IsNullOrEmpty(urgentReason) && MessageBox.Show(this,
            "قد تتضمن المعلومات علامة خطر: " + urgentReason + "\n\nإذا كانت الأعراض تحدث الآن أو تتدهور، اتصل بخدمات الطوارئ المحلية أو اذهب إلى أقرب قسم طوارئ. لا تؤخر المساعدة بانتظار تحليل التطبيق.\n\nهل تريد متابعة التحليل التثقيفي المحلي بعد طلب المساعدة عند الحاجة؟",
            "تنبيه عاجل", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        if (MessageBox.Show(this, "ستُعالج الحالة والصور محلياً على هذا الكمبيوتر. لا تُرسل إلى خدمة سحابية من هذا التطبيق. تجنّب المعلومات التعريفية. هل تريد المتابعة؟", "موافقة التحليل المحلي", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        busy = true; analyzeButton.Enabled = false; askButton.Enabled = false; analyzeButton.Text = "جارٍ التحليل…"; answerBox.Text = "يجري التحليل محلياً…";
        try
        {
            var prompt = ComposeCasePrompt();
            var images = await GetImageDataAsync();
            chatHistory.Clear(); chatHistory.Add(("user", prompt, images));
            var messages = new object[] { new { role = "system", content = SystemPrompt() }, new { role = "user", content = prompt, images } };
            var body = new { model = MedicalModel, messages, stream = false, keep_alive = "10m", options = new { temperature = 0.15, num_ctx = 8192 } };
            using var response = await http.PostAsync(OllamaUrl + "/api/chat", new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {SafeError(raw)}");
            using var json = JsonDocument.Parse(raw);
            var answer = json.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "لم يصل نص في الرد.";
            chatHistory.Add(("assistant", answer, new List<string>())); answerBox.Text = answer; answerBox.SelectionStart = 0; answerBox.ScrollToCaret();
        }
        catch (TaskCanceledException) { answerBox.Text = "انتهت مهلة التحليل. النموذج المحلي قد يحتاج وقتاً على الأجهزة الأبطأ؛ جرّب صورة واحدة أو وصفاً أقصر."; }
        catch (Exception ex) { answerBox.Text = "تعذّر التحليل المحلي. افتح إعداد النموذج وتحقق من Ollama وMedGemma.\n\n" + SafeError(ex.Message); }
        finally { busy = false; analyzeButton.Enabled = true; askButton.Enabled = true; analyzeButton.Text = "حلّل الحالة"; }
    }

    private async Task<List<string>> GetImageDataAsync()
    {
        var result = new List<string>();
        foreach (var path in imagePaths.Take(4))
        {
            var ext = Path.GetExtension(path);
            if (ext.Equals(".dcm", StringComparison.OrdinalIgnoreCase) || ext.Equals(".ima", StringComparison.OrdinalIgnoreCase))
            {
                var file = await DicomFile.OpenAsync(path);
                using var bitmap = new DicomImage(file.Dataset).RenderImage().As<Bitmap>();
                using var memory = new MemoryStream(); bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                result.Add(Convert.ToBase64String(memory.ToArray()));
            }
            else result.Add(Convert.ToBase64String(await File.ReadAllBytesAsync(path)));
        }
        return result;
    }

    private async Task AskFollowUpAsync()
    {
        if (busy || string.IsNullOrWhiteSpace(followUpBox.Text)) return;
        if (!modelReady) { await OpenSetupAsync(); if (!modelReady) return; }
        if (chatHistory.Count == 0) { MessageBox.Show(this, "حلّل الحالة أولاً لبدء محادثة متابعة.", "المحادثة", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        var question = followUpBox.Text.Trim(); followUpBox.Clear(); busy = true; askButton.Enabled = false; analyzeButton.Enabled = false;
        try
        {
            chatHistory.Add(("user", question, new List<string>()));
            var messages = new List<object> { new { role = "system", content = SystemPrompt() } };
            foreach (var turn in chatHistory.TakeLast(12)) messages.Add(turn.Images.Count > 0 ? new { role = turn.Role, content = turn.Content, images = turn.Images } : new { role = turn.Role, content = turn.Content });
            var body = new { model = MedicalModel, messages, stream = false, keep_alive = "10m", options = new { temperature = 0.15, num_ctx = 8192 } };
            using var response = await http.PostAsync(OllamaUrl + "/api/chat", new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            var raw = await response.Content.ReadAsStringAsync(); if (!response.IsSuccessStatusCode) throw new HttpRequestException(SafeError(raw));
            using var json = JsonDocument.Parse(raw); var answer = json.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "لم يصل رد.";
            chatHistory.Add(("assistant", answer, new List<string>())); answerBox.AppendText($"\n\n— سؤالك —\n{question}\n\n— متابعة MedGemma —\n{answer}"); answerBox.SelectionStart = answerBox.TextLength; answerBox.ScrollToCaret();
        }
        catch (Exception ex) { answerBox.AppendText("\n\nتعذّرت المتابعة: " + SafeError(ex.Message)); }
        finally { busy = false; askButton.Enabled = true; analyzeButton.Enabled = true; }
    }

    private string ComposeCasePrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"مجال المساعدة: {categoryLabels[category]}.");
        if (!string.IsNullOrWhiteSpace(ageBox.Text)) sb.AppendLine($"العمر: {ageBox.Text.Trim()}.");
        if (sexBox.SelectedIndex > 0) sb.AppendLine($"الجنس البيولوجي: {sexBox.SelectedItem}.");
        AppendSection(sb, "سبب الاستشارة والأعراض", LimitText(concernBox.Text, 5000));
        AppendSection(sb, "التاريخ والسياق الصحي", LimitText(historyBox.Text, 5000));
        AppendSection(sb, "الفحوصات والقياسات والملاحظات", LimitText(resultsBox.Text, 6000));
        if (imagePaths.Count > 0) sb.AppendLine($"يوجد {Math.Min(imagePaths.Count, 4)} صورة/شريحة مرفقة للفحص البصري. إن كانت ملفات DICOM فهي شرائح منفردة وليست سلسلة تصوير كاملة.");
        sb.AppendLine();
        sb.AppendLine("أجب باللغة العربية الواضحة. اذكر ما تدعمه المعلومات وما لا يمكن استنتاجه منها. لا تملأ الفراغات بافتراضات.");
        return sb.ToString();
    }

    private string SystemPrompt()
    {
        string domainInstructions = category switch
        {
            "radiology" => "إذا وُجدت صور، ابدأ بوصف مرئي موجز ومحايد لما يظهر، وافصل الوصف عن أي تفسير محتمل. اذكر جودة الصورة وحدودها. لا تدّعِ تشخيصاً أو استبعاد مرض، ووضّح أن القراءة النهائية لاختصاصي الأشعة. إذا لم تكن الصورة فعلاً فحصاً طبياً أو كانت غير واضحة، صرّح بذلك. إذا كانت صورة DICOM مستوردة، تذكر أنك ترى شريحة واحدة فقط ولا تفسر سلسلة CT/MRI كاملة من شريحة منفردة.",
            "labs" => "في التحاليل، استخدم الوحدات والمدى المرجعي المقدمين فقط. لا تفترض مجالاً مرجعياً عند غيابه. ميّز القيم الظاهرة عن تفسيرها المحتمل، واطلب المعلومات الناقصة عند الحاجة.",
            "nutrition" => "قدّم معلومات تغذوية عامة فقط. لا تصف حمية علاجية فردية ولا مكملات أو جرعات. انتبه للحمل والأطفال والأمراض المزمنة واضطرابات الأكل، ووجّه لمختص تغذية/طبيب عند وجودها.",
            "medication" => "قدّم معلومات عامة عن الاستخدامات والتحذيرات والتداخلات المحتملة إذا أمكن. لا تقترح بدء دواء أو إيقافه أو تغيير الجرعة. اطلب الرجوع إلى الطبيب أو الصيدلي، ولا تتعامل مع القائمة كبديل مراجعة دوائية.",
            "emergency" => "ابدأ بتحديد ما إذا كانت الأعراض المذكورة قد تمثل خطراً يتطلب الاتصال الفوري بخدمات الطوارئ المحلية. لا تطمئن المستخدم إذا وجدت مؤشرات إنذار.",
            "pediatrics" => "راعِ اختلاف الأطفال عن البالغين، واطلب العمر والوزن عند الحاجة. لا تعط جرعات أو تطمئن بشأن طفل مع أعراض خطيرة.",
            "women" => "اسأل عن احتمال الحمل أو عمر الحمل عند صلته بالسؤال، واذكر علامات الخطر المتعلقة بالحمل عند انطباقها.",
            "mental" => "استخدم لغة داعمة وغير حكمية. إذا ذكر المستخدم نية أو خطة لإيذاء نفسه أو غيره، شجعه فوراً على الاتصال بالطوارئ المحلية وإخبار شخص موثوق والبقاء معه.",
            _ => "قدّم ملخصاً تعليمياً منظماً للمعلومات الصحية، واذكر احتمالات عامة لا تشخيصاً، والأسئلة المفيدة لطرحها على المختص."
        };
        return "أنت مساعد معلومات صحية تثقيفي باللغة العربية، لا طبيب ولا نظام تشخيص معتمد. "
            + "اتبع مبدأ السلامة: عند علامة خطر محتملة، ابدأ بتوجيه واضح لطلب المساعدة العاجلة ولا تؤخره بالأسئلة. "
            + "لا تقدّم تشخيصاً نهائياً، ولا خطة علاج، ولا وصفة أو جرعات دوائية. لا تقل إن الحالة سليمة أو خطيرة على نحو جازم من بيانات ناقصة. "
            + "لا تختلق نتائج أو مراجع أو نسب دقة. إذا تعذّر الاستنتاج فقل ذلك صراحة. حافظ على السرية ولا تطلب اسم المريض أو رقم هويته. "
            + "الإسعافات الأولية غير الدوائية مسموحة ومطلوبة عند الطوارئ ولا تعدّ وصفة علاجية. "
            + "عند وجود خطر فوري أو عند بُعد المسافة عن المستشفى أو صعوبة الوصول إليه: لا تكتفِ بجملة «راجع الطبيب» أو «استشر مختصاً» وحدها. "
            + "قدّم أولاً الإسعافات الأولية الآمنة خطوة بخطوة (ما يمكن فعله الآن وما يجب تجنّبه)، ثم وجّه المستخدم للاتصال بالإسعاف "
            + "ورقم الطوارئ المحلي في بلده (مثال: 1122 في الجزائر، 15 في المغرب، 997 في السعودية، 123 في مصر، 998 في الإمارات) "
            + "إلى أقرب مركز صحي، مع شرح ما يجب فعله أثناء الانتظار أو النقل. وضّح أن ذلك إسعاف أولي لا يغني عن تقييم مختص. "
            + "لا تبدأ الرد بالنصيحة العامة عند وجود إجراء عاجل آمن يمكن تنفيذه فوراً. "
            + domainInstructions + "\n\n"
            + "نسّق الرد بعناوين قصيرة: ١) ملخص المعلومات، ٢) ما يمكن ملاحظته أو فهمه، ٣) احتمالات عامة مع مستوى يقين منخفض أو متوسط فقط وسبب عدم اليقين، ٤) معلومات ناقصة وأسئلة للمختص، ٥) متى يلزم طلب مساعدة عاجلة إن كان السياق يدعم ذلك. "
            + "لا تستخدم لغة مخيفة أو واثقة أكثر مما تسمح به البيانات."
            + (imagePaths.Count > 0 ? " الصور المرفقة تُرسل للتحليل البصري فقط، وليست بديلاً عن تقرير طبيب الأشعة." : "");
    }

    private bool ValidateCase()
    {
        if (string.IsNullOrWhiteSpace(ageBox.Text)) return true;
        if (!int.TryParse(ageBox.Text.Trim(), out var age) || age is < 0 or > 120)
        {
            MessageBox.Show(this, "اكتب العمر كرقم بين 0 و120، أو اتركه فارغاً.", "العمر", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        return true;
    }

    /// <summary>تطبيع عربي: يزيل الهمزات والتشكيل لتقارب الصيغ (ألم/الم، راس/رأس) مطابقاً للنسخة اللينكسية.</summary>
    private static string NormalizeArabic(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.ToLowerInvariant())
        {
            switch (ch)
            {
                case 'أ': case 'إ': case 'آ': case 'ٱ': builder.Append('ا'); break;
                case 'ء': case 'ـ': case '\u064B': case '\u064C': case '\u064D':
                case '\u064E': case '\u064F': case '\u0650': case '\u0651': case '\u0652': break;
                case 'ؤ': builder.Append('و'); break;
                case 'ئ': builder.Append('ي'); break;
                case 'ى': builder.Append('ي'); break;
                default: builder.Append(ch); break;
            }
        }
        return Regex.Replace(builder.ToString(), "\\s+", " ");
    }

    private string DetectUrgentWarning()
    {
        var raw = $"{concernBox.Text} {historyBox.Text} {resultsBox.Text}".ToLowerInvariant();
        var text = NormalizeArabic(raw);
        var redFlags = new (string[] Terms, string Reason)[]
        {
            (new[] { "ألم صدر", "الم في الصدر", "الم صدر", "ألم في الصدر", "ألم بالصدر", "الم بالصدر", "ضغط الصدر", "ثقل الصدر", "chest pain" }, "ألم أو ضغط في الصدر"),
            (new[] { "ضيق تنفس", "صعوبة التنفس", "لا أستطيع التنفس", "shortness of breath" }, "صعوبة في التنفس"),
            (new[] { "إغماء", "اغماء", "فاقد الوعي", "فقدان الوعي", "unconscious" }, "فقدان الوعي أو إغماء"),
            (new[] { "شلل", "ضعف مفاجئ", "تدلي الوجه", "تلعثم", "سكتة" }, "أعراض عصبية مفاجئة"),
            (new[] { "انتحار", "أقتل نفسي", "اقتل نفسي", "إيذاء نفسي", "suicide" }, "خطر إيذاء النفس"),
            (new[] { "نزيف شديد", "ينزف بشدة", "قيء دم", "براز أسود", "نزيف حاد", "نزيف قوي", "نزيف غزير" }, "نزيف مهم محتمل"),
            (new[] { "تشنج", "اختلاج", "seizure" }, "تشنج أو اختلاج"),
            (new[] { "حساسية شديدة", "تورم اللسان", "تورم الحلق", "anaphylaxis" }, "حساسية شديدة محتملة")
        };
        foreach (var flag in redFlags)
        {
            if (flag.Terms.Any(term => raw.Contains(term) || text.Contains(NormalizeArabic(term))))
                return flag.Reason;
        }
        // اقتران كلمات الألم مع مواضع خطيرة (يمسك صيغا مختلفة مثل «واجع بطني»).
        var coOccurrenceFlags = new (string[] PainWords, string[] Regions, string Reason)[]
        {
            (new[] { "الم", "وجع", "واجع", "ضغط", "ثقل", "حرقة" },
             new[] { "صدر" }, "ألم أو ضغط في الصدر"),
            (new[] { "الم", "وجع", "واجع", "تصلب" },
             new[] { "بطن" }, "ألم بطني شديد محتمل"),
            (new[] { "الم", "وجع", "واجع", "اسوأ", "انفجار" },
             new[] { "راس", "رقبة", "عقب" }, "الم راسي مفاجئ او شديد محتمل")
        };
        foreach (var flag in coOccurrenceFlags)
        {
            if (flag.PainWords.Any(word => text.Contains(NormalizeArabic(word)))
                && flag.Regions.Any(region => text.Contains(NormalizeArabic(region))))
                return flag.Reason;
        }
        return string.Empty;
    }

    private void SetUrgentBanner(string reason)
    {
        urgentBanner.Visible = !string.IsNullOrWhiteSpace(reason);
        urgentBanner.Text = urgentBanner.Visible
            ? $"تنبيه: قد تشير المعلومات إلى {reason}. لا تنتظر التحليل إذا كانت الحالة تحدث الآن أو تتدهور؛ اطلب خدمات الطوارئ المحلية."
            : string.Empty;
    }

    private static string LimitText(string value, int limit) => value.Trim().Length <= limit ? value.Trim() : value.Trim()[..limit] + "\n[تم اختصار النص الطويل]";

    private static void AppendSection(StringBuilder sb, string title, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.AppendLine($"{title}:");
            sb.AppendLine(value.Trim());
        }
    }

    private void AddImages()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "اختيار صور طبية",
            Multiselect = true,
            Filter = "صور وملفات DICOM|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.dcm;*.ima|صور|*.png;*.jpg;*.jpeg;*.webp;*.gif|DICOM|*.dcm;*.ima|كل الملفات|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var path in dialog.FileNames)
        {
            if (imagePaths.Contains(path, StringComparer.OrdinalIgnoreCase)) continue;
            if (imagePaths.Count >= 4)
            {
                MessageBox.Show(this, "حد هذه الجلسة أربع صور أو شرائح. اختر أهم الصور، ولا تعتبر ذلك بديلاً عن مراجعة سلسلة الأشعة كاملة.", "حد المرفقات", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
            }
            var info = new FileInfo(path);
            bool dicom = path.EndsWith(".dcm", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".ima", StringComparison.OrdinalIgnoreCase);
            if (info.Length > 12 * 1024 * 1024 && !dicom)
            {
                MessageBox.Show(this, $"الصورة أكبر من 12 ميغابايت:\n{Path.GetFileName(path)}", "حجم الصورة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                continue;
            }
            imagePaths.Add(path);
        }
        RefreshAttachments();
    }

    private void RemoveImage()
    {
        if (attachmentList.SelectedIndex < 0 || attachmentList.SelectedIndex >= imagePaths.Count) return;
        imagePaths.RemoveAt(attachmentList.SelectedIndex);
        RefreshAttachments();
    }

    private void RefreshAttachments()
    {
        attachmentList.Items.Clear();
        foreach (var path in imagePaths) attachmentList.Items.Add((path.EndsWith(".dcm", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".ima", StringComparison.OrdinalIgnoreCase) ? "DICOM · " : "صورة · ") + Path.GetFileName(path));
        attachmentCount.Text = imagePaths.Count == 0
            ? "الصيغ: PNG · JPG · WEBP · DICOM (.dcm/.ima). حد أقصى 4 صور للتحليل"
            : $"{imagePaths.Count} صورة — لن تُرسل إلا عند ضغط «حلّل الحالة» والموافقة.";
    }

    private void ImportTextReport()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "استيراد تقرير نصي",
            Multiselect = false,
            Filter = "تقارير نصية|*.txt;*.csv;*.md|ملفات نصية|*.txt;*.csv|كل الملفات|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            string text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            if (resultsBox.TextLength > 0) resultsBox.AppendText(Environment.NewLine + Environment.NewLine);
            resultsBox.AppendText($"— {Path.GetFileName(dialog.FileName)} —{Environment.NewLine}{text}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "تعذّر قراءة الملف النصي. " + SafeError(ex.Message), "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearCase()
    {
        if (string.IsNullOrWhiteSpace(concernBox.Text) && string.IsNullOrWhiteSpace(historyBox.Text)
            && string.IsNullOrWhiteSpace(resultsBox.Text) && imagePaths.Count == 0) return;
        if (MessageBox.Show(this, "سيتم مسح تفاصيل الحالة والنتيجة الحالية من النافذة. متابعة؟", "حالة جديدة", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        ageBox.Clear();
        sexBox.SelectedIndex = 0;
        concernBox.Clear();
        historyBox.Clear();
        resultsBox.Clear();
        imagePaths.Clear();
        chatHistory.Clear();
        RefreshAttachments();
        answerBox.Text = "أدخل تفاصيل الحالة، واختر مجال المساعدة، ثم اضغط «حلّل الحالة».\n\nتذكير: مخرجات النموذج قد تكون غير دقيقة. اعرضها على مختص صحي قبل اتخاذ قرار طبي.";
    }

    private void SaveReport()
    {
        if (string.IsNullOrWhiteSpace(answerBox.Text)) return;
        using var dialog = new SaveFileDialog
        {
            Title = "حفظ نتيجة المراجعة",
            FileName = $"TabibAI-{DateTime.Now:yyyy-MM-dd-HHmm}.txt",
            Filter = "تقرير نصي|*.txt",
            AddExtension = true,
            DefaultExt = "txt"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(dialog.FileName, answerBox.Text, new UTF8Encoding(true));
        MessageBox.Show(this, "تم حفظ التقرير في المسار الذي اخترته.", "حفظ التقرير", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task CheckModelAsync()
    {
        try
        {
            using var response = await http.GetAsync(OllamaUrl + "/api/tags");
            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                modelReady = doc.RootElement.GetProperty("models").EnumerateArray().Any(m => m.GetProperty("name").GetString()?.StartsWith("medgemma1.5", StringComparison.OrdinalIgnoreCase) == true);
            }
        }
        catch { modelReady = false; }
        connectionStatus.Text = modelReady ? "● MedGemma جاهز محلياً" : "○ إعداد النموذج المحلي مطلوب";
        connectionStatus.ForeColor = modelReady ? Teal : Muted;
    }

    private async Task OpenSetupAsync()
    {
        using var dialog = new LocalSetupForm(modelReady);
        if (dialog.ShowDialog(this) == DialogResult.Cancel) return;
        if (dialog.Action == "install")
        {
            Process.Start(new ProcessStartInfo("https://ollama.com/download/windows") { UseShellExecute = true });
            MessageBox.Show(this, "ثبّت Ollama ثم أعد فتح التطبيق واضغط «تنزيل MedGemma». يتطلب التنزيل الأول اتصالاً بالإنترنت ومساحة تقارب 3.3 GB.", "تثبيت محلي", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (dialog.Action == "pull") await PullModelAsync();
    }

    private async Task PullModelAsync()
    {
        try
        {
            connectionStatus.Text = "⏳ جارٍ تنزيل MedGemma…";
            var body = new { name = MedicalModel, stream = true };
            using var request = new HttpRequestMessage(HttpMethod.Post, OllamaUrl + "/api/pull")
            { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) throw new HttpRequestException(await response.Content.ReadAsStringAsync());
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            while (await reader.ReadLineAsync() is { } line)
            {
                try
                {
                    using var evt = JsonDocument.Parse(line);
                    var root = evt.RootElement;
                    string state = root.TryGetProperty("status", out var status) ? status.GetString() ?? "جارٍ التنزيل" : "جارٍ التنزيل";
                    if (root.TryGetProperty("total", out var total) && total.TryGetInt64(out long all) && all > 0
                        && root.TryGetProperty("completed", out var completed) && completed.TryGetInt64(out long done))
                        connectionStatus.Text = $"⏳ {state} · {Math.Clamp(done * 100 / all, 0, 100)}%";
                    else connectionStatus.Text = "⏳ " + state;
                }
                catch (JsonException) { }
            }
            await CheckModelAsync();
            MessageBox.Show(this, modelReady ? "تم تنزيل MedGemma. أصبح التحليل يعمل محلياً." : "لم يظهر النموذج ضمن النماذج المثبتة. تحقق من Ollama.", "إعداد النموذج", MessageBoxButtons.OK, modelReady ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex) { await CheckModelAsync(); MessageBox.Show(this, "تعذّر تنزيل النموذج. تأكد من تثبيت Ollama وتشغيله واتصال الإنترنت.\n\n" + SafeError(ex.Message), "Ollama", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static string SafeError(string value)
    {
        if (value.Length > 420) value = value[..420] + "…";
        return value.Replace("\r", " ").Replace("\n", " ");
    }

    private static void DrawBorder(Control control, Graphics graphics)
    {
        using var pen = new Pen(Border);
        graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
    }

    private static Button MakeButton(string text, bool primary, int width, int height)
    {
        var button = new Button { Text = text, Size = new Size(width, height), Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8.7F, FontStyle.Bold), RightToLeft = RightToLeft.Yes };
        StyleButton(button, primary);
        return button;
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Border;
        button.BackColor = primary ? Teal : Color.White;
        button.ForeColor = primary ? Color.White : Ink;
        button.Font = new Font("Segoe UI", 8.7F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }
}

internal sealed class LocalSetupForm : Form
{
    public string Action { get; private set; } = "";
    private readonly CheckBox terms = new();
    private readonly Label status = new();
    public LocalSetupForm(bool ready)
    {
        Text = "إعداد MedGemma المحلي"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        ClientSize = new Size(620, 355); BackColor = Color.White; Font = new Font("Segoe UI", 9F);
        var title = new Label { Text = "الذكاء الطبي المحلي", Dock = DockStyle.Top, Height = 48, Padding = new Padding(20, 10, 20, 0), ForeColor = Color.FromArgb(27,45,56), Font = new Font("Segoe UI", 15F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
        Controls.Add(title);
        var text = new Label { Text = "يعتمد التطبيق على MedGemma 1.5 4B عبر Ollama. يعمل الاستدلال على هذا الجهاز دون مفتاح API؛ يلزم الإنترنت لتنزيل النموذج أول مرة (نحو 3.3 GB). نتائج النموذج ليست تشخيصاً سريرياً معتمداً، وقد يخطئ، كما أن قدرته على الصور الطبية لا تعادل اختصاصي الأشعة. لا تستخدمه لاتخاذ قرار علاجي.", Dock = DockStyle.Top, Height = 103, Padding = new Padding(22,10,22,8), ForeColor = Color.FromArgb(104,124,132), TextAlign = ContentAlignment.MiddleRight };
        Controls.Add(text); Controls.SetChildIndex(text,0);
        var link = new LinkLabel { Text = "اقرأ شروط HAI-DEF وMedGemma من Google قبل التنزيل أو الاستخدام", Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(22,0,22,0) };
        link.LinkClicked += (_,_) => Process.Start(new ProcessStartInfo("https://developers.google.com/health-ai-developer-foundations/terms") { UseShellExecute = true });
        Controls.Add(link); Controls.SetChildIndex(link,0);
        terms.Text = "قرأت الشروط وأوافق على استخدامها؛ أفهم أن النموذج غير معتمد للتشخيص السريري."; terms.Dock = DockStyle.Top; terms.Height = 36; terms.Padding = new Padding(20,0,20,0); terms.TextAlign = ContentAlignment.MiddleRight; terms.RightToLeft = RightToLeft.Yes;
        Controls.Add(terms); Controls.SetChildIndex(terms,0);
        status.Text = ready ? "الحالة: النموذج مثبت وجاهز." : "الحالة: يتطلب تثبيت Ollama ثم تنزيل النموذج."; status.Dock = DockStyle.Top; status.Height = 30; status.TextAlign = ContentAlignment.MiddleRight; status.Padding = new Padding(22,0,22,0);
        Controls.Add(status); Controls.SetChildIndex(status,0);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(16,8,16,0), BackColor = Color.FromArgb(246,249,249) };
        var install = new Button { Text = "تنزيل Ollama", Width = 120, Height = 34 };
        install.Click += (_,_) => { Action = "install"; DialogResult = DialogResult.OK; Close(); };
        var pull = new Button { Text = "تنزيل MedGemma", Width = 145, Height = 34, Enabled = !ready };
        pull.Click += (_,_) => { if (!terms.Checked) { MessageBox.Show(this,"اقرأ شروط النموذج وأكد الاطلاع عليها أولاً.","الشروط",MessageBoxButtons.OK,MessageBoxIcon.Warning); return; } Action = "pull"; DialogResult = DialogResult.OK; Close(); };
        var close = new Button { Text = "إغلاق", Width = 90, Height = 34, DialogResult = DialogResult.Cancel };
        footer.Controls.Add(install); footer.Controls.Add(pull); footer.Controls.Add(close); Controls.Add(footer); AcceptButton = pull; CancelButton = close;
    }
}
