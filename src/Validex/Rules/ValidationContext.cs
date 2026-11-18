namespace Validex;

internal sealed class ValidationContext
{
    public List<ValidationError> Errors { get; } = [];
}

internal interface IValidationRule<in T>
{
    bool ContainsAsyncRules { get; }
    void Validate(T instance, ValidationContext context);
    ValueTask ValidateAsync(T instance, ValidationContext context, CancellationToken cancellationToken);
}

internal sealed class RuleComponent<TProperty>(Func<TProperty, CancellationToken, ValueTask<bool>> predicate, string message, bool isAsync)
{
    public Func<TProperty, CancellationToken, ValueTask<bool>> Predicate { get; } = predicate;
    public string Message { get; set; } = message;
    public string? ErrorCode { get; set; }
    public bool IsAsync { get; } = isAsync;
}

internal sealed class PropertyRule<T, TProperty>(Func<T, TProperty> accessor, string propertyName) : IValidationRule<T>
{
    private readonly List<RuleComponent<TProperty>> _components = [];
    private Func<T, bool>? _condition;
    public bool ContainsAsyncRules => _components.Any(component => component.IsAsync);
    public string PropertyName { get; } = propertyName;
    public TProperty GetValue(T instance) => accessor(instance);

    public void Add(Func<TProperty, CancellationToken, ValueTask<bool>> predicate, string message, bool isAsync = false)
        => _components.Add(new(predicate, message, isAsync));

    public void SetMessage(string message)
    {
        if (_components.Count == 0) throw new InvalidOperationException("Add a validation rule before setting its message.");
        _components[^1].Message = message;
    }

    public void SetErrorCode(string errorCode)
    {
        if (_components.Count == 0) throw new InvalidOperationException("Add a validation rule before setting its error code.");
        _components[^1].ErrorCode = errorCode;
    }

    public void SetCondition(Func<T, bool> condition) => _condition = condition;

    public CascadeMode CascadeMode { get; set; } = CascadeMode.Continue;

    public void Validate(T instance, ValidationContext context)
    {
        if (_condition is not null && !_condition(instance)) return;
        var value = accessor(instance);
        foreach (var component in _components)
        {
            if (!component.Predicate(value, CancellationToken.None).GetAwaiter().GetResult())
            {
                context.Errors.Add(new(PropertyName, component.Message, component.ErrorCode));
                if (CascadeMode == CascadeMode.Stop) break;
            }
        }
    }

    public async ValueTask ValidateAsync(T instance, ValidationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_condition is not null && !_condition(instance)) return;
        var value = accessor(instance);
        foreach (var component in _components)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await component.Predicate(value, cancellationToken).ConfigureAwait(false))
            {
                context.Errors.Add(new(PropertyName, component.Message, component.ErrorCode));
                if (CascadeMode == CascadeMode.Stop) break;
            }
        }
    }
}

internal sealed class NestedRule<T, TProperty>(PropertyRule<T, TProperty> source, IValidator<TProperty> validator) : IValidationRule<T>
{
    public bool ContainsAsyncRules => validator is IValidatorMetadata metadata && metadata.ContainsAsyncRules;
    public void Validate(T instance, ValidationContext context)
    {
        var value = source.GetValue(instance);
        if (value is null) return;
        AddErrors(validator.Validate(value).Errors, context);
    }
    public async ValueTask ValidateAsync(T instance, ValidationContext context, CancellationToken cancellationToken)
    {
        var value = source.GetValue(instance);
        if (value is null) return;
        var result = await validator.ValidateAsync(value, cancellationToken).ConfigureAwait(false);
        AddErrors(result.Errors, context);
    }
    private void AddErrors(IEnumerable<ValidationError> errors, ValidationContext context)
    {
        foreach (var error in errors)
        {
            var path = string.IsNullOrEmpty(source.PropertyName) ? error.PropertyName :
                string.IsNullOrEmpty(error.PropertyName) ? source.PropertyName : $"{source.PropertyName}.{error.PropertyName}";
            context.Errors.Add(new(path, error.ErrorMessage, error.ErrorCode));
        }
    }
}

internal sealed class CollectionRule<T, TElement>(Func<T, IEnumerable<TElement>?> accessor, string propertyName, IValidator<TElement> validator) : IValidationRule<T>
{
    public bool ContainsAsyncRules => validator is IValidatorMetadata metadata && metadata.ContainsAsyncRules;
    public void Validate(T instance, ValidationContext context)
    {
        var items = accessor(instance);
        if (items is null) return;
        var index = 0;
        foreach (var item in items)
        {
            var result = validator.Validate(item);
            context.Errors.AddRange(result.Errors.Select(error => new ValidationError($"{propertyName}[{index}].{error.PropertyName}".TrimEnd('.'), error.ErrorMessage, error.ErrorCode)));
            index++;
        }
    }
    public async ValueTask ValidateAsync(T instance, ValidationContext context, CancellationToken cancellationToken)
    {
        var items = accessor(instance);
        if (items is null) return;
        var index = 0;
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await validator.ValidateAsync(item, cancellationToken).ConfigureAwait(false);
            context.Errors.AddRange(result.Errors.Select(error => new ValidationError($"{propertyName}[{index}].{error.PropertyName}".TrimEnd('.'), error.ErrorMessage, error.ErrorCode)));
            index++;
        }
    }
}

internal interface IValidatorMetadata { bool ContainsAsyncRules { get; } }
