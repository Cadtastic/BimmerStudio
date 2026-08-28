---
id: glossary
title: Glossar
keywords: glossar, SGBD, ZCS, FA, FSW, PSW, CABD, VO, begriffe, abkürzungen
---

# Glossar

Die Abkürzungen der BMW-Diagnose, wie sie in den Werkzeugen und in den Daten
auftauchen.

| Begriff | Bedeutung |
|---|---|
| **SGBD** | *Steuergerätebeschreibungsdatei*. Die `.prg`, die EDIABAS mitteilt, wie mit einem Steuergerät zu sprechen ist und welche Jobs es bietet. |
| **SG** | *Steuergerät*. |
| **Job** | Eine benannte Operation innerhalb einer SGBD. Keine Funktion der Anwendung — jedes Steuergerät definiert eigene. |
| **EDIABAS** | Die Laufzeitumgebung, die SGBDs ausführt und das Protokoll abwickelt. Alles andere ist eine Oberfläche darüber. |
| **BEST/2** | Die Sprache, in der SGBDs geschrieben sind. Übersetzt in den Bytecode einer `.prg`. |
| **Gruppendatei** | Eine `.grp` (benannt `d_*`) für eine Steuergerätefamilie. Beim Laden fragt EDIABAS das Fahrzeug nach der verbauten Variante. |
| **Variante** | Eine konkrete Steuergerätebeschreibung, im Gegensatz zur Gruppe. |
| **Baureihe** | Modellreihe — E90, E46, F10. |
| **FG / FGNR** | *Fahrgestellnummer*, die VIN. |
| **ZCS** | *Zentrale Codierschlüssel*: die werkseitige Ausstattung eines Fahrzeugs. |
| **FA** | *Fahrzeugauftrag*: Modell, Ausstattung, Optionen. Englisch auch VO. |
| **VO** | *Vehicle Order*, die englische Bezeichnung des FA. |
| **SA** | *Sonderausstattung*, z. B. `S639A` für Navigation. |
| **FSW** | *Funktionsschlüsselwort*: benennt eine codierbare Option. |
| **PSW** | *Parameterschlüsselwort*: der Wert, auf den die Option gesetzt ist. |
| **FSW/PSW** | Zusammen eine Codiereinstellung: welche Option, und worauf gesetzt. |
| **CABD** | *Codierablaufbeschreibungsdatei*: steuert, wie ein Steuergerät codiert wird. |
| **NETTODATEN** | Die rohen Codierbytes, die tatsächlich geschrieben werden. |
| **DATEN** | Die SP-Daten-Codiertabellen, die NCS Expert liest. |
| **SP-Daten** | Das Datenpaket der E-Reihe: SGBDs plus Codierdaten. |
| **PSdZData** | Das Gegenstück der F/G-Reihe. Anderes Format, hier noch nicht unterstützt. |
| **DTC** | *Diagnostic Trouble Code*: ein Eintrag im Fehlerspeicher. |
| **FS** | *Fehlerspeicher*. Daher `FS_LESEN` und `FS_LOESCHEN`. |
| **UDS** | *Unified Diagnostic Services*, das moderne ISO-Protokoll neuerer Steuergeräte. |
| **KWP2000 / DS2** | Ältere BMW-Diagnoseprotokolle. DS2 ist das älteste in der E-Reihe gebräuchliche. |
| **D-CAN** | Diagnose über CAN, etwa ab 2007. |
| **K-Line** | Der ältere Eindraht-Diagnosebus. Zeitkritisch. |
| **ENET** | Ethernet-Diagnose (DoIP) für F- und G-Modelle. |
| **I-Stufe** | *Integrationsstufe*: der Softwarestand eines Fahrzeugs oder Steuergeräts. |
| **Codierindex** | Kennzeichnet, welche Version der Codierdaten ein Steuergerät erwartet. |
