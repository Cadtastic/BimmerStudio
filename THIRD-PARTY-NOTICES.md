# Third-party notices

BimmerStudio is distributed under GPL-3.0-or-later. The components below are
redistributed or linked, under their own terms.

## EdiabasLib — GPL-3.0

- Upstream: https://github.com/uholeschak/ediabaslib
- Fork in use: https://github.com/Cadtastic/ediabaslib (`external/ediabaslib`)
- Copyright (c) Ulrich Holeschak

The EDIABAS interpreter that reads and executes BMW SGBD (`.prg`/`.grp`)
description files, and the transport implementations beneath it. This is a GPL
work; linking it is why BimmerStudio as a whole is GPL-3.0. Source for the exact
revision used is the pinned submodule commit, and the fork's changes are on the
`feature/cross-platform` branch.

## NuGet packages

| Package | Licence |
|---|---|
| Avalonia (and Avalonia.* packages) | MIT |
| CommunityToolkit.Mvvm | MIT |
| Markdown.Avalonia | MIT |
| Serilog and sinks | Apache-2.0 |
| Microsoft.Extensions.* / System.* | MIT |
| BouncyCastle.Cryptography (via EdiabasLib) | MIT |
| Newtonsoft.Json (via EdiabasLib) | MIT |
| xunit, Shouldly, coverlet (test only) | Apache-2.0 / BSD-3-Clause / MIT |

## Data and documentation not included

No BMW data ships with this repository. SGBD files, SP-Daten / NCS `DATEN`
coding tables, CABD `.ipo` files, INPA series catalogues (`CFGDAT/*.ENG`) and the
original EDIABAS/NCS/Tool32 help content remain the property of BMW AG and its
suppliers. BimmerStudio reads these from a local installation the user already
has; it neither redistributes them nor embeds their content.

## Trademarks

"BMW" is a registered trademark of BMW AG. "EDIABAS", "INPA", "NCS Expert",
"Tool32" and "WinKFP" are the property of their respective owners. This project
is an independent, unaffiliated interoperability tool.
