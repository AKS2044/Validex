namespace Validex;

/// <summary>The immutable outcome of validating a model.</summary>
public sealed class ValidationResult
{
    /// <summary>Creates a result from a snapshot of the supplied errors.</summary>
    public ValidationResult(IEnumerable<ValidationError>? errors = null)
    {
        Errors = Array.AsReadOnly((errors ?? []).ToArray());
    }

    /// <summary>Whether validation completed without errors.</summary>
    public bool IsValid => Errors.Count == 0;
    /// <summary>All validation errors.</summary>
    public IReadOnlyCollection<ValidationError> Errors { get; }
}
