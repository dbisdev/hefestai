namespace Loremaster.Domain.Enums;

/// <summary>
/// Type of generation request
/// </summary>
public enum GenerationRequestType
{
    /// <summary>
    /// RAG-based dice roll interpretation.
    /// </summary>
    RagDiceRoll = 0,
    
    /// <summary>
    /// AI-generated narrative content.
    /// </summary>
    AiNarrative = 1,
    
    /// <summary>
    /// PDF document import and parsing.
    /// </summary>
    PdfImport = 2,
    
    /// <summary>
    /// OCR-based image text extraction.
    /// </summary>
    OcrImport = 3,
    
    /// <summary>
    /// Entity field generation using RAG and templates.
    /// Generates field values based on game system rules.
    /// </summary>
    EntityFieldGeneration = 4,
    
    /// <summary>
    /// Entity image/avatar generation.
    /// Creates visual representation based on entity data.
    /// </summary>
    EntityImageGeneration = 5
}
