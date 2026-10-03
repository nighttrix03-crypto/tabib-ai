using System.Text;
using System.Text.Json;

namespace TabibAI.Linux.Core;

/// <summary>حالة محفوظة محلياً على هذا الجهاز فقط (نقل لـ HistoryStore.cs في نسخة ويندوز).</summary>
public sealed class CaseRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public string Category { get; set; } = "general";
    public string Age { get; set; } = "";
    public string Sex { get; set; } = "";
    public string Concern { get; set; } = "";
    public string History { get; set; } = "";
    public string Results { get; set; } = "";
    public string Answer { get; set; } = "";
    public List<string> ImagePaths { get; set; } = new();
    public List<CaseTurn> Turns { get; set; } = new();
}

public sealed class CaseTurn
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}

/// <summary>ملف JSON واحد لكل حالة داخل مجلد بيانات المستخدم. لا يغادر الجهاز.</summary>
public static class CaseStore
{
    public static string Root
    {
        get
        {
            var baseDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrWhiteSpace(baseDir))
                baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            return Path.Combine(baseDir, "TabibAI", "history");
        }
    }

    public static List<CaseRecord> LoadAll()
    {
        var records = new List<CaseRecord>();
        if (!Directory.Exists(Root)) return records;
        foreach (var file in Directory.EnumerateFiles(Root, "*.json"))
        {
            try
            {
                var record = JsonSerializer.Deserialize<CaseRecord>(File.ReadAllText(file));
                if (record is not null && !string.IsNullOrWhiteSpace(record.Id)) records.Add(record);
            }
            catch { /* ملف تالف يُتخطى دون تعطيل السجل */ }
        }
        return records.OrderByDescending(r => r.SavedAt).ToList();
    }

    public static void Save(CaseRecord record)
    {
        Directory.CreateDirectory(Root);
        var path = Path.Combine(Root, record.Id + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
    }

    public static void Delete(string id)
    {
        try { File.Delete(Path.Combine(Root, id + ".json")); }
        catch { /* الملف قد يكون محذوفاً أصلاً */ }
    }

    public static void Clear()
    {
        foreach (var record in LoadAll()) Delete(record.Id);
    }
}
