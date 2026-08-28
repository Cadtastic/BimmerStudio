using System.Text.Json;
using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;
using BimmerStudio.Domain.Vehicles;
using BimmerStudio.Infrastructure.Ediabas;
using BimmerStudio.Infrastructure.Ediabas.Transports;
using Microsoft.Extensions.Logging.Abstractions;

// Surveys an EDIABAS Ecu folder: what jobs exist, how they classify, and how much of the
// argument and result documentation the description files actually carry.
//
//   BimmerStudio.SgbdInventory <ecuPath> [outputJson] [--sgbd NAME] [--limit N]
//                              [--phrases out.tsv]
//
// --phrases writes every distinct comment line with its occurrence count, frequency-sorted,
// as the raw material for a language pack's data-phrase dictionary. Lines are the unit
// because the same line recurs across thousands of jobs while full comments vary.

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: BimmerStudio.SgbdInventory <ecuPath> [output.json] [--sgbd NAME] [--limit N]");
    return 1;
}

var ecuPath = args[0];
if (!Directory.Exists(ecuPath))
{
    Console.Error.WriteLine($"'{ecuPath}' does not exist.");
    return 1;
}

var outputPath = args.Length > 1 && !args[1].StartsWith("--", StringComparison.Ordinal) ? args[1] : null;
var onlySgbd = ValueAfter("--sgbd");
var limit = int.TryParse(ValueAfter("--limit"), out var parsed) ? parsed : int.MaxValue;
var phrasesPath = ValueAfter("--phrases");
// Points at a language pack: the phrase inventory is then filtered to lines that pack does
// not yet translate, which is the list worth working through.
var dictionaryPath = ValueAfter("--missing");
var jobCommentsOnly = args.Contains("--job-comments");
// Group files are compiled SGBDs too; surveying them shows what a group actually exposes.
var pattern = args.Contains("--groups") ? "*.grp" : "*.prg";

var classifier = new JobSafetyClassifier();
var factory = new EdiabasConnectionFactory([new SimulationInterfaceFactory()], NullLoggerFactory.Instance);

var workspace = new Workspace(Guid.NewGuid(), "inventory", VehiclePlatform.ESeries, ecuPath, ecuPath);
var profile = ConnectionProfile.Create("inventory", TransportIds.Simulation,
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [SimulationTransportSettings.SimulationPath] = ecuPath,
    });

await using var connection = await factory.ConnectAsync(profile, workspace);

var files = Directory.GetFiles(ecuPath, pattern, SearchOption.TopDirectoryOnly)
    .Where(file => onlySgbd is null
        || Path.GetFileNameWithoutExtension(file).Equals(onlySgbd, StringComparison.OrdinalIgnoreCase))
    .Take(limit)
    .ToList();

var report = new List<SgbdReport>();
var needsVehicle = 0;

foreach (var file in files)
{
    var name = Path.GetFileNameWithoutExtension(file);

    try
    {
        await using var session = await connection.OpenSessionAsync(SgbdIdentifier.Variant(name));
        var jobs = await session.GetJobsAsync();

        var described = new List<JobReport>(jobs.Count);
        foreach (var job in jobs)
        {
            var detail = await session.DescribeJobAsync(job.Name);
            described.Add(new JobReport(
                detail.Name,
                classifier.Classify(detail.Name).ToString(),
                [.. detail.Comments],
                [.. detail.Arguments.Select(ToParameter)],
                [.. detail.Results.Select(ToParameter)]));
        }

        report.Add(new SgbdReport(name, session.ResolvedVariant, described));
        Console.WriteLine($"{name,-24} {described.Count,4} jobs");
    }
    catch (VehicleConnectionRequiredException)
    {
        needsVehicle++;
    }
    catch (DiagnosticConnectionException ex)
    {
        Console.Error.WriteLine($"{name}: {ex.Message}");
    }
}

PrintSummary(report, needsVehicle, files.Count);

if (outputPath is not null)
{
    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(outputPath, json);
    Console.WriteLine($"\nWrote {outputPath}");
}

if (phrasesPath is not null)
{
    await WritePhraseInventoryAsync(report, phrasesPath, LoadDictionaryKeys(dictionaryPath), jobCommentsOnly);
}

return 0;

// Existing translations, so the inventory can report only what is still missing.
static HashSet<string>? LoadDictionaryKeys(string? packPath)
{
    if (packPath is null)
    {
        return null;
    }

    using var stream = File.OpenRead(packPath);
    using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    });

    var keys = new HashSet<string>(StringComparer.Ordinal);
    foreach (var property in document.RootElement.GetProperty("dataPhrases").EnumerateObject())
    {
        keys.Add(BimmerStudio.Infrastructure.Localization.TextNormaliser.NormaliseWhitespace(property.Name));
    }

    return keys;
}

static async Task WritePhraseInventoryAsync(
    List<SgbdReport> report,
    string path,
    HashSet<string>? alreadyTranslated,
    bool jobCommentsOnly)
{
    // Length cap: beyond it, lines are one-off prose (buffer layout essays), which no
    // dictionary should carry — and which a translation memory would never hit anyway.
    const int maxLength = 100;
    const int minLength = 3;

    var counts = new Dictionary<string, int>(StringComparer.Ordinal);

    void Count(string? text)
    {
        foreach (var line in (text ?? string.Empty).Split('\n'))
        {
            var normalised = BimmerStudio.Infrastructure.Localization.TextNormaliser
                .NormaliseWhitespace(line);

            if (normalised.Length is < minLength or > maxLength)
            {
                continue;
            }

            counts[normalised] = counts.GetValueOrDefault(normalised) + 1;
        }
    }

    foreach (var job in report.SelectMany(sgbd => sgbd.Jobs))
    {
        foreach (var comment in job.Comments)
        {
            Count(comment);
        }

        // Job comments alone when asked: they are what the job list and the job header show,
        // so they dominate what a user actually reads. Ranking every comment line by frequency
        // optimises total occurrences instead, which can leave a whole ECU untranslated —
        // ACC2's descriptions are rare globally but they are the entire screen when it is open.
        if (jobCommentsOnly)
        {
            continue;
        }

        foreach (var parameter in job.Arguments.Concat(job.Results))
        {
            Count(parameter.Comment);
        }
    }

    var all = counts
        .OrderByDescending(entry => entry.Value)
        .ThenBy(entry => entry.Key, StringComparer.Ordinal)
        .ToList();

    var total = all.Sum(entry => (long)entry.Value);

    var ordered = alreadyTranslated is null
        ? all
        : all.Where(entry => !alreadyTranslated.Contains(entry.Key)).ToList();

    var lines = ordered.Select(entry => $"{entry.Value}\t{entry.Key}");
    await File.WriteAllLinesAsync(path, lines);

    if (alreadyTranslated is not null)
    {
        var translated = total - ordered.Sum(entry => (long)entry.Value);
        Console.WriteLine(
            $"\nCoverage: {translated * 100.0 / total:F1}% of {total} occurrences already translated");
        Console.WriteLine($"Untranslated: {ordered.Count} distinct lines -> {path}");
    }
    else
    {
        Console.WriteLine($"\nPhrase inventory: {ordered.Count} distinct lines, {total} occurrences -> {path}");
    }

    // The coverage curve is what decides how many phrases are worth working through.
    var remaining = ordered.Sum(entry => (long)entry.Value);
    foreach (var top in new[] { 100, 250, 500, 1000, 1500, 2000, 3000 })
    {
        if (top > ordered.Count || remaining == 0)
        {
            break;
        }

        var covered = ordered.Take(top).Sum(entry => (long)entry.Value);
        Console.WriteLine($"  next {top,5}: {covered * 100.0 / remaining:F1}% of what remains"
            + $" ({covered * 100.0 / total:F1}% of everything)");
    }
}

static ParameterReport ToParameter(JobParameterInfo info) =>
    new(info.Name, info.Type, info.Comment);

string? ValueAfter(string flag)
{
    var index = Array.IndexOf(args, flag);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static void PrintSummary(List<SgbdReport> report, int needsVehicle, int total)
{
    var jobs = report.SelectMany(sgbd => sgbd.Jobs).ToList();
    var distinct = jobs.DistinctBy(job => job.Name, StringComparer.OrdinalIgnoreCase).ToList();

    Console.WriteLine($"\n--- {report.Count} of {total} SGBDs read offline ({needsVehicle} need a vehicle) ---");
    Console.WriteLine($"{jobs.Count} job entries, {distinct.Count} distinct names\n");

    Console.WriteLine("Safety classification of distinct names:");
    foreach (var group in distinct.GroupBy(job => job.Safety).OrderByDescending(group => group.Count()))
    {
        Console.WriteLine($"  {group.Key,-12} {group.Count(),6}  ({group.Count() * 100.0 / distinct.Count:F1}%)");
    }

    Console.WriteLine("\nDocumentation coverage (all job entries):");
    Report("with a comment", jobs.Count(job => job.Comments.Length > 0), jobs.Count);
    Report("with arguments declared", jobs.Count(job => job.Arguments.Length > 0), jobs.Count);
    Report("with argument comments", jobs.Count(job => job.Arguments.Any(a => a.Comment is not null)), jobs.Count);
    Report("with results declared", jobs.Count(job => job.Results.Length > 0), jobs.Count);

    var argTypes = jobs.SelectMany(job => job.Arguments)
        .Where(argument => argument.Type is not null)
        .GroupBy(argument => argument.Type!, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(group => group.Count())
        .Take(12);

    Console.WriteLine("\nMost common argument types:");
    foreach (var group in argTypes)
    {
        Console.WriteLine($"  {group.Key,-16} {group.Count(),6}");
    }

    Console.WriteLine("\nSample job comments (the text a user actually reads):");
    foreach (var group in jobs
                 .SelectMany(job => job.Comments)
                 .Where(comment => comment.Length > 8)
                 .GroupBy(comment => comment, StringComparer.OrdinalIgnoreCase)
                 .OrderByDescending(group => group.Count())
                 .Take(15))
    {
        Console.WriteLine($"  {group.Count(),4}x  {group.Key}");
    }

    static void Report(string label, int count, int total) =>
        Console.WriteLine($"  {label,-26} {count,6} / {total}  ({count * 100.0 / Math.Max(total, 1):F1}%)");
}

internal sealed record SgbdReport(string Name, string ResolvedVariant, List<JobReport> Jobs);

internal sealed record JobReport(
    string Name,
    string Safety,
    string[] Comments,
    ParameterReport[] Arguments,
    ParameterReport[] Results);

internal sealed record ParameterReport(string Name, string? Type, string? Comment);
