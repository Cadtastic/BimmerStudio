# BimmerStudio

A cross-platform diagnostics and coding suite for older BMWs, replacing the
legacy Windows-only toolchain (Tool32, INPA, NCS Expert, WinKFP) with a single
application that runs on Windows, macOS and Linux.

Built for E-series cars first, with the vehicle-platform and transport layers
designed so F/G-series support can be added without reworking the core.

> **Status: in development.** Milestone 1 is read-only by design — the app can
> connect, run diagnostic jobs and read fault memory, but cannot write to a
> vehicle. Coding and flash programming come in later milestones behind an
> explicit safety gate.

## Why this exists

BMW Standard Tools 2.12 dates from the Windows XP era and no longer installs or
runs cleanly on Windows 11, and never ran on macOS or Linux at all. The tools
themselves are mostly thin front ends over EDIABAS, which executes per-ECU
description files (SGBDs, `.prg`). BimmerStudio keeps that model and replaces
only the front end.

## Architecture

Clean Architecture, with the dependency rule enforced by project references:

| Project | Role |
|---|---|
| `BimmerStudio.Domain` | Entities and value objects. No dependencies. |
| `BimmerStudio.Application` | Use cases and ports (interfaces). References Domain only. |
| `BimmerStudio.Infrastructure` | Persistence, catalogs, importers, logging. |
| `BimmerStudio.Infrastructure.Ediabas` | The **only** project that references EdiabasLib. |
| `BimmerStudio.App` | Avalonia UI and composition root. |

`Infrastructure.Ediabas` is an anti-corruption layer: no `EdiabasNet` type
escapes it. The rest of the app talks to `IDiagnosticConnection` /
`IDiagnosticSession`, so the interpreter can be replaced (for example by a
PSdZ-based stack for F/G cars) without touching the UI or use cases.

## Requirements

- .NET 10 SDK
- A vehicle interface: FTDI K+DCAN cable (E-series), ENET (F/G), or ELM327.
  No hardware is needed for development — the simulation transport replays
  recorded traffic.
- Your own EDIABAS/SP-Daten installation. **No BMW data ships with this repo.**

## Building

```bash
git clone --recurse-submodules https://github.com/Cadtastic/BimmerStudio.git
cd BimmerStudio
dotnet build BimmerStudio.slnx
```

If you already cloned without submodules:

```bash
git submodule update --init --recursive
```

## Vehicle data

The app never bundles BMW data. Point a workspace at your existing installation
(the folder containing `Ecu/` with `.prg` and `d_*.grp` files) and it indexes
what it finds. The same applies to NCS `DATEN` coding tables and to the legacy
help reference pack.

## Licence

GPL-3.0-or-later. BimmerStudio links [EdiabasLib](https://github.com/uholeschak/ediabaslib)
(GPL-3.0) via the `external/ediabaslib` submodule, which makes the combined work
GPL-3.0. See `LICENSE` and `THIRD-PARTY-NOTICES.md`.

Not affiliated with, endorsed by, or connected to BMW AG. "BMW", "INPA",
"NCS Expert" and "EDIABAS" are the property of their respective owners.

## Safety

Diagnostics that only read from the car are safe. Writing — clearing fault
memory, actuator tests, coding, and above all flash programming — can leave an
ECU misconfigured or unusable. Milestone 1 blocks every write path. When those
features arrive they will require explicit confirmation and an automatic backup
of the original state, and should only ever be used with a battery charger
connected.
