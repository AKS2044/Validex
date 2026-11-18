using System.Text.RegularExpressions;
using System.Collections;

namespace Validex;

/// <summary>Standard validation rules for Validex rule chains.</summary>
public static class RuleBuilderExtensions
{
    /// <summary>Requires a value to be non-null.</summary>
    public static RuleBuilder<T, TProperty> NotNull<T, TProperty>(this RuleBuilder<T, TProperty> builder)
        => builder.Must(value => value is not null, "The value must not be null.");

    /// <summary>Requires a value to equal the expected value.</summary>
    public static RuleBuilder<T, TProperty> Equal<T, TProperty>(this RuleBuilder<T, TProperty> builder, TProperty expected)
        => builder.Must(value => EqualityComparer<TProperty>.Default.Equals(value, expected), "The value must equal the expected value.");

    /// <summary>Requires a value to differ from the expected value.</summary>
    public static RuleBuilder<T, TProperty> NotEqual<T, TProperty>(this RuleBuilder<T, TProperty> builder, TProperty unexpected)
        => builder.Must(value => !EqualityComparer<TProperty>.Default.Equals(value, unexpected), "The value must not equal the specified value.");

    /// <summary>Requires a string to contain a non-whitespace character.</summary>
    public static RuleBuilder<T, string?> NotEmpty<T>(this RuleBuilder<T, string?> builder)
        => builder.Must(value => !string.IsNullOrWhiteSpace(value), "The value must not be empty.");

    /// <summary>Requires a string to have at least the specified length. Null values pass this rule.</summary>
    public static RuleBuilder<T, string?> MinimumLength<T>(this RuleBuilder<T, string?> builder, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        return builder.Must(value => value is null || value.Length >= length, $"The value must be at least {length} characters long.");
    }

    /// <summary>Requires a string to have no more than the specified length. Null values pass this rule.</summary>
    public static RuleBuilder<T, string?> MaximumLength<T>(this RuleBuilder<T, string?> builder, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        return builder.Must(value => value is null || value.Length <= length, $"The value must be at most {length} characters long.");
    }

    /// <summary>Requires a string length to fall within the inclusive bounds. Null values pass this rule.</summary>
    public static RuleBuilder<T, string?> Length<T>(this RuleBuilder<T, string?> builder, int min, int max)
    {
        if (min < 0 || max < min) throw new ArgumentOutOfRangeException(nameof(min), "Length bounds must be non-negative and max must be greater than or equal to min.");
        return builder.Must(value => value is null || value.Length >= min && value.Length <= max, $"The value must be between {min} and {max} characters long.");
    }

    /// <summary>Requires a string to resemble an email address. Null values pass this rule.</summary>
    public static RuleBuilder<T, string?> EmailAddress<T>(this RuleBuilder<T, string?> builder)
        => builder.Must(value => value is null || Regex.IsMatch(value, @"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.CultureInvariant), "The value is not a valid email address.");

    /// <summary>Requires a string to match a regular expression. Null values pass this rule.</summary>
    public static RuleBuilder<T, string?> Matches<T>(this RuleBuilder<T, string?> builder, string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var regex = new Regex(pattern, RegexOptions.CultureInvariant);
        return builder.Must(value => value is null || regex.IsMatch(value), "The value does not match the required pattern.");
    }

    /// <summary>Requires a comparable value to be greater than the specified value.</summary>
    public static RuleBuilder<T, TProperty> GreaterThan<T, TProperty>(this RuleBuilder<T, TProperty> builder, TProperty value) where TProperty : IComparable<TProperty>
        => builder.Must(candidate => candidate is not null && candidate.CompareTo(value) > 0, "The value must be greater than the specified value.");

    /// <summary>Requires a nullable comparable value to be greater than the specified value.</summary>
    public static RuleBuilder<T, TProperty?> GreaterThan<T, TProperty>(this RuleBuilder<T, TProperty?> builder, TProperty value) where TProperty : struct, IComparable<TProperty>
        => builder.Must(candidate => candidate.HasValue && candidate.Value.CompareTo(value) > 0, "The value must be greater than the specified value.");

    /// <summary>Requires a comparable value to be greater than or equal to the specified value.</summary>
    public static RuleBuilder<T, TProperty> GreaterThanOrEqualTo<T, TProperty>(this RuleBuilder<T, TProperty> builder, TProperty value) where TProperty : IComparable<TProperty>
        => builder.Must(candidate => candidate is not null && candidate.CompareTo(value) >= 0, "The value must be greater than or equal to the specified value.");

    /// <summary>Requires a nullable comparable value to be greater than or equal to the specified value.</summary>
    public static RuleBuilder<T, TProperty?> GreaterThanOrEqualTo<T, TProperty>(this RuleBuilder<T, TProperty?> builder, TProperty value) where TProperty : struct, IComparable<TProperty>
        => builder.Must(candidate => candidate.HasValue && candidate.Value.CompareTo(value) >= 0, "The value must be greater than or equal to the specified value.");

    /// <summary>Requires a comparable value to be less than the specified value.</summary>
    public static RuleBuilder<T, TProperty> LessThan<T, TProperty>(this RuleBuilder<T, TProperty> builder, TProperty value) where TProperty : IComparable<TProperty>
        => builder.Must(candidate => candidate is not null && candidate.CompareTo(value) < 0, "The value must be less than the specified value.");

    /// <summary>Requires a nullable comparable value to be less than the specified value.</summary>
    public static RuleBuilder<T, TProperty?> LessThan<T, TProperty>(this RuleBuilder<T, TProperty?> builder, TProperty value) where TProperty : struct, IComparable<TProperty>
        => builder.Must(candidate => candidate.HasValue && candidate.Value.CompareTo(value) < 0, "The value must be less than the specified value.");

    /// <summary>Requires a comparable value to be less than or equal to the specified value.</summary>
    public static RuleBuilder<T, TProperty> LessThanOrEqualTo<T, TProperty>(this RuleBuilder<T, TProperty> builder, TProperty value) where TProperty : IComparable<TProperty>
        => builder.Must(candidate => candidate is not null && candidate.CompareTo(value) <= 0, "The value must be less than or equal to the specified value.");

    /// <summary>Requires a nullable comparable value to be less than or equal to the specified value.</summary>
    public static RuleBuilder<T, TProperty?> LessThanOrEqualTo<T, TProperty>(this RuleBuilder<T, TProperty?> builder, TProperty value) where TProperty : struct, IComparable<TProperty>
        => builder.Must(candidate => candidate.HasValue && candidate.Value.CompareTo(value) <= 0, "The value must be less than or equal to the specified value.");

    /// <summary>Requires a collection to contain at least one item.</summary>
    public static RuleBuilder<T, TCollection> NotEmpty<T, TCollection>(this RuleBuilder<T, TCollection> builder)
        => builder.Must(value => value is IEnumerable collection && collection.Cast<object?>().Any(), "The collection must not be empty.");

    /// <summary>Requires a collection to contain at least the specified number of items.</summary>
    public static RuleBuilder<T, TCollection> MinimumCount<T, TCollection>(this RuleBuilder<T, TCollection> builder, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return builder.Must(value => value is IEnumerable collection && collection.Cast<object?>().Count() >= count, $"The collection must contain at least {count} items.");
    }

    /// <summary>Requires a collection to contain no more than the specified number of items.</summary>
    public static RuleBuilder<T, TCollection> MaximumCount<T, TCollection>(this RuleBuilder<T, TCollection> builder, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return builder.Must(value => value is null || value is IEnumerable collection && collection.Cast<object?>().Count() <= count, $"The collection must contain at most {count} items.");
    }
}
