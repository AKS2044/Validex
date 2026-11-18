using System.Linq.Expressions;

namespace Validex;

/// <summary>Base class for validators configured with the Validex Fluent API.</summary>
public abstract class AbstractValidator<T> : IValidator<T>, IValidatorMetadata
{
    private readonly List<IValidationRule<T>> _rules = [];
    bool IValidatorMetadata.ContainsAsyncRules => _rules.Any(rule => rule.ContainsAsyncRules);

    /// <summary>Creates a rule chain for the selected property.</summary>
    public RuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var member = expression.Body is MemberExpression direct ? direct :
            expression.Body is UnaryExpression { Operand: MemberExpression converted } ? converted : null;
        var propertyName = member?.Member.Name ?? string.Empty;
        var rule = new PropertyRule<T, TProperty>(expression.Compile(), propertyName);
        _rules.Add(rule);
        return new(rule, nestedRule => _rules.Add(nestedRule));
    }

    /// <summary>Creates a rule for each item in a collection property.</summary>
    public CollectionRuleBuilder<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>?>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var member = expression.Body as MemberExpression;
        var propertyName = member?.Member.Name ?? string.Empty;
        return new(validator => _rules.Add(new CollectionRule<T, TElement>(expression.Compile(), propertyName, validator)));
    }

    /// <inheritdoc />
    public ValidationResult Validate(T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (((IValidatorMetadata)this).ContainsAsyncRules)
            throw new InvalidOperationException("This validator contains asynchronous rules. Use ValidateAsync instead.");
        var context = new ValidationContext();
        foreach (var rule in _rules) rule.Validate(instance, context);
        return new(context.Errors);
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        cancellationToken.ThrowIfCancellationRequested();
        var context = new ValidationContext();
        foreach (var rule in _rules)
            await rule.ValidateAsync(instance, context, cancellationToken).ConfigureAwait(false);
        return new(context.Errors);
    }
}
