namespace Validex;

/// <summary>A transport-independent base response that applications may extend.</summary>
public abstract class ResponseBase
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess { get; set; }
    /// <summary>An optional description of the outcome.</summary>
    public string? Description { get; set; }
    /// <summary>Validation errors associated with the response.</summary>
    public IReadOnlyCollection<ValidationError> Errors { get; set; } = Array.Empty<ValidationError>();
}
