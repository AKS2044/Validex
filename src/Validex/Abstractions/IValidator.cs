namespace Validex;

/// <summary>Validates instances of a model.</summary>
/// <typeparam name="T">The model type.</typeparam>
public interface IValidator<in T>
{
    /// <summary>Validates an instance synchronously. Throws when the validator contains asynchronous rules.</summary>
    ValidationResult Validate(T instance);

    /// <summary>Validates an instance asynchronously.</summary>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}

/// <summary>Controls whether a rule chain continues after a failure.</summary>
public enum CascadeMode
{
    /// <summary>Run every component in the chain.</summary>
    Continue,
    /// <summary>Stop the chain after its first failure.</summary>
    Stop
}
