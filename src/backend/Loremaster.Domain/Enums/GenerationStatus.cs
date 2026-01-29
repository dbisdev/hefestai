namespace Loremaster.Domain.Enums;

/// <summary>
/// Status of a generation request or import
/// </summary>
public enum GenerationStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
