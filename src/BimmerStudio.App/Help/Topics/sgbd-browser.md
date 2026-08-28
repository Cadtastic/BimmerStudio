---
id: sgbd-browser
title: ECU browser
keywords: SGBD, job, Tool32, prg, grp, group file, variant, run, results, browser
---

# ECU browser

The equivalent of Tool32: load an ECU description file, look at the jobs it
offers, run one, read the results.

## Loading a description file

Two kinds of file appear in the list:

- **Variants** (`.prg`) — one specific ECU, for example `CAS` or `MSV70`. Most can
  be opened with no car attached, so you can browse jobs offline.
- **Group files** (`.grp`, named `d_*`) — a family, for example `d_motor`. Opening
  one asks the vehicle which variant is actually fitted, so it needs a live
  connection. The resolved variant is shown once it succeeds.

Some variants also require a connection. Engine and transmission files in
particular (MSV70, MSD80, GS19, the DDE family) run an initialisation job that
talks to the ECU before anything else can happen. When that is the case the app
says so rather than reporting an error — it is normal, not a fault.

## The job list

Read out of the file you loaded. Every ECU offers a different set, so this list
is not something BimmerStudio defines.

Selecting a job shows what the description file documents about it: its purpose,
its arguments, and the results it returns. Many files carry no documentation at
all — the blocks are optional and often omitted — so an empty description is
common and not a sign of a problem.

Press **F1** with a job selected for help composed from that ECU's own
documentation plus its safety classification.

## Arguments

Typed exactly as EDIABAS expects: semicolons between values, no quoting. The
argument list for the selected job tells you what it wants. Jobs that take no
arguments ignore the field.

## Running

- **Run once** — executes the job a single time.
- **Run continuously** — re-runs it on an interval, for watching a value change.
  While a continuous job is running the connection is reserved, so other work
  waits until you stop it.

Only read-class jobs run against a real car. See [Safety](safety).

## Results

EDIABAS returns a **system set** describing the call itself — `JOBSTATUS`,
`VARIANTE`, sometimes `UBATT` — followed by zero or more **data sets** carrying the
payload, one per record. A fault-memory read returns one data set per stored
fault.

`JOBSTATUS` is how EDIABAS reports job-level failure. A job that runs but reports
a non-OK status has not errored; it has told you something, and the status text is
usually the most informative part of the result.
