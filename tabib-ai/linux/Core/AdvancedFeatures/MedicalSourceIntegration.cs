using System;
using System.Collections.Generic;
using System.Linq;

namespace TabibAI.Linux.Core.AdvancedFeatures;

/// <summary>
/// دمج مصادر المعلومات الطبية الموثوقة والمواد السريرية للتطبيق Tabib AI.
/// يوفر الوصول إلى أحدث الأدلة، والمبادئ التوجيهية، والموارد السريرية.
/// </summary>
public sealed class MedicalSourceIntegration
{
    private readonly List<IMedicalSource> _sources;
    private readonly IMedicalContentValidator _validator;

    public MedicalSourceIntegration()
    {
        _sources = new List<IMedicalSource>
        {
            new UpToDateSource(),
            new CochraneDatabase(),
            new PubMedRepository(),
            new GuidelinesAmerica(),
            new EuropeanMedicalSocieties(),
            new NationalGuidelinesInstitute(),
            new WHOGuidelines(),
            new AcademicMedicalJournals()
        };
        
        _validator = new MedicalContentValidator();
    }

    /// <summary>
    /// البحث عن المصادر الطبية ذات الصلة بحالة المريض.
    /// </summary>
    public List<MedicalSourceResult> SearchRelevantSources(PatientData patient)
    {
        var allResults = new List<MedicalSourceResult>();
        
        foreach (var source in _sources)
        {
            try
           :
                var result = source.Search(patient);
                if (result.ConfidenceScore >= 0.7m && _validator.IsValid(result))
                {
                    allResults.Add(result);
                }
            }
            catch (Exception)
            {
                // تجاهل المصادر التي تفشل في الاستجابة
                continue;
            }
        }
        
        // ترتيب حسب الموثوقية والتأثير
        return allResults
            .OrderByDescending(r => r.ConfidenceScore)
            .ThenBy(r => r.Source.Rank)
            .Take(20)
            .ToList();
    }

    /// <summary>
    /// الحصول على مصدر طبي مخصص لحالة معينة.
    /// </summary>
    public IMedicalSource GetSourceForSpecialty(MedicalSpecialty specialty)
    {
        return _sources.FirstOrDefault(s => s.Specialties.Contains(specialty)) 
               ?? _sources.First();
    }

    /// <summary>
    /// التحقق من جودة محتوى مصدر طبي.
    /// </summary>
    public bool ValidateSourceQuality(IMedicalSource source)
    {
        return _validator.IsHighQuality(source);
    }

    /// <summary>
    /// الحصول على قائمة بجميع المصادر الطبية المدمجة.
    /// </summary>
    public IReadOnlyList<IMedicalSource> GetIntegratedSources()
    {
        return _sources.AsReadOnly();
    }
}

/// <summary>
/// واجهة لأنظمة المصادر الطبية.
/// </summary>
public interface IMedicalSource
{
    /// <summary>
    /// اسم المصدر (مثل: UpToDate، Cochrane، إلخ).
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// التخصصات الطبية التي يغطيها المصدر.
    /// </summary>
    List<MedicalSpecialty> Specialties { get; }
    
    /// <summary>
    /// ترتيب المصدر (1=الأعلى، 10=الأدنى).
    /// </summary>    int Rank { get; }
    
    /// <summary>
    /// البحث عن محتوى طبي لمريض معين.
    /// </summary>
    MedicalSourceResult Search(PatientData patient);
    
    /// <summary>
    /// التحقق من أحدث المحتوى.
    /// </summary>
    bool UpdateContent();
}

/// <summary>
/// نتيجة البحث من مصدر طبي.
/// </summary>
public sealed class MedicalSourceResult
{
    /// <summary>
    /// عنوان المحتوى.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// ملخص المحتوى.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// مستوى الثقة في المحتوى.
    /// </summary>
    public decimal ConfidenceScore { get; set; }
    
    /// <summary>
    /// المصدر الذي قدم هذا المحتوى.
    /// </summary>
    public IMedicalSource Source { get; set; } = null!;
    
    /// <summary>
    /// تاريخ نشر المحتوى.
    /// </summary>
    public DateTime PublicationDate { get; set; }
    
    /// <summary>
    /// الرابط للوصول إلى المحتوى الكامل.
    /// </summary>
    public string ContentUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// الأنواع الفرعية من المحتوى.
    /// </summary>
    public List<ContentType> ContentTypes { get; set; } = new();
    
    /// <summary>
    /// المفاتيح الرئيسية للاستخدام السريري.
    /// </summary>
    public List<string> KeyPoints { get; set; } = new();
    
    /// <summary>
    /// القيود أو التحذيرات الخاصة بالمحتوى.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// ما إذا كان المحتوى محدثًا حاليًا.
    /// </summary>
    public bool IsCurrent { get; set; }
    
    /// <summary>
    /// تأثير المحتوى السريري (منخفض، متوسط، عالٍ).
    /// </summary>
    public ClinicalImpactLevel ImpactLevel { get; set; }
}

/// <summary>
/// أنواع المحتوى الطبي المختلفة.
/// </summary>
public enum ContentType
{
    ClinicalGuideline,
    SystematicReview,
    RandomizedControlledTrial,
    CaseReport,
    MetaAnalysis,
    ConferenceAbstract,
    MedicalNews,
    DrugInformation
}

/// <summary>
/// التخصصات الطبية المدعومة.
/// </summary>
public enum MedicalSpecialty
{
    InternalMedicine,
    Cardiology,
    Neurology,
    Pediatrics,
    Oncology,
    Surgery,
    Psychiatry,
    Orthopedics,
    ObstetricsGynecology,
    EmergencyMedicine,
    FamilyMedicine,
    Dermatology,
    Ophthalmology,
    Otolaryngology,
    Urology
}

/// <summary>
/// مستوى التأثير السريري للمحتوى الطبي.
/// </summary>
public enum ClinicalImpactLevel
{
    Low,      // معلومات أساسية، توصيات غير قوية
    Moderate, // أدلة متوسطة الجودة، توصيات محدودة
    High,     // أدلة عالية الجودة، توصيات قوية
    Critical  // أدلة عالية الجودة، توصيات تغيير الممارسة
}

/// <summary>
/// مدقق جودة المحتوى الطبي.
/// </summary>
public sealed class MedicalContentValidator
{
    private readonly HashSet<string> _trustedSourceDomains = new()
    {
        "uptodate.com",
        "cochrane.org",
        "pubmed.ncbi.nlm.nih.gov",
        "aafp.org",
        "nejm.org",
        "science.org",
        "bmj.com",
        "nature.com医学",
        "sciencemag.org"
    };

    /// <summary>
    /// التحقق من صحة محتوى مصدر طبي.
    /// </summary>
    public bool IsValid(MedicalSourceResult result)
    {
        if (result == null || string.IsStr(result.Title) || result.ConfidenceScore < 0.3m)
        {
            return false;
        }
        
        // التحقق من النطاق الزمني (لا يقبل المحتوى القديم جدًا)
        var ageInDays = (DateTime.Now - result.PublicationDate).TotalDays;
        if (ageInDays > 365) // أقدم من سنة واحدة
        {
            return false; // إلا إذا كان دليلًا رئيسيًا
        }
        
        // التحقق من جود المصدر
        if (!_trustedSourceDomains.Contains(GetDomain(result.ContentUrl)))
        {
            return false; // مصادر غير معروفة
        }
        
        return true;
    }

    /// <summary>
    /// التحقق مما إذا كان المصدر عالي الجودة.
    /// </summary>
    public bool IsHighQuality(IMedicalSource source)
    {
        return source.Rank <= 3; // أعلى المصادر فقط
    }

    private string GetDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host.ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// مصدر UpToDate الطبي.
/// </summary>
public sealed class UpToDateSource : IMedicalSource
{
    public string Name => "UpToDate";
    public List<MedicalSpecialty> Specialties => Enum.GetValues<MedicalSpecialty>().ToList();
    public int Rank => 1;
    
    public MedicalSourceResult Search(PatientData patient)
    {
        // محاكاة البحث في UpToDate
        return new MedicalSourceResult
        {
            Title = $"UpToDate: {patient.Diagnosis} - تقييم وتوصيات العلاج",
            Summary = $"تقييم شامل لـ {patient.Age} {GetPatientDescription(patient)} مع توصيات العلاج المحدثة القائمة على الأدلة.",
            ConfidenceScore = 0.95m,
            Source = this,
            PublicationDate = DateTime.Now.AddDays(-7),
            ContentUrl = "https://www.uptodate.com", // سيكون نموذجًا فعليًا
            ContentTypes = new List<ContentType> { ContentType.ClinicalGuideline, ContentType.SystematicReview },
            KeyPoints = new List<string>
            {
                "النهج التشخيصي التفريقي",
                "اعتبارات مختبرية موصى بها",
                "خيارات العلاج القائمة على الأدلة",
                "اعتبارات المتابعة والمراقبة"
            },
            Warnings = new List<string>
            {
                "يجب أن تكون المعلومات الطبية المؤهلة للحصول على ترخيص مشمولة",
                "استشر Mundy متخصصًا صحيًا مرخصًا قبل اتخاذ القرارات العلاجية"
            },
            IsCurrent = true,
            ImpactLevel = ClinicalImpactLevel.High
        };
    }

    public bool UpdateContent() => true; // UpToDate محدث دائمًا
}

/// <I hemostatic sourceMedical> يمكن إضافة المزيد من مصادر نموذجية هنا.</summary>
public sealed class CochraneDatabase : IMedicalSource
{
    public string Name => "Cochrane Database of Systematic Reviews";
    public List<MedicalSpecialty> Specialties => new List<MedicalSpecialty> { MedicalSpecialty.InternalMedicine, MedicalSpecialty.Pediatrics, MedicalSpecialty.Cardiology };
    public int Rank => 2;
    
    public MedicalSourceResult Search(PatientData patient)
    {
        return new MedicalSourceResult
        
        {
            Title = $"Cochrane Review: {patient.Diagnosis} Intervention Effectiveness",
            Summary = $"تحليل منهجي عالي الجودة للتدخلات لعلاج {patient.Diagnosis}. يشمل تحليل الفوائد والمخاطر وتطبيقات السريرية.",
            ConfidenceScore = 0.92m,
            Source = this,
            PublicationDate = DateTime.Now.AddDays(-14),
            ContentUrl = "https://www.cochranelibrary.com",
            ContentTypes = new List<ContentType> { ContentType.SystematicReview, ContentType.MetaAnalysis },
            KeyPoints = new List<string>
            {
                "فعالية التدخلات المبنية على الأدلة",
                "مخاطر الآثار الجانبية",
                "تطبيقات إرشادية للعيادات السريرية",
                "الفجوات في المعرفة"
            },
            Warnings = new List<string>
            {
                "المراجعات المنهجية قد تحتوي على بيانات محدودة",
                "يمكن أن تتقادم التوجيهات السريرية"
            },
            IsCurrent = true,
            ImpactLevel = ClinicalImpactLevel.High
        };
    }

    public bool UpdateContent() => true;
}

// مصادر إضافية نموذجية يمكن إضافتها:
// - PubMed
// - Guidelines America
// - European Medical Societies
// - National Guidelines Institute
// - WHO Guidelines
// - Academic Medical Journals

/// <summary>
/// فئة نموذجية لمصادر طبية إضافية.
/// </summary>
public abstract class MedicalSourceTemplate : IMedicalSource
{
    public abstract string Name { get; }
    public abstract List<MedicalSpecialty> Specialties { get; }
    public abstract int Rank { get; }
    public abstract MedicalSourceResult Search(PatientData patient);
    public abstract bool UpdateContent();
}
