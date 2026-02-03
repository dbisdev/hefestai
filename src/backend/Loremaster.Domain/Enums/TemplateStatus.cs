namespace Loremaster.Domain.Enums;

/// <summary>
/// Status of an entity template in the confirmation workflow.
/// Templates must be confirmed before they can be used to create entities.
/// </summary>
public enum TemplateStatus
{
    /// <summary>
    /// Initial state after RAG extraction. Template is being reviewed.
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Template has been submitted for review by the user.
    /// </summary>
    PendingReview = 1,
    
    /// <summary>
    /// Template has been confirmed and can be used for entity creation.
    /// </summary>
    Confirmed = 2,
    
    /// <summary>
    /// Template was rejected and cannot be used.
    /// </summary>
    Rejected = 3
}
