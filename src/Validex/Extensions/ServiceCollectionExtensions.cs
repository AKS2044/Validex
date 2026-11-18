using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Validex;

/// <summary>Dependency injection registration helpers for Validex validators.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers concrete validator implementations found in the assembly containing the marker type.</summary>
    public static IServiceCollection AddValidatorsFromAssemblyContaining<TMarker>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        => services.AddValidatorsFromAssembly(typeof(TMarker).Assembly, lifetime);

    /// <summary>Registers concrete validator implementations found in the specified assembly.</summary>
    public static IServiceCollection AddValidatorsFromAssembly(this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        var implementations = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false })
            .Where(type => type.ImplementedInterfaces.Any(IsValidatorInterface))
            .ToArray();

        foreach (var implementation in implementations)
        {
            foreach (var serviceType in implementation.ImplementedInterfaces.Where(IsValidatorInterface))
            {
                if (services.Any(descriptor => descriptor.ServiceType == serviceType && descriptor.ImplementationType == implementation.AsType()))
                    continue;
                services.Add(ServiceDescriptor.Describe(serviceType, implementation.AsType(), lifetime));
            }
        }
        return services;
    }

    private static bool IsValidatorInterface(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IValidator<>);
}
