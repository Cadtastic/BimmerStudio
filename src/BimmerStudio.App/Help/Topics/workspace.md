---
id: workspace
title: Workspaces
keywords: workspace, environment, SP-Daten, EDIABAS, Ecu, DATEN, path, setup
---

# Workspaces

A workspace is an environment: which kind of vehicle you are working on, and where
its data lives. You need one before you can connect.

## ECU data folder

Point this at the **`Ecu`** folder of an EDIABAS or SP-Daten installation — the one
containing `.prg` files (individual ECU variants) and `d_*.grp` files (group files).
In a standard install that is `C:\EDIABAS\Ecu`.

BimmerStudio ships no BMW data and never copies it. It reads the files where they
already are, so keeping your SP-Daten up to date is done the same way it always was.

Once the path is valid the workspace reports how many description files it found.
A typical E-series installation has a few hundred variants and several dozen groups.

## Simulation folder

Optional. Points at EDIABAS `.sim` files, which replay recorded traffic so the app
can be used with no car and no cable attached. Useful for learning the tool, and
the only way to exercise write-class jobs safely.

## Vehicle platform

E-series today. The transport layer already supports ENET, which is how F- and
G-series cars are reached, but their coding data uses a different format
(PSdZData rather than SP-Daten) that is not implemented yet.

## Why data is not bundled

SP-Daten, the ECU description files and the NCS coding tables are BMW's property.
This application is an independent tool for reading files you already have; it does
not redistribute them.
