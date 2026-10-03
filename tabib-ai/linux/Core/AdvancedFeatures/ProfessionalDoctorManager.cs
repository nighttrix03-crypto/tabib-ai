using System;
using System.Collections.Generic;
using System.Linq;
using TabibAI.Linux.Core;
using TabibAI.Linux.Core.AdvancedFeatures;

namespace TabibAI.Linux.Core.AdvancedFeatures;

/// <summary>
/// مدير اتخاذ القرار السريري المتقدم للتطبيق Tabib AI.
/// يقدم مستويات متعددة من الثقة، والمراجع السريرية، وخيارات العلاج البديلة.
/// </summary>
public sealed class ProfessionalDoctorManager
{
    // مكونات متقدمة لدعم اتخاذ القرار السريري الاحترافي
    private readonly MedicalSourceIntegration _sourceIntegration;
    private readonly ClinicalGuidelineEngine _guidelineEngine;
    private readonly MedicalRecordIntegration _recordIntegration;
    private readonly AdvancedSearchEngine _advancedSearchEngine;

    public ProfessionalDoctorManager()
    {
        _sourceIntegration = new MedicalSourceIntegration();
        _guidelineEngine = new ClinicalGuidelineEngine();
        _recordIntegration = new MedicalRecordIntegration();
        _advancedSearchEngine = new AdvancedSearchEngine();
    }

    /// <summary>
    /// تقديم تشخيص سريري متقدم مع مستويات ثقة متعددة ومراجع.
    /// </summary>
    public AdvancedClinicalDecision SupportDiagnosis(PatientData patient, MedicalHistory history)
    {
        // التحقق من السجلات الطبية
        var medicalRecords = _recordIntegration.GetPatientRecords(patient.Id);
        
        // البحث في المصادر الطبية الموثوقة
        var relevantSources = _sourceIntegration.SearchRelevantSources(patient);
        
        // تطبيق المبادئ التوجيهية السريرية
        var applicableGuidelines = _guidelineEngine.GetApplicableGuidelines(
            patient.Diagnosis, patient.Age, patient.Sex);
        
        // البحث عن حالات مماثلة
        var similarCases = _advancedSearchEngine.FindSimilarCases(patient);
        
        // إنشاء تشخيص تفريقي
        var differentialDiagnosis = GenerateDifferentialDiagnosis(patient, history, similarCases);
        
        // حساب مستويات الثقة
        var confidenceIntervals = CalculateConfidenceIntervals(patient, differentialDiagnosis, similarCases);
        
        // توليد خيارات العلاج
        var treatmentOptions = GenerateTreatmentOptions(differentialDiagnosis, patient, applicableGuidelines);
        
        // إنشاء خطة متابعة
        var followUpPlan = CreateFollowUpPlan(patient, differentialDiagnosis);
        
        return new AdvancedClinicalDecision
        {
            PrimaryDiagnosis = differentialDiagnosis.PrimaryDiagnosis,
            DifferentialDiagnosis = differentialDiagnosis,
            ConfidenceIntervals = confidenceIntervals,
            TreatmentOptions = treatmentOptions,
            FollowUpPlan = followUpPlan,
            SourceReferences = relevantSources,
            ClinicalGuidelines = applicableGuidelines,
            SimilarCases = similarCases
        };
    }

    #region Helper Methods Private

    private DifferentialDiagnosis GenerateDifferentialDiagnosis(
        PatientData patient, MedicalHistory history, List<SimilarCase> similarCases)
    {
        var diagnoses = new List<DiagnosisWithConfidence>();
        
        // الاشتباهات الأولية
        var primarySuspects = GenerateInitialSuspects(patient, history);
        diagnoses.AddRange(primarySuspects);
        
        // الحالات المشابهة
        if (similarCases.Any())
        {
            diagnoses.AddRange(similarCases.Select(c => new DiagnosisWithConfidence
            {
                Diagnosis = c.Diagnosis,
                Confidence = c.Confidence * 0.8m,
                Reasoning = $"حالة مشابهة: {c.CaseId}",
                Urgency = c.Urgency
            }));
        }
        
        return new DifferentialDiagnosis
        {
            PrimaryDiagnosis = diagnoses.OrderByDescending(d => d.Confidence).First(),
            AlternativeDiagnoses = diagnoses.Where(d => d != diagnoses[0]).ToList(),
            ConfidenceInterval = CalculateConfidenceInterval(diagnoses),
            NextStepRecommendations = GetNextStepRecommendations(diagnoses)
        };
    }

    private ConfidenceIntervals CalculateConfidenceIntervals(
        PatientData patient, DifferentialDiagnosis differential, List<SimilarCase> similarCases)
    {
        var confidenceLevel = CalculateOverallConfidence(patient, differential, similarCases);
        var confidenceInterval = new ConfidenceInterval
        {
            LowerBound = Math.Max(0.1, confidenceLevel - 0.2),
            UpperBound = Math.Min(1.0, confidenceLevel + 0.2),
            Level = confidenceLevel,
            Interpretation = GetConfidenceInterpretation(confidenceLevel)
        };
        
        return new ConfidenceIntervals
        {
            PrimaryDiagnosis = confidenceInterval,
            AlternativeDiagnoses = GenerateAlternativeConfidenceIntervals(differential.AlternativeDiagnoses),
            UncertaintyFactors = IdentifyUncertaintyFactors(patient, differential, similarCases),
            DecisionSupport = GetDecisionSupportRecommendations(confidenceInterval)
        };
    }

    #endregion
}