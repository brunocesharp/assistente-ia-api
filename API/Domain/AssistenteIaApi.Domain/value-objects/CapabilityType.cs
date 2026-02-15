namespace AssistenteIaApi.Domain.ValueObjects;

public enum CapabilityType
{
    LLM_Generation = 0,
    LLM_Classification = 1,
    LLM_Reasoning = 2,
    Vision_OCR = 3,
    Vision_ObjectDetection = 4,
    Embedding_Search = 5,
    RuleEngine = 6,
    ExternalIntegration = 7
}
