namespace Validex;

/// <summary>A convenience response populated from a validation result.</summary>
public class ValidationResponse : ResponseBase
{
    /// <summary>Creates a response for the supplied result.</summary>
    public ValidationResponse(ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        IsSuccess = result.IsValid;
        Errors = result.Errors;
        Description = result.IsValid ? null : "Validation failed.";
    }
}
