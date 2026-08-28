---
id: glossary
title: Glossary
keywords: glossary, SGBD, ZCS, FA, FSW, PSW, CABD, VO, terms, abbreviations, German
---

# Glossary

BMW's diagnostic vocabulary is German and heavily abbreviated. These are the terms
that appear in the tools and in the data.

| Term | Meaning |
|---|---|
| **SGBD** | *Steuergerätebeschreibungsdatei* — ECU description file. The `.prg` that tells EDIABAS how to talk to one ECU and what jobs it offers. |
| **SG** | *Steuergerät* — control unit, ECU. |
| **Job** | A named operation inside an SGBD. Not a feature of the application; each ECU defines its own. |
| **EDIABAS** | The runtime that executes SGBDs and handles the protocol. Everything else is a front end over it. |
| **BEST/2** | The language SGBDs are written in. Compiled to the bytecode in a `.prg`. |
| **Group file** | A `.grp` (named `d_*`) covering an ECU family. Loading one makes EDIABAS ask the car which variant is fitted. |
| **Variant** | One concrete ECU description, as opposed to a group. |
| **Baureihe** | Model series — E90, E46, F10. |
| **FG / FGNR** | *Fahrgestellnummer* — chassis number, the VIN. |
| **ZCS** | *Zentrale Codierschlüssel* — the central coding key holding a car's factory equipment. |
| **FA** | *Fahrzeugauftrag* — vehicle order. The build record: model, options, equipment codes. Also called the VO. |
| **VO** | Vehicle order. The English name for the FA. |
| **SA** | *Sonderausstattung* — optional equipment code, for example `S639A` for navigation. |
| **FSW** | *Funktionsschlüsselwort* — function keyword. Names one codeable option. |
| **PSW** | *Parameterschlüsselwort* — parameter keyword. The value that option is set to. |
| **FSW/PSW** | Together, one coding setting: which option, and what it is set to. |
| **CABD** | *Codierablaufbeschreibungsdatei* — coding sequence description. Drives how an ECU gets coded. |
| **NETTODATEN** | Net data — the raw coding bytes actually written to an ECU. |
| **DATEN** | The SP-Daten coding tables NCS Expert reads, describing what each ECU can be coded to. |
| **SP-Daten** | The E-series data package: SGBDs plus coding data. |
| **PSdZData** | The F/G-series equivalent. A different format; not yet supported here. |
| **DTC** | Diagnostic trouble code — an entry in the fault memory. |
| **FS** | *Fehlerspeicher* — fault memory. Hence `FS_LESEN` (read) and `FS_LOESCHEN` (clear). |
| **UDS** | Unified Diagnostic Services, the modern ISO protocol. Newer ECUs; standard jobs such as `STATUS_LESEN`. |
| **KWP2000 / DS2** | Older BMW diagnostic protocols. DS2 is the oldest still commonly met on E-series. |
| **D-CAN** | Diagnostics over CAN, used from roughly 2007. |
| **K-line** | The older single-wire diagnostic bus. Timing-sensitive. |
| **ENET** | Ethernet diagnostics (DoIP) for F- and G-series cars. |
| **I-Stufe** | Integration level — the software baseline a car or ECU is on. |
| **Codierindex** | Coding index. Identifies which coding data version an ECU expects. |
