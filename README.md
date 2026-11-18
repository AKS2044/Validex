# Validex

Validex is a small, extensible Fluent API for validating .NET request models. It targets .NET 10, supports nullable reference types and async rules, and has no ASP.NET Core dependency.

## Installation

Install the package when it is published:

```shell
dotnet add package Validex
```

Or reference `src/Validex/Validex.csproj` from a project in this repository. The library depends only on `Microsoft.Extensions.DependencyInjection.Abstractions` for optional DI registration helpers.

### Publishing a release

Pushing a Git tag in `vMAJOR.MINOR.PATCH` format (for example, `v1.0.0`) runs the GitHub Actions workflow at `.github/workflows/publish.yml`. It tests the solution, packs the library using the tag as its package version, and publishes the package and symbols to NuGet.org. Configure a repository Actions secret named `NUGET_API_KEY` with a NuGet.org API key that has package push permissions before creating a release tag.

For a console application using the built-in Microsoft container, add `Microsoft.Extensions.DependencyInjection` to the application as well; web applications already include a container integration through their host.

## Define a model and validator

```csharp
public sealed class CreateUserRequest
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public int Age { get; init; }
    public bool RequirePhone { get; init; }
    public string? Phone { get; init; }
}

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Age)
            .GreaterThanOrEqualTo(18);

        RuleFor(request => request.Phone)
            .NotEmpty()
            .When(request => request.RequirePhone);
    }
}
```

`When` applies to the complete property rule chain. Each chain continues after a failure by default. Call `.Cascade(CascadeMode.Stop)` to stop that chain after its first error.

## Standard rules

- Strings: `NotEmpty`, `NotNull`, `MinimumLength`, `MaximumLength`, `Length`, `EmailAddress`, `Matches`.
- Comparable values: `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`.
- Collections: `NotEmpty`, `NotNull`, `MinimumCount`, `MaximumCount`.
- Any value: `NotNull`, `Equal`, `NotEqual`, `Must`.

Length checks allow null values so they can be combined with `NotNull` or `NotEmpty`. Collection count rules treat null as empty for `NotEmpty` and `MinimumCount`, and allow null for `MaximumCount`. Numeric comparisons require a non-null comparable value.

## Custom and cross-property rules

Use `Must` for domain-specific checks. Use `RuleFor(model => model)` for a check that concerns more than one property.

```csharp
RuleFor(request => request.Name)
    .Must(name => name is not null && name.StartsWith("user_", StringComparison.Ordinal))
    .WithMessage("Name must start with 'user_'.")
    .WithErrorCode("name.prefix");

RuleFor(request => request)
    .Must(request => request.StartDate < request.EndDate)
    .WithMessage("StartDate must be earlier than EndDate.");
```

Third-party rule extensions can be implemented as extension methods over `RuleBuilder<T, TProperty>` and composed using `Must`.

## Async validation

```csharp
RuleFor(request => request.Email)
    .MustAsync(async (email, cancellationToken) =>
    {
        return !await userService.ExistsAsync(email!, cancellationToken);
    })
    .WithMessage("An account already uses this email.");

var result = await validator.ValidateAsync(request, cancellationToken);
```

`ValidateAsync` passes its cancellation token to each async rule and checks cancellation between rules. Calling `Validate` on a validator containing async rules throws `InvalidOperationException`; it never blocks waiting for an async operation. Exceptions from custom rules are allowed to propagate to the caller.

## Nested models and collections

```csharp
RuleFor(request => request.Address)
    .NotNull()
    .SetValidator(new AddressValidator());

RuleForEach(request => request.Orders)
    .SetValidator(new OrderValidator());
```

Nested errors include their full path, including collection indexes, for example `Address.City` and `Orders[0].Price`. Null nested objects and collections are skipped; add `NotNull` or `NotEmpty` when they are required.

## Read validation results

```csharp
ValidationResult result = validator.Validate(request);
if (!result.IsValid)
{
    foreach (ValidationError error in result.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage} ({error.ErrorCode})");
    }
}
```

`ValidationResult` snapshots its errors and exposes a read-only collection. Each error includes a property path, message, and optional error code.

## Dependency injection

Register one validator explicitly:

```csharp
services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
```

Or scan only the assembly containing a marker type. The default lifetime is scoped; a different `ServiceLifetime` can be supplied. Repeating the scan does not add the same implementation twice.

```csharp
services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
```

## Responses

`ResponseBase` is an optional, transport-independent response model. It is not used by the validation engine and can be extended by applications.

```csharp
public sealed class ApiResponse<T>(ValidationResult validation) : ValidationResponse(validation)
{
    public T? Data { get; init; }
    public string? TraceId { get; init; }
}

var response = new ApiResponse<User>(new ValidationResult()) { Data = user };
```

`ValidationResponse` maps a `ValidationResult` to `IsSuccess`, `Errors`, and a default failure description. Applications can instead derive directly from `ResponseBase` and choose their own mapping.

## Complete small application example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Validex;

var services = new ServiceCollection();
services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
using var provider = services.BuildServiceProvider();

var validator = provider.GetRequiredService<IValidator<CreateUserRequest>>();
var request = new CreateUserRequest { Name = "A", Email = "not-an-email", Age = 16 };
var result = await validator.ValidateAsync(request);

if (result.IsValid)
{
    Console.WriteLine("The request is valid.");
}
else
{
    foreach (var error in result.Errors)
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
}
```
