# Status — end of session 1

Written at commit `a805ebf` on `main`. Everything below is verified against the real
SP-Daten installation at `BMW Exx CODING\3-UPDATE\EDIABAS\Ecu` (279 `.prg`, 80 `.grp`).

## Where things stand

The Tool32 half of milestone 1 is done and working. You can point the app at an EDIABAS
installation, connect, browse any ECU's jobs with localised descriptions, see what a job
takes and returns before running it, run it, and read the results — all strictly read-only,
on Windows, macOS or Linux.

The INPA half — vehicle scan and live values — has not been started. That is the next
session's work.

| | |
|---|---|
| Repo | https://github.com/Cadtastic/BimmerStudio — `main` only, no open branches |
| Interpreter fork | https://github.com/Cadtastic/ediabaslib — `master`, pinned at `3f12a815b` |
| Size | 88 source files / ~5,200 lines, 13 test files / ~1,450 lines |
| Tests | **146 passing** (59 domain, 65 application, 22 integration) |
| Localisation | **5,730 phrase translations**; job descriptions 99.95% readable, all comment text 95.58% |
| Build | `dotnet build BimmerStudio.slnx`, .NET 10, Avalonia 11.3.20 |

Integration tests read a real `Ecu` folder via `BIMMERSTUDIO_ECU_PATH` and skip themselves
when it is unset, so CI stays green without any BMW data committed.

```bash
git clone --recurse-submodules https://github.com/Cadtastic/BimmerStudio.git
```

## What exists

**Diagnostics core.** A fork of EdiabasLib, retargeted to portable `net8.0`/`net10.0` so it
runs off Windows, wrapped in an anti-corruption layer: no `EdiabasNet` type escapes
`Infrastructure.Ediabas`. Each connection owns a dedicated worker thread fed by a channel, so
callers get `Task`s and real mid-job cancellation via the interpreter's `AbortJobFunc`.

**Transports.** K+DCAN serial, ENET/DoIP, ELM327 and simulation, behind
`IEdiabasInterfaceFactory`. Adding one is a single registration.

**Safety.** Every job is classified from its name before it can run; only `Read` and
`CommInit` execute against hardware. `Unknown` is treated as a write.

**UI.** Avalonia, MVVM, dark. Workspace and connection setup, ECU picker grouped by vehicle
module, job list with safety badges, argument and result documentation, run once/continuous,
per-job results.

**Localisation.** English and German packs; a new language is one JSON file dropped into
`languages/`. UI strings, help topics, composed job help and a 535-entry dictionary for the
German text inside SGBDs all follow the selection. Job and result *names* never translate.

**Help.** F1 is context-sensitive via a `Help.TopicId` attached property; job help is composed
at request time from that ECU's own documentation plus its safety verdict.

## Translation coverage, and how to measure it

Run the inventory tool with `--missing <pack>` for a coverage report, adding
`--job-comments` to restrict it to the text under each job name:

```
dotnet run --project tools/BimmerStudio.SgbdInventory -- <EcuPath>   --phrases out.tsv --missing src/BimmerStudio.App/Assets/Languages/en.json --job-comments
```

**Read "readable coverage", not "percent translated."** Roughly half of any
untranslated remainder is protocol service names, EDIABAS table references and job-name
tokens that must render verbatim in every language, and much of the rest was written in
English by BMW. Counting those as gaps once made a finished job list look 55% done.

| | Job descriptions | All comment text |
|---|---|---|
| Readable | **99.95%** | **95.58%** |
| Real German gap | 17 lines / 32 occ | 6,474 lines / 12,278 occ |

Job descriptions are done: the 17 remaining lines are truncated fragments where the
governing verb sits on an adjacent line, and any translation would be invention. The
4.42% gap in the wider set is entirely in **argument and result documentation** — the
byte-layout tables and value-range notes — which is where further effort would go.
The classifier behind these numbers is a heuristic and says so in its output: trust the
shape of the table, not an individual row.

To continue: filter the emitted TSV to the `UntranslatedGerman` class, split it into
chunks of ~500, and hand each to an agent with the brief used before (skip protocol
identifiers, table references, hex and English; keep `$xx` prefixes and translate the
German after them; skip anything ambiguous). Merge results with
`scripts/Merge-PhraseTranslations.ps1`, never by hand — its header records four ways
that merge has silently destroyed or discarded data.

## Facts worth not rediscovering

These cost real time to establish and are all measured, not assumed.

- **Reserved metadata jobs are `_ARGUMENTS` and `_RESULTS`**, not `_JOBARGS`/`_JOBRESULTS`
  despite the `ARG*`/`RESULT*` result keys. The wrong names fail with `EDIABAS_SYS_0008`.
- **A leading underscore does not mean "reserved".** SGBDs ship `_COD_SCHREIBEN`,
  `_HISTORY_LOESCHEN`, `_FLASH_COMICRO`. An early classifier treated all `_`-prefixed jobs as
  safe reads and would have offered **flash programming as a harmless read**. Membership is an
  exact-name set in `ReservedJobNames`.
- **~16% of variants cannot be opened offline.** Engine and transmission files (MSV70, MSD80,
  GS19, DDE) run `INITIALISIERUNG` on first execution, which talks to the ECU. Surfaced as
  `VehicleConnectionRequiredException`, not an error.
- **74 of 80 group files need a vehicle**, because opening one runs `IDENTIFIKATION` to
  discover the fitted variant. The six that work offline (`d_virt*`, `d_0099`) are
  virtual-ECU stubs. Groups are disabled in the picker when no vehicle can answer.
- **`EdiabasNet` keeps process-wide static state** — `SharedDataDict` is cleared when the last
  instance is disposed. Concurrent connections corrupt each other and fail on a random SGBD
  each run. **One connection at a time per process.** The test assembly disables parallelism to
  match.
- **A real race was fixed in the fork**: `_trapBitDict` was initialised outside the
  constructor's lock and published before completion. Worth upstreaming to Uwe.
- **SGBD text is Windows-1252**, and .NET does not carry that codec off Windows.
  `EdiabasEncoding.EnsureRegistered()` runs at composition.
- **FTDI uses the virtual COM port, not D2XX.** Consequence: 5-baud slow init for the oldest
  K-line ECUs is unavailable off Windows. D-CAN cars are unaffected.
- **Comment text is tabular.** Lines are the translation unit, and tab or double-space runs
  are column gaps. Flattening them produces the run-on prose we already fixed once.

## Next: the INPA environment (WP7)

The goal is what INPA gave you: connect to a car and see, per ECU, whether it responds, what
it is, and what faults it has stored — plus a live-value view.

**Vehicle scan.** `VehicleScanService` walks a series' ECU list, opens each, runs `IDENT` then
`FS_LESEN`, and builds a `ScanReport`. The pieces that already exist: session management,
safety classification (both jobs are `Read`), the `VehicleConnectionRequiredException` path
for ECUs that are simply not fitted, and per-job result plumbing.

Three things to decide:

1. **Which ECUs to try.** Three sources, probably combined: an app-authored series → group
   list; an importer for the INPA `CFGDAT/*.ENG` catalogues already on this machine (format
   confirmed: `[ROOT_*]` sections, `ENTRY=SGBD,Description` — these are BMW's content, so
   import locally, never redistribute); or a live sweep of candidate `d_*.grp` files. The
   sweep is the only one that needs no catalogue, and group resolution already identifies the
   fitted variant.
2. **How to render DTCs.** `FS_LESEN` returns one data set per fault, typically with
   `F_ORT_NR`/`F_ORT_TEXT` and environmental conditions. Map to a `DtcEntry` but keep the raw
   sets — the mapping is best-effort across ECU generations.
3. **Scan pacing.** One connection, one worker thread, so the scan is inherently sequential.
   It needs `IProgress` and clean cancellation; a full sweep will take a while.

**Live values.** `ExecuteJobContinuousAsync` already streams with real cancellation. What is
missing is a view: pick `STATUS_*` jobs and result fields, show tiles and a chart, log to CSV.
`LiveChartsCore` is already in `Directory.Packages.props` but not yet referenced.

**Also worth doing early.** Workspace persistence exists but only for one implicit workspace —
named workspaces are still to come. And nothing has been run against a real car yet: the
K+DCAN path is implemented and unit-tested but unproven on hardware. A read-only `IDENT` on
your own E-series would be the highest-value single test in the project.

## Later milestones

- **M2** — Tool32 `.tst` scripts, UDS argument wizard via `_TABLE`/`_TABLES`, trace viewer,
  live-session `.sim` capture.
- **M3** — NCS Expert coding: DATEN parsing (`.C0x`, `SGDAT\*.ipo`, `BR_REF.DAT`), FA/VO and
  ZCS, FSW/PSW read and diff preview *before* any write, then writes behind the armed
  `IWriteGate` with a mandatory backup.
- **M4** — WinKFP flashing. Last, bench-tested on sacrificial ECUs before any car.

## Open questions

- **Dictionary coverage** is 54.5% of all comment-line occurrences (72% of German-flagged).
  Growing it is adding lines to `en.json`; the `--phrases` inventory ranks what is worth doing
  next.
- **Module map** names 191 of 359 SGBDs. The rest show their raw code under
  "Other / unrecognised", which is deliberate — a confident wrong label is worse than none.
- **GPL-3.0** applies to the whole app because it links EdiabasLib. Fine for an open tool;
  it forecloses a closed-source product without a clean-room interpreter.
- **Trademark** — avoid "BMW" in any public product name.
