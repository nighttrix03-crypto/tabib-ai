using System;

namespace TabibAI.Linux.Core.Services;

/// <summary>
/// Model information for selection.
/// </summary>
public record ModelInfo(
    string Key,
    string Name,
    string Description,
    double SizeGB,
    int MinRamMB,
    int RecommendedRamMB,
    int MinVramMB,
    string[] RequiredInstructionSets,
    ModelTier Tier,
    ModelUseCase[] UseCases,
    string HuggingFaceRepo,
    string Quantization,
    int ContextLength
);

/// <summary>
/// Model tier classification.
/// </summary>
public enum ModelTier
{
    UltraLight,    // < 2GB RAM - gemma2-2b
    Light,         // 2-4GB RAM - meditron-7b
    Standard,      // 4-8GB RAM - llama3-8b
    Heavy,         // 8-16GB RAM - larger models
    UltraHeavy     // > 16GB RAM - 70B+ models
}

/// <summary>
/// Model use case classification.
/// </summary>
public enum ModelUseCase
{
    GeneralChat,
    MedicalQA,
    ClinicalReasoning,
    RadiologyAnalysis,
    LabAnalysis,
    EmergencyTriage,
    Multilingual,
    CodeGeneration,
    Reasoning
}