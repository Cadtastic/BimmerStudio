using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using BimmerStudio.Infrastructure.Ediabas.Transports;
using EdiabasLib;
using Microsoft.Extensions.Logging;

namespace BimmerStudio.Infrastructure.Ediabas;

/// <summary>
/// Creates connections by pairing an interpreter with the transport a profile names.
/// </summary>
public sealed class EdiabasConnectionFactory : IDiagnosticConnectionFactory
{
    private readonly IReadOnlyDictionary<string, IEdiabasInterfaceFactory> _interfaceFactories;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<EdiabasConnectionFactory> _logger;

    public EdiabasConnectionFactory(
        IEnumerable<IEdiabasInterfaceFactory> interfaceFactories,
        ILoggerFactory loggerFactory)
    {
        _interfaceFactories = interfaceFactories.ToDictionary(
            factory => factory.TransportId,
            StringComparer.OrdinalIgnoreCase);
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<EdiabasConnectionFactory>();

        // SGBD text is Windows-1252; without this every umlaut decodes to a replacement char.
        EdiabasEncoding.EnsureRegistered();
    }

    public Task<IDiagnosticConnection> ConnectAsync(
        ConnectionProfile profile,
        Workspace workspace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(workspace);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_interfaceFactories.TryGetValue(profile.TransportId, out var interfaceFactory))
        {
            throw new DiagnosticConnectionException(
                $"No transport is registered for '{profile.TransportId}'. "
                + $"Available: {string.Join(", ", _interfaceFactories.Keys.Order())}.");
        }

        if (!Directory.Exists(workspace.EcuDataPath))
        {
            throw new DiagnosticConnectionException(
                $"The ECU data folder '{workspace.EcuDataPath}' does not exist. "
                + "Point the workspace at the Ecu folder of an EDIABAS or SP-Daten installation.");
        }

        EdiabasNet? ediabas = null;
        try
        {
            ediabas = new EdiabasNet
            {
                EdInterfaceClass = interfaceFactory.CreateInterface(profile, workspace),
                AbortJobFunc = null,
            };

            ediabas.SetConfigProperty("EcuPath", workspace.EcuDataPath);
            if (!string.IsNullOrWhiteSpace(workspace.TracePath))
            {
                ediabas.SetConfigProperty("TracePath", workspace.TracePath);
            }

            interfaceFactory.ConfigureRuntime(ediabas, profile, workspace);

            _logger.LogInformation(
                "Opened {Transport} connection '{Profile}' against {EcuPath}",
                profile.TransportId,
                profile.Name,
                workspace.EcuDataPath);

            var connection = new EdiabasConnection(
                profile,
                ediabas,
                _loggerFactory.CreateLogger<EdiabasConnection>());

            return Task.FromResult<IDiagnosticConnection>(connection);
        }
        catch (Exception ex)
        {
            ediabas?.Dispose();

            if (ex is DiagnosticConnectionException)
            {
                throw;
            }

            throw new DiagnosticConnectionException(
                $"Could not open connection '{profile.Name}': {ex.Message}", ex);
        }
    }
}
