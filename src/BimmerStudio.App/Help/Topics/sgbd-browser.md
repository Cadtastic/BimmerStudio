---
id: sgbd-browser
title: ECU browser
keywords: SGBD, job, Tool32, prg, grp, group file, variant, run, results, browser
---

# ECU browser

The equivalent of Tool32: load an ECU description file, look at the jobs it
offers, run one, read the results.

## Variants and group files

Selecting an ECU loads it immediately. Two kinds of file appear in the list, each
tagged in the picker.

**Variants** (`.prg`, tagged *Variant*) describe one specific ECU — `CAS`,
`MSV70`, `MSD80`. This is where the jobs live. A typical E-series installation has
a few hundred of them, and most can be opened with no car attached, so you can
read an ECU's job list offline.

**Group files** (`.grp`, named `d_*`, tagged *Group*) describe a *family* —
`d_motor` for the engine, `d_kombi` for the instrument cluster. A group file is a
dispatcher, not a lesser ECU: it contains the logic to ask the car which variant
is actually fitted, and once it knows, the session behaves exactly as if you had
loaded that variant directly. The variant it resolved is shown under the job list.

That makes groups the entry point you usually want with a car connected, because
you rarely know in advance whether a given 3-series has an MSV70 or an MSD80 —
`d_motor` finds out for you. It is also why they are not filtered out of the list.

The trade-off is that a group cannot be inspected offline: identifying the ECU
means talking to it. **On a simulation connection, group files are therefore shown
greyed out**, marked "needs a vehicle", rather than offered and then failing.
Connect through a K+DCAN cable, ENET or an ELM327 adapter and they become
available.

Of the 80 group files in a typical installation, about six — the stripped-down
"virtual ECU" ones — would technically open without a car, but they expose only a
generic set (`IDENT`, `INFO`, `INITIALISIERUNG`) that any variant offers anyway,
so they are greyed out with the rest rather than singled out by name.

Some variants need a connection too. Engine and transmission files in particular
(MSV70, MSD80, GS19, the DDE family) run an initialisation job that talks to the
ECU before anything else can happen. In both cases the app says a vehicle is
needed rather than reporting an error — it is normal, not a fault.

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

Typed exactly as EDIABAS expects: **positional values separated by semicolons**,
no quoting, no names. What goes in each position is defined by the job.

The panel under the argument line shows what the selected job declares: each
argument's name, its type, and the description the file carries for it. About
half of all jobs declare arguments; the rest genuinely take none, and the panel
says so. **Insert template** pre-fills the line with one placeholder per
argument — zeros for numeric types, `?` where you must supply a value — so the
slot count is right before you start editing.

The declared results are shown below the run buttons before the first execution,
so you know what a job returns before running it.

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
