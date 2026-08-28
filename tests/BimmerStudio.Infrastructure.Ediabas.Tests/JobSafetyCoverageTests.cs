using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;
using BimmerStudio.Infrastructure.Ediabas.Transports;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace BimmerStudio.Infrastructure.Ediabas.Tests;

/// <summary>
/// Measures the safety classifier against every job name in a real SGBD corpus.
/// </summary>
/// <remarks>
/// The classifier decides which jobs the UI will run, so its accuracy is a safety property, not
/// a nicety. Unit tests prove the rules behave as written; only the real corpus shows whether
/// those rules actually cover the vocabulary BMW uses. Too many unclassified jobs would make the
/// app block everything and be useless, so the coverage floor is asserted here.
/// </remarks>
public sealed class JobSafetyCoverageTests(EcuDataFixture fixture, ITestOutputHelper output)
    : IClassFixture<EcuDataFixture>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [SkippableFact]
    public async Task Classifier_recognises_the_overwhelming_majority_of_real_job_names()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        var classifier = new JobSafetyClassifier();
        var jobNames = await CollectJobNamesAsync();

        Skip.If(jobNames.Count == 0, "No jobs could be enumerated offline.");

        var byCategory = jobNames
            .GroupBy(classifier.Classify)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var safety in Enum.GetValues<JobSafety>())
        {
            var count = byCategory.TryGetValue(safety, out var jobs) ? jobs.Count : 0;
            output.WriteLine($"{safety,-12} {count,6}  ({count * 100.0 / jobNames.Count:F1}%)");
        }

        var unknown = byCategory.TryGetValue(JobSafety.Unknown, out var unclassified)
            ? unclassified
            : [];

        output.WriteLine($"\nDistinct job names: {jobNames.Count}");
        output.WriteLine($"Unclassified sample: {string.Join(", ", unknown.Take(40))}");

        var unknownShare = unknown.Count * 100.0 / jobNames.Count;
        unknownShare.ShouldBeLessThan(
            25.0,
            $"{unknown.Count} of {jobNames.Count} job names are unclassified. "
            + "Unknown jobs are blocked as writes, so a high share makes the app unusable.");
    }

    [SkippableFact]
    public async Task No_destructive_job_in_the_corpus_is_classified_as_read()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        var classifier = new JobSafetyClassifier();
        var jobNames = await CollectJobNamesAsync();

        Skip.If(jobNames.Count == 0, "No jobs could be enumerated offline.");

        // The failure that would actually hurt: a job that erases, writes or flashes being
        // waved through as a read. Checked against real names rather than invented ones.
        var misclassified = jobNames
            .Where(name =>
                classifier.Classify(name).IsReadOnly()
                && (name.Contains("LOESCH", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("SCHREIB", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("CODIER", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("FLASH", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("STEUERN", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        misclassified.ShouldBeEmpty(
            $"These write-class jobs would be treated as safe reads: {string.Join(", ", misclassified)}");
    }

    private async Task<IReadOnlyList<string>> CollectJobNamesAsync()
    {
        var factory = new EdiabasConnectionFactory(
            [new SimulationInterfaceFactory()],
            NullLoggerFactory.Instance);

        var profile = ConnectionProfile.Create(
            "coverage",
            TransportIds.Simulation,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [SimulationTransportSettings.SimulationPath] = fixture.EcuPath!,
            });

        await using var connection = await factory.ConnectAsync(
            profile, fixture.CreateWorkspace(fixture.EcuPath));

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in fixture.SgbdFiles)
        {
            try
            {
                await using var session = await connection.OpenSessionAsync(
                    SgbdIdentifier.Variant(Path.GetFileNameWithoutExtension(file)),
                    new CancellationTokenSource(Timeout).Token);

                foreach (var job in await session.GetJobsAsync(new CancellationTokenSource(Timeout).Token))
                {
                    names.Add(job.Name);
                }
            }
            catch (DiagnosticConnectionException)
            {
                // SGBDs needing a live vehicle contribute nothing offline; covered elsewhere.
            }
        }

        return [.. names];
    }
}
