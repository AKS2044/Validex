namespace Validex;

/// <summary>A single validation failure.</summary>
public sealed record ValidationError
{
    /// <summary>Creates a validation error.</summary>
    public ValidationError(string propertyName, string errorMessage, string? errorCode = null)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        ErrorCode = errorCode;
    }

    /// <summary>The path of the invalid property.</summary>
    public string PropertyName { get; }
    /// <summary>A human-readable explanation of the failure.</summary>
    public string ErrorMessage { get; }
    /// <summary>An optional machine-readable error identifier.</summary>
    public string? ErrorCode { get; }
}
