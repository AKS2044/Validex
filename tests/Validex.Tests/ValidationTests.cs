using Microsoft.Extensions.DependencyInjection;

namespace Validex.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void Validate_returns_all_errors_and_preserves_snapshot()
    {
        var validator = new AccountValidator();
        var model = new Account { Name = "", Email = "bad", Age = 16, Tags = [] };
        var result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Equal(5, result.Errors.Count);
        Assert.Equal(2, result.Errors.Count(error => error.PropertyName == nameof(Account.Name)));
        Assert.Equal(5, new ValidationResult(result.Errors).Errors.Count);
    }

    [Fact]
    public void Standard_rules_cover_strings_numbers_collections_and_generic_values()
    {
        var validator = new AllRulesValidator();
        Assert.False(validator.Validate(new RuleModel()).IsValid);
        Assert.True(validator.Validate(new RuleModel
        {
            Required = "ok", Text = "abc", Email = "a@b.co", Pattern = "AB-12", Number = 10,
            OtherNumber = 3, NullableNumber = 2, Items = [1, 2], Optional = "same"
        }).IsValid);
    }

    [Fact]
    public void Must_when_cascade_and_model_level_rule_work()
    {
        var validator = new WindowValidator();
        Assert.Empty(validator.Validate(new Window { Start = 1, End = 2, Enabled = false }).Errors);
        var result = validator.Validate(new Window { Start = 3, End = 2, Enabled = true });
        Assert.Equal("", result.Errors.Single().PropertyName);
        Assert.Equal("Start must precede End.", result.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void Cascade_stop_keeps_only_the_first_failure_in_chain()
    {
        var result = new StopCascadeValidator().Validate(new Account { Name = "", Email = null, Age = 0 });
        Assert.Single(result.Errors);
        Assert.Equal(nameof(Account.Name), result.Errors.Single().PropertyName);
    }

    [Fact]
    public async Task Async_rules_validate_and_honor_cancellation()
    {
        var validator = new AsyncValidator();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => validator.Validate(new Account { Email = "taken" })));
        Assert.False((await validator.ValidateAsync(new Account { Email = "taken" })).IsValid);

        using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SlowAsyncValidator().ValidateAsync(new Account { Email = "free" }, source.Token));
    }

    [Fact]
    public void Nested_and_collection_errors_include_property_paths()
    {
        var validator = new OrderRequestValidator();
        var result = validator.Validate(new OrderRequest
        {
            Address = new Address { City = "" },
            Orders = [new OrderLine { Price = 0 }, new OrderLine { Price = -1 }]
        });
        Assert.Contains(result.Errors, error => error.PropertyName == "Address.City");
        Assert.Contains(result.Errors, error => error.PropertyName == "Orders[0].Price");
        Assert.Contains(result.Errors, error => error.PropertyName == "Orders[1].Price");
    }

    [Fact]
    public void Dependency_injection_scans_assembly_once_and_response_can_be_extended()
    {
        var services = new ServiceCollection();
        services.AddValidatorsFromAssemblyContaining<AccountValidator>();
        services.AddValidatorsFromAssemblyContaining<AccountValidator>();
        var accountRegistrations = services.Where(descriptor => descriptor.ServiceType == typeof(IValidator<Account>)).ToArray();
        Assert.Equal(4, accountRegistrations.Length);
        Assert.Equal(4, accountRegistrations.Select(descriptor => descriptor.ImplementationType).Distinct().Count());
        Assert.All(accountRegistrations, descriptor => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));

        var response = new ApiResponse<string>(new ValidationResult());
        Assert.True(response.IsSuccess);
        response.TraceId = "trace";
        Assert.Equal("trace", response.TraceId);
    }

    private sealed class Account { public string? Name { get; init; } public string? Email { get; init; } public int Age { get; init; } public List<string> Tags { get; init; } = []; }
    private sealed class AccountValidator : AbstractValidator<Account>
    {
        public AccountValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100).WithMessage("Name is too short.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
            RuleFor(x => x.Tags).NotNull().NotEmpty();
        }
    }
    private sealed class AllRulesValidator : AbstractValidator<RuleModel>
    {
        public AllRulesValidator()
        {
            RuleFor(x => x.Required).NotNull().NotEmpty();
            RuleFor(x => x.Text).MinimumLength(2).MaximumLength(5).Length(2, 5);
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Pattern).Matches("^[A-Z]{2}-\\d{2}$");
            RuleFor(x => x.Number).GreaterThan(5).GreaterThanOrEqualTo(6).LessThan(20).LessThanOrEqualTo(10).Equal(10).NotEqual(9);
            RuleFor(x => x.OtherNumber).NotEqual(2);
            RuleFor(x => x.NullableNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Items).NotNull().NotEmpty().MinimumCount(1).MaximumCount(3);
            RuleFor(x => x.Optional).Equal("same").NotEqual("different");
        }
    }
    private sealed class RuleModel
    {
        public string? Required { get; init; }
        public string? Text { get; init; }
        public string? Email { get; init; }
        public string? Pattern { get; init; }
        public int Number { get; init; }
        public int OtherNumber { get; init; }
        public int? NullableNumber { get; init; }
        public List<int>? Items { get; init; }
        public string? Optional { get; init; }
    }
    private sealed class Window { public int Start { get; init; } public int End { get; init; } public bool Enabled { get; init; } }
    private sealed class WindowValidator : AbstractValidator<Window>
    {
        public WindowValidator() => RuleFor(x => x).Must(window => window.Start < window.End, "Start must precede End.").When(window => window.Enabled);
    }
    private sealed class AsyncValidator : AbstractValidator<Account>
    {
        public AsyncValidator() => RuleFor(x => x.Email).MustAsync(async (email, token) =>
        {
            await Task.Delay(1, token);
            return email != "taken";
        }, "Email is already in use.");
    }
    private sealed class SlowAsyncValidator : AbstractValidator<Account>
    {
        public SlowAsyncValidator() => RuleFor(x => x.Email).MustAsync(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            return true;
        });
    }
    private sealed class StopCascadeValidator : AbstractValidator<Account>
    {
        public StopCascadeValidator() => RuleFor(x => x.Name).Cascade(CascadeMode.Stop).NotEmpty().MinimumLength(5);
    }
    private sealed class Address { public string? City { get; init; } }
    private sealed class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator() => RuleFor(x => x.City).NotEmpty();
    }
    private sealed class OrderLine { public decimal Price { get; init; } }
    private sealed class OrderLineValidator : AbstractValidator<OrderLine>
    {
        public OrderLineValidator() => RuleFor(x => x.Price).GreaterThan(0m);
    }
    private sealed class OrderRequest { public Address Address { get; init; } = null!; public List<OrderLine>? Orders { get; init; } }
    private sealed class OrderRequestValidator : AbstractValidator<OrderRequest>
    {
        public OrderRequestValidator()
        {
            RuleFor(x => x.Address).NotNull().SetValidator(new AddressValidator());
            RuleForEach(x => x.Orders).SetValidator(new OrderLineValidator());
        }
    }
    private sealed class ApiResponse<T>(ValidationResult result) : ValidationResponse(result)
    {
        public T? Data { get; set; }
        public string? TraceId { get; set; }
    }
}
