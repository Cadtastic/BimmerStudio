# Folds agent-translated phrase files into en.json.
#
# Two bugs are deliberately designed out of this script, both of which silently destroyed data:
#
#  1. Never build JSON by string surgery. An earlier version spliced entries in with -replace,
#     and in a .NET replacement string `$0` means "the entire match", so every phrase containing
#     a dollar-digit sequence was corrupted (". $0A Tankinhalt" -> "}A Tankinhalt").
#  2. Never use PowerShell's default hashtables for these keys. ConvertFrom-Json -AsHashtable
#     and [ordered]@{} are case-insensitive, so "OKAY, wenn fehlerfrei" and
#     "OKAY, wenn Fehlerfrei" collapse into one entry and a translation is lost. The application
#     compares phrase keys with StringComparer.Ordinal, so this script must too.
#
# Guards, because a bad merge is worse than a missing translation:
#   - keys already present are never overwritten
#   - keys not actually present in the corpus are rejected (agent invention)
#   - duplicates across agents are reported and taken once
#   - protocol identifiers and table references are refused even if an agent proposed one

param(
    [Parameter(Mandatory)] [string] $PackPath,
    [Parameter(Mandatory)] [string] $CorpusTsv,
    [Parameter(Mandatory)] [string[]] $AgentFiles
)

$ErrorActionPreference = 'Stop'

function New-OrdinalMap {
    [System.Collections.Specialized.OrderedDictionary]::new([System.StringComparer]::Ordinal)
}

# Reads a JSON object's string properties, preserving exact keys and their order.
function Read-JsonMap([System.Text.Json.JsonElement] $element) {
    $map = New-OrdinalMap
    foreach ($property in $element.EnumerateObject()) {
        $map[$property.Name] = $property.Value.GetString()
    }
    return $map
}

$options = [System.Text.Json.JsonDocumentOptions]::new()
$options.CommentHandling = [System.Text.Json.JsonCommentHandling]::Skip
$options.AllowTrailingCommas = $true

$packDocument = [System.Text.Json.JsonDocument]::Parse((Get-Content $PackPath -Raw), $options)
$root = $packDocument.RootElement

$packId = $root.GetProperty('id').GetString()
$packName = $root.GetProperty('displayName').GetString()
$existingUi = Read-JsonMap $root.GetProperty('ui')
$existingPhrases = Read-JsonMap $root.GetProperty('dataPhrases')

# Every phrase that actually occurs in the SGBDs, so invented keys can be rejected.
$corpus = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($line in [IO.File]::ReadLines($CorpusTsv)) {
    $parts = $line.Split("`t", 2)
    if ($parts.Count -eq 2) { [void]$corpus.Add($parts[1]) }
}

$accepted = New-OrdinalMap
$stats = [ordered]@{ proposed = 0; alreadyPresent = 0; notInCorpus = 0; refused = 0; duplicate = 0; unchanged = 0 }

# Categories that must render verbatim no matter what an agent proposed.
#
# A leading `$xx` is deliberately NOT one of them. SGBDs write "$04 Selbsttest" and
# "$1602 DSC Sensor-Cluster lesen", where the prefix is an identifier but the German after it
# genuinely needs translating; refusing every `$hex` key once rejected 92% of a valid batch.
# Deciding which words are German is the translator's judgement. This list catches only
# mechanical category errors, and the no-op check below catches identifiers left untranslated.
$refuse = @(
    '^table\s',
    '^\$[0-9A-Fa-f]+\s*$',
    '^(KWP\s?2000|KWP2000|UDS)\s*[:.]',
    '^(ZZZ|BBxh|PPPxV|PPPx|dxxxx)$'
)

foreach ($file in $AgentFiles) {
    if (-not (Test-Path $file)) { Write-Warning "missing: $file"; continue }

    $agentDocument = [System.Text.Json.JsonDocument]::Parse((Get-Content $file -Raw), $options)
    $entries = Read-JsonMap $agentDocument.RootElement

    foreach ($key in $entries.Keys) {
        $stats.proposed++

        if ($existingPhrases.Contains($key)) { $stats.alreadyPresent++; continue }
        if (-not $corpus.Contains($key))     { $stats.notInCorpus++;    continue }
        if ($accepted.Contains($key))        { $stats.duplicate++;      continue }

        $blocked = $false
        foreach ($pattern in $refuse) {
            if ($key -cmatch $pattern) { $blocked = $true; break }
        }
        if ($blocked) { $stats.refused++; continue }

        # An agent that correctly leaves an identifier alone returns it unchanged. That is not a
        # translation, so it must not enter the dictionary as one.
        if ($entries[$key].Trim() -ceq $key.Trim()) { $stats.unchanged++; continue }

        $accepted[$key] = $entries[$key]
    }

    $agentDocument.Dispose()
}

$packDocument.Dispose()

foreach ($k in $stats.Keys) { "{0,-16} {1}" -f $k, $stats[$k] }
"accepted         $($accepted.Count)"

if ($accepted.Count -eq 0) { return }

# Existing phrases in their original order, then the new ones.
$merged = New-OrdinalMap
foreach ($key in $existingPhrases.Keys) { $merged[$key] = $existingPhrases[$key] }
foreach ($key in $accepted.Keys)        { $merged[$key] = $accepted[$key] }

# Written with a real JSON writer: it escapes quotes, backslashes and control characters, and
# treats `$` as an ordinary character.
$stream = [System.IO.File]::Create($PackPath)
try {
    $writerOptions = [System.Text.Json.JsonWriterOptions]::new()
    $writerOptions.Indented = $true
    # Relaxed so German umlauts stay readable instead of becoming \uXXXX escapes.
    $writerOptions.Encoder = [System.Text.Encodings.Web.JavaScriptEncoder]::UnsafeRelaxedJsonEscaping

    $writer = [System.Text.Json.Utf8JsonWriter]::new($stream, $writerOptions)
    try {
        $writer.WriteStartObject()
        $writer.WriteString('id', $packId)
        $writer.WriteString('displayName', $packName)

        $writer.WriteStartObject('ui')
        foreach ($key in $existingUi.Keys) { $writer.WriteString($key, $existingUi[$key]) }
        $writer.WriteEndObject()

        $writer.WriteStartObject('dataPhrases')
        foreach ($key in $merged.Keys) { $writer.WriteString($key, $merged[$key]) }
        $writer.WriteEndObject()

        $writer.WriteEndObject()
        $writer.Flush()
    }
    finally { $writer.Dispose() }
}
finally { $stream.Dispose() }

"wrote $PackPath ($($merged.Count) phrases)"
