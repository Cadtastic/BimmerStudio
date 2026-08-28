---
id: sgbd-browser
title: Steuergeräte-Browser
keywords: SGBD, job, Tool32, prg, grp, Gruppendatei, Variante, ausführen, Ergebnisse
---

# Steuergeräte-Browser

Das Gegenstück zu Tool32: eine Steuergerätebeschreibung laden, die angebotenen
Jobs ansehen, einen ausführen, die Ergebnisse lesen.

## Varianten und Gruppendateien

Die Auswahl eines Steuergeräts lädt es sofort. In der Liste erscheinen zwei Arten
von Dateien, jeweils gekennzeichnet und nach Fahrzeugbereich gruppiert.

**Varianten** (`.prg`, gekennzeichnet als *Variante*) beschreiben ein bestimmtes
Steuergerät — `CAS`, `MSV70`, `MSD80`. Hier liegen die Jobs. Eine typische
E-Reihen-Installation enthält einige hundert davon, und die meisten lassen sich
ohne angeschlossenes Fahrzeug öffnen.

**Gruppendateien** (`.grp`, benannt `d_*`, gekennzeichnet als *Gruppe*) beschreiben
eine *Familie* — `d_motor` für den Motor, `d_kombi` für das Kombiinstrument. Eine
Gruppendatei ist ein Verteiler, kein geringeres Steuergerät: sie enthält die Logik,
das Fahrzeug nach der verbauten Variante zu fragen, und danach verhält sich die
Sitzung genau so, als hätte man diese Variante direkt geladen.

Damit sind Gruppen bei angeschlossenem Fahrzeug meist der richtige Einstieg — man
weiß selten im Voraus, ob ein 3er einen MSV70 oder einen MSD80 hat; `d_motor`
findet es heraus. Deshalb werden sie auch nicht ausgeblendet.

Der Haken: offline lässt sich eine Gruppe nicht untersuchen, denn das Erkennen des
Steuergeräts bedeutet, mit ihm zu sprechen. **Über eine Simulationsverbindung sind
Gruppendateien daher ausgegraut** und mit „Erfordert aktive Verbindung“ markiert,
statt angeboten zu werden und dann zu scheitern. Über K+DCAN-Kabel, ENET oder
einen ELM327-Adapter stehen sie zur Verfügung.

Auch manche Varianten brauchen eine Verbindung. Besonders Motor- und
Getriebedateien (MSV70, MSD80, GS19, die DDE-Familie) führen beim Laden einen
Initialisierungsjob aus, der mit dem Steuergerät spricht. In beiden Fällen meldet
die Anwendung, dass ein Fahrzeug benötigt wird, statt einen Fehler zu zeigen — das
ist normal und keine Störung.

## Die Jobliste

Wird aus der geladenen Datei ausgelesen. Jedes Steuergerät bietet einen anderen
Satz; diese Liste definiert also nicht BimmerStudio.

Die Auswahl eines Jobs zeigt, was die Beschreibungsdatei über ihn dokumentiert:
Zweck, Argumente und die zurückgelieferten Ergebnisse. Viele Dateien enthalten gar
keine Dokumentation — die Blöcke sind optional und fehlen oft — eine leere
Beschreibung ist daher üblich und kein Anzeichen für ein Problem.

**F1** bei ausgewähltem Job zeigt Hilfe, die aus der Dokumentation genau dieses
Steuergeräts und der Sicherheitseinstufung zusammengesetzt wird.

## Argumente

Genau so einzugeben, wie EDIABAS sie erwartet: **positionsabhängige Werte, durch
Semikolon getrennt**, ohne Anführungszeichen, ohne Namen. Was an welche Position
gehört, legt der Job fest.

Die Tafel unter der Eingabezeile zeigt, was der ausgewählte Job deklariert: Name,
Typ und die hinterlegte Beschreibung jedes Arguments. Etwa die Hälfte aller Jobs
deklariert Argumente; der Rest erwartet tatsächlich keine, und die Tafel sagt das.
**Vorlage einfügen** füllt die Zeile mit einem Platzhalter je Argument — Nullen für
Zahlentypen, `?` wo ein Wert einzutragen ist — damit die Anzahl der Stellen vor dem
Bearbeiten stimmt.

Die deklarierten Ergebnisse stehen vor der ersten Ausführung über den Schaltflächen,
sodass vorher klar ist, was ein Job zurückgibt.

## Ausführen

- **Einmal ausführen** — führt den Job einmal aus.
- **Zyklisch ausführen** — wiederholt ihn in einem Intervall, um Werte zu beobachten.
  Währenddessen ist die Verbindung belegt; andere Arbeiten warten bis zum Stopp.

Am realen Fahrzeug laufen nur lesende Jobs. Siehe [Sicherheit](safety).

## Ergebnisse

EDIABAS liefert einen **Systemsatz** über den Aufruf selbst — `JOBSTATUS`,
`VARIANTE`, teils `UBATT` — gefolgt von null oder mehr **Datensätzen** mit der
Nutzlast, einer je Datensatz. Ein Fehlerspeicherauslesen liefert einen Datensatz je
gespeichertem Fehler.

`JOBSTATUS` ist EDIABAS' Weg, ein Scheitern auf Jobebene zu melden. Ein Job, der
läuft und einen abweichenden Status meldet, ist nicht fehlgeschlagen — er hat etwas
mitgeteilt, und der Statustext ist meist der aufschlussreichste Teil des Ergebnisses.

Ergebnisse werden je Job behalten: ein anderer Job zeigt seine eigenen, und die
Rückkehr zeigt wieder die vorherigen. **Leeren** verwirft sie.
