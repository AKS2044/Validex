namespace Validex;

/// <summary>Builds a validation chain for one model property.</summary>
public sealed class RuleBuilder<T, TProperty>
{
    private readonly PropertyRule<T, TProperty> _rule;
    private readonly Action<IValidationRule<T>> _addRule;
    internal RuleBuilder(PropertyRule<T, TProperty> rule, Action<IValidationRule<T>> addRule) { _rule = rule; _addRule = addRule; }

    /// <summary>Sets the message for the last rule in this chain.</summary>
    public RuleBuilder<T, TProperty> WithMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _rule.SetMessage(message);
        return this;
    }

    /// <summary>Sets an optional error code for the last rule in this chain.</summary>
    public RuleBuilder<T, TProperty> WithErrorCode(string errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        _rule.SetErrorCode(errorCode);
        return this;
    }

    /// <summary>Runs the complete chain only when the condition is true.</summary>
    public RuleBuilder<T, TProperty> When(Func<T, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _rule.SetCondition(condition);
        return this;
    }

    /// <summary>Sets how this chain proceeds after a rule fails.</summary>
    public RuleBuilder<T, TProperty> Cascade(CascadeMode cascadeMode)
    {
        _rule.CascadeMode = cascadeMode;
        return this;
    }

    /// <summary>Adds a synchronous predicate to this chain.</summary>
    public RuleBuilder<T, TProperty> Must(Func<TProperty, bool> predicate, string message = "The value is invalid.")
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _rule.Add((value, _) => ValueTask.FromResult(predicate(value)), message);
        return this;
    }

    /// <summary>Adds an asynchronous predicate to this chain.</summary>
    public RuleBuilder<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicate, string message = "The value is invalid.")
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _rule.Add(async (value, token) => await predicate(value, token).ConfigureAwait(false), message, isAsync: true);
        return this;
    }

    /// <summary>Validates a non-null property with another validator.</summary>
    public RuleBuilder<T, TProperty> SetValidator(IValidator<TProperty> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _addRule(new NestedRule<T, TProperty>(_rule, validator));
        return this;
    }
}

/// <summary>Builds validation for each item in a collection property.</summary>
public sealed class CollectionRuleBuilder<T, TElement>
{
    private readonly Action<IValidator<TElement>> _setValidator;
    internal CollectionRuleBuilder(Action<IValidator<TElement>> setValidator) => _setValidator = setValidator;

    /// <summary>Validates each non-null item with the supplied validator.</summary>
    public CollectionRuleBuilder<T, TElement> SetValidator(IValidator<TElement> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _setValidator(validator);
        return this;
    }
}
