using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Case record model.
/// </summary>
public record CaseRecord(
    Guid Id,
    string Category,
    string Age,
    string Sex,
    string Concern,
    string Answer,
    DateTime Timestamp,
    List<string> Images
);

/// <summary>
/// Interface for case storage.
/// </summary>
public interface ICaseStore
{
    Task SaveAsync(CaseRecord record);
    Task<List<CaseRecord>> LoadAllAsync();
    Task DeleteAsync(Guid id);
    Task<CaseRecord?> GetAsync(Guid id);
}

/// <summary>
/// Interface for safety rules.
/// </summary>
public interface ISafetyRules
{
    string? ValidateAge(string age);
    string DetectUrgentWarning(string concern, string symptoms, string history);
    string UrgentBannerText(string reason);
    string LimitText(string text, int maxLength);
    bool HasCaseInput(string age, string sex, string concern, int imageCount);
    string DiagnosisDisclaimer { get; }
    string EmergencyMessage { get; }
    int ConcernLimit { get; }
    string DefaultAnswer { get; }
    string Shorten(string text);
}

/// <summary>
/// Interface for report writing.
/// </summary>
public interface IReportWriter
{
    string BuildHtml(string title, string subtitle, string content, string disclaimer);
    string BuildMarkdown(string title, string subtitle, string content, string disclaimer);
}

/// <summary>
/// Interface for DICOM import.
/// </summary>
public interface IDicomImporter
{
    byte[] ToPng(string dicomPath);
    bool IsDicomPath(string path);
    Task<byte[]> ToPngAsync(string dicomPath, CancellationToken ct = default);
}