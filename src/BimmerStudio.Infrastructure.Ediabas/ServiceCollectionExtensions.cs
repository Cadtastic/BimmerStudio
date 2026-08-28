using BimmerStudio.Application.Abstractions;
using BimmerStudio.Infrastructure.Ediabas.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BimmerStudio.Infrastructure.Ediabas;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the EdiabasLib-backed diagnostics stack and its built-in transports.
    /// </summary>
    /// <remarks>
    /// A new transport is one more <c>IEdiabasInterfaceFactory</c> registration. Replacing the
    /// interpreter entirely means registering a different
    /// <see cref="IDiagnosticConnectionFactory"/> instead of calling this.
    /// </remarks>
    public static IServiceCollection AddEdiabasDiagnostics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        EdiabasEncoding.EnsureRegistered();

        services.TryAddEnumerable(
        [
            ServiceDescriptor.Singleton<IEdiabasInterfaceFactory, SerialInterfaceFactory>(),
            ServiceDescriptor.Singleton<IEdiabasInterfaceFactory, EnetInterfaceFactory>(),
            ServiceDescriptor.Singleton<IEdiabasInterfaceFactory, Elm327InterfaceFactory>(),
            ServiceDescriptor.Singleton<IEdiabasInterfaceFactory, SimulationInterfaceFactory>(),
        ]);

        services.TryAddSingleton<IDiagnosticConnectionFactory, EdiabasConnectionFactory>();

        return services;
    }
}
