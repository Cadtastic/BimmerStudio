---
id: safety
title: Safety
keywords: safety, read-only, write, risk, brick, coding, flash, danger
---

# Safety

Reading from a car is safe. Writing to one is not, and the difference is worth
taking seriously: a wrong coding value leaves an ECU misconfigured, and an
interrupted flash can leave it unusable.

## What the app allows today

BimmerStudio is **read-only**. Every job is classified before it can run, and only
two categories are permitted against a real vehicle:

| Category | Meaning | Allowed |
|---|---|---|
| Read | Reads data. Cannot change anything. | Yes |
| CommInit | Opens communication. Changes nothing. | Yes |
| MemoryClear | Erases stored data such as fault memory. | No |
| Actuator | Drives a physical output. | No |
| Coding | Writes coding data. | No |
| Flash | Reprograms firmware. | No |
| Unknown | Could not be classified. | No |

## Why Unknown is blocked

Job names are the only signal available before running a job, and running one to
find out what it does is exactly what must not happen. BMW's naming is consistent
enough to classify most jobs — `_LESEN` reads, `_LOESCHEN` erases, `STEUERN_`
actuates — but roughly one name in six does not match any known pattern.

Those are treated as writes. An unrecognised job is most likely harmless, but
"most likely" is not a good enough basis for touching an ECU, so it is blocked and
labelled rather than guessed at.

A leading underscore does **not** mean a job is safe, incidentally. Real ECU files
ship jobs such as `_COD_SCHREIBEN` and `_FLASH_COMICRO` that write coding data and
reprogram flash.

## Simulation

Against a simulation, every job is permitted — there is no car to damage. The
title bar shows which kind of connection is active, and blocked jobs explain
why they are blocked.

## When writing arrives

Coding and flashing are planned, behind an explicit confirmation step and an
automatic backup of the original state. Whenever you do use them, on any tool:

- Connect a battery charger. Coding with a weak battery is how ECUs get damaged.
- Read and save the current state first.
- Do not interrupt a flash. That is the one operation with no easy recovery.
