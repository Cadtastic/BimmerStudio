---
id: overview
title: BimmerStudio
keywords: help, start, introduction, F1
---

# BimmerStudio

A single application replacing the legacy BMW toolchain — Tool32, INPA, NCS Expert
and WinKFP — running on Windows, macOS and Linux.

Press **F1** anywhere for help on what you are looking at. **Shift+F1** opens this
help browser with search.

## How the pieces fit together

Three things have to be in place before you can talk to a car:

1. **A workspace** — points at your EDIABAS or SP-Daten installation, so the app
   knows where the ECU description files live.
2. **A connection** — how the car is reached: a K+DCAN cable, ENET, an ELM327
   adapter, or a simulation that needs no hardware at all.
3. **An SGBD** — the description file for the ECU you want to talk to.

## The one idea worth understanding

**Jobs are not features of this application.** They live inside each ECU
description file (the SGBD), and every ECU exposes a different set. `FS_LESEN`
reads fault memory on almost any ECU, but the engine controller in an E90 offers
jobs the door module has never heard of.

This means the job list you see is read out of the file you loaded, not from a
list BimmerStudio ships. It also means job names are German protocol identifiers
defined by BMW, so they are always shown exactly as the SGBD declares them.

## Language

The display language is chosen in the left panel and takes effect immediately.
Languages are packs: the shipped ones are English and German, and a new language
is a single JSON file dropped into the `languages` folder next to the
application — no reinstall.

Two kinds of text behave differently. The application's own labels translate
fully, including the help you are reading and the job help composed from an ECU's
own documentation. Text that comes out of the vehicle data — job descriptions,
argument notes — is German at the source; the English pack translates the common
phrases and shows the original as a tooltip, and anything unrecognised is shown
verbatim rather than hidden. Job names never translate, in any language: they are
the protocol, and they must match what every other tool and forum shows.

## Read-only for now

The app currently blocks anything that could change your car. Reading is safe;
writing is not, and the write paths are being built with the care they need. See
[Safety](safety).
