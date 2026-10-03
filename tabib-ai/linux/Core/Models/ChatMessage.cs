using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TabibAI.Linux.Core.Models;

/// <summary>
/// Chat message types for icons and styling
/// </summary>
public enum MessageRole
{
    User,
    Assistant,
    System,
    Error
}

/// <summary>
/// Chat modes for different conversation contexts
/// </summary>
public enum ChatMode
{
    General,
    Medical,
    Code,
    Translate
}

/// <summary>
/// Text direction for RTL/LTR support
/// </summary>
public enum TextDirection
{
    Auto,
    LTR,
    RTL
}

/// <summary>
/// Represents a single chat message with rich metadata
/// </summary>
public class ChatMessage : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MessageRole Role { get; set; } = MessageRole.User;

    private string _content = "";
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// مساعد لروابط الواجهة (XAML): true = رسالة مستخدم، false = رسالة المساعد.
    /// </summary>
    [JsonIgnore]
    public bool IsUser
    {
        get => Role == MessageRole.User;
        set => Role = value ? MessageRole.User : MessageRole.Assistant;
    }

    // Streaming support
    private bool _isStreaming;
    public bool IsStreaming
    {
        get => _isStreaming;
        set
        {
            if (_isStreaming != value)
            {
                _isStreaming = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsComplete { get; set; } = true;
    
    // RTL/LTR support
    public TextDirection Direction { get; set; } = TextDirection.Auto;
    public string? Language { get; set; }
    
    // Chat mode context
    public ChatMode Mode { get; set; } = ChatMode.General;
    
    // Metadata
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Gets the icon name for the message role
    /// </summary>
    [JsonIgnore]
    public string IconName => Role switch
    {
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        MessageRole.System => "system",
        MessageRole.Error => "error",
        _ => "unknown"
    };
    
    /// <summary>
    /// Determines if the message content is RTL based on language or content analysis
    /// </summary>
    public bool IsRTL
    {
        get
        {
            if (Direction == TextDirection.RTL) return true;
            if (Direction == TextDirection.LTR) return false;
            
            // Auto-detect based on language
            if (!string.IsNullOrEmpty(Language))
            {
                var rtlLanguages = new[] { "ar", "he", "fa", "ur", "ps", "sd" };
                return rtlLanguages.Any(l => Language.StartsWith(l, StringComparison.OrdinalIgnoreCase));
            }
            
            // Auto-detect based on content (first strong RTL character)
            return ContainsRtlCharacters(Content);
        }
    }
    
    private static bool ContainsRtlCharacters(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        foreach (var c in text)
        {
            // Check for strong RTL characters (Arabic, Hebrew, etc.)
            if (c >= 0x0590 && c <= 0x08FF) return true; // Hebrew, Arabic, Syriac, etc.
            if (c >= 0xFB1D && c <= 0xFDFF) return true; // Arabic Presentation Forms
            if (c >= 0xFE70 && c <= 0xFEFF) return true; // Arabic Presentation Forms-B
            // LTR strong characters (Latin, etc.) - if we hit these first, it's LTR
            if (c >= 0x0041 && c <= 0x007A) return false; // Basic Latin
            if (c >= 0x00C0 && c <= 0x024F) return false; // Latin Extended
        }
        return false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Represents a saved conversation session
/// </summary>
public class ConversationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public ChatMode Mode { get; set; } = ChatMode.General;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<ChatMessage> Messages { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
    
    /// <summary>
    /// Generates a title from the first user message
    /// </summary>
    public void GenerateTitle()
    {
        var firstUserMsg = Messages.FirstOrDefault(m => m.Role == MessageRole.User);
        if (firstUserMsg != null)
        {
            Title = firstUserMsg.Content.Length > 50 
                ? firstUserMsg.Content[..50] + "..." 
                : firstUserMsg.Content;
        }
        else
        {
            Title = $"Conversation {CreatedAt:yyyy-MM-dd HH:mm}";
        }
    }
}

public class MedicalCase
{
    public Guid Id { get; set; }
    public string ChiefComplaint { get; set; } = "";
    public List<DifferentialDiagnosis> DifferentialDiagnosis { get; set; } = new();
    public List<string> Plan { get; set; } = new();
    public string Referral { get; set; } = "";
    public List<string> EmergencyFlags { get; set; } = new();
    public List<string> FollowUpQuestions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
}

public class DifferentialDiagnosis
{
    public string Condition { get; set; } = "";
    public int Probability { get; set; }
    public string Reasoning { get; set; } = "";
    public string ICD10Code { get; set; } = "";
}

public class MedicalResponse
{
    public List<DifferentialDiagnosis> Differential { get; set; } = new();
    public List<string> Plan { get; set; } = new();
    public string Referral { get; set; } = "";
    public List<string> EmergencyFlags { get; set; } = new();
    public List<string> FollowUpQuestions { get; set; } = new();
}
