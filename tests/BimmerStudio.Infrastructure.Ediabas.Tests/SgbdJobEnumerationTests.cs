using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Infrastructure.Ediabas.Transports;
using Microsoft.Extensions.Logging.Abstractions;

namespace BimmerStudio.Infrastructure.Ediabas.Tests;

/// <summary>
/// Exercises the session engine against real SGBD files.
/// </summary>
/// <remarks>
/// Reading an SGBD's own metadata jobs never touches the vehicle, so these run with no car and
/// no simulation: they prove the interpreter, the worker-thread wrapper and the result mapping
/// against the real thing rather than a stand-in.
/// </remarks>
public sealed class SgbdJobEnumerationTests(EcuDataFixture fixture) : IClassFixture<EcuDataFixture>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [SkippableFact]
    public async Task Enumerates_jobs_of_a_real_sgbd()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        var sgbd = PickSgbd();
        await using var connection = await ConnectAsync();
        await using var session = await connection.OpenSessionAsync(sgbd);

        var jobs = await session.GetJobsAsync(new CancellationTokenSource(Timeout).Token);

        jobs.ShouldNotBeEmpty();
        jobs.Select(job => job.Name).ShouldBeUnique();
        session.ResolvedVariant.ShouldNotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task Describes_a_job_with_its_documented_results()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var connection = await ConnectAsync();
        await using var session = await connection.OpenSessionAsync(PickSgbd());

        var jobs = await session.GetJobsAsync(new CancellationTokenSource(Timeout).Token);
        var identJob = jobs.FirstOrDefault(job =>
            job.Name.StartsWith("IDENT", StringComparison.OrdinalIgnoreCase)) ?? jobs[0];

        var described = await session.DescribeJobAsync(
            identJob.Name, new CancellationTokenSource(Timeout).Token);

        described.Name.ShouldBe(identJob.Name);

        // A job that reads identification data must declare results; arguments are optional.
        described.Results.ShouldNotBeEmpty();
        described.Results.ShouldAllBe(result => !string.IsNullOrWhiteSpace(result.Name));
    }

    [SkippableFact]
    public async Task Describing_an_undocumented_job_yields_empty_metadata_rather_than_failing()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var connection = await ConnectAsync();
        await using var session = await connection.OpenSessionAsync(PickSgbd());

        var described = await session.DescribeJobAsync(
            "NOT_A_DOCUMENTED_JOB", new CancellationTokenSource(Timeout).Token);

        described.Comments.ShouldBeEmpty();
        described.Arguments.ShouldBeEmpty();
        described.Results.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task Reports_reserved_metadata_jobs_separately()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var connection = await ConnectAsync();
        await using var session = await connection.OpenSessionAsync(PickSgbd());

        var jobs = await session.GetJobsAsync(new CancellationTokenSource(Timeout).Token);

        // _JOBS lists real jobs only, so nothing reserved should surface as a runnable job.
        jobs.ShouldAllBe(job => !job.IsReserved);
    }

    [SkippableFact]
    public async Task Loading_an_unknown_sgbd_fails_with_a_domain_exception()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var connection = await ConnectAsync();

        var open = async () => await connection.OpenSessionAsync(
            SgbdIdentifier.Variant("definitely_not_a_real_sgbd"));

        // The point of the anti-corruption layer: no EdiabasLib exception type escapes.
        await open.ShouldThrowAsync<DiagnosticConnectionException>();
    }

    [SkippableFact]
    public async Task Cancelling_before_execution_does_not_run_the_job()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        await using var connection = await ConnectAsync();
        await using var session = await connection.OpenSessionAsync(PickSgbd());

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var execute = async () => await session.ExecuteJobAsync(
            JobRequest.For("IDENT"), cancelled.Token);

        await execute.ShouldThrowAsync<OperationCanceledException>();
    }

    [SkippableFact]
    public async Task Every_sgbd_either_enumerates_offline_or_reports_needing_a_vehicle()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        // Sweeps the whole corpus, so an SGBD the interpreter cannot handle shows up here rather
        // than as a crash in the UI. Two outcomes are acceptable: jobs enumerate from the file,
        // or the SGBD says it needs a car. Anything else is a defect.
        await using var connection = await ConnectAsync();

        var enumerated = 0;
        var needsVehicle = new List<string>();
        var unexpected = new List<string>();
        var totalJobs = 0;

        foreach (var file in fixture.SgbdFiles)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            try
            {
                await using var session = await connection.OpenSessionAsync(
                    SgbdIdentifier.Variant(name),
                    new CancellationTokenSource(Timeout).Token);

                var jobs = await session.GetJobsAsync(new CancellationTokenSource(Timeout).Token);
                totalJobs += jobs.Count;
                enumerated++;
            }
            catch (VehicleConnectionRequiredException)
            {
                needsVehicle.Add(name);
            }
            catch (Exception ex)
            {
                unexpected.Add($"{name}: {ex.GetType().Name} {ex.Message}");
            }
        }

        unexpected.ShouldBeEmpty(
            $"{unexpected.Count} of {fixture.SgbdFiles.Count} SGBDs failed for an unclassified reason");

        // Most ECU description files are fully inspectable with no car present; that is what
        // makes offline job browsing possible at all.
        enumerated.ShouldBeGreaterThan(fixture.SgbdFiles.Count / 2);
        totalJobs.ShouldBeGreaterThan(enumerated);
    }

    private Task<IDiagnosticConnection> ConnectAsync()
    {
        var factory = new EdiabasConnectionFactory(
            [new SimulationInterfaceFactory(), new SerialInterfaceFactory()],
            NullLoggerFactory.Instance);

        // Metadata jobs are answered from the file, so the transport is never used. Simulation
        // is chosen because it cannot reach a vehicle even by accident.
        var profile = ConnectionProfile.Create(
            "test-sim",
            TransportIds.Simulation,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [SimulationTransportSettings.SimulationPath] = fixture.EcuPath!,
            });

        return factory.ConnectAsync(profile, fixture.CreateWorkspace(fixture.EcuPath));
    }

    /// <summary>
    /// Prefers CAS, the Car Access System, because every late E-series car has one and it
    /// carries a large, varied job list. Falls back to whatever is present.
    /// </summary>
    private SgbdIdentifier PickSgbd()
    {
        var preferred = fixture.SgbdFiles.FirstOrDefault(file =>
            Path.GetFileNameWithoutExtension(file).Equals("CAS", StringComparison.OrdinalIgnoreCase));

        return SgbdIdentifier.Variant(
            Path.GetFileNameWithoutExtension(preferred ?? fixture.SgbdFiles[0]));
    }
}
