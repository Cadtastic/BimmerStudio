---
id: overview
title: BimmerStudio
keywords: hilfe, start, einführung, F1
---

# BimmerStudio

Eine einzige Anwendung als Ersatz für die alte BMW-Werkzeugkette — Tool32, INPA,
NCS Expert und WinKFP — lauffähig unter Windows, macOS und Linux.

**F1** öffnet überall die Hilfe zum gerade Angezeigten. **Umschalt+F1** öffnet
diese Hilfe mit Suchfunktion.

## Wie die Teile zusammenspielen

Drei Dinge müssen stehen, bevor eine Verbindung zum Fahrzeug möglich ist:

1. **Eine Arbeitsumgebung** — verweist auf die vorhandene EDIABAS- oder
   SP-Daten-Installation, damit die Anwendung die Steuergerätebeschreibungen findet.
2. **Eine Verbindung** — wie das Fahrzeug erreicht wird: K+DCAN-Kabel, ENET, ein
   ELM327-Adapter oder eine Simulation ganz ohne Hardware.
3. **Eine Steuergerätebeschreibung** — die SGBD des Steuergeräts, mit dem gearbeitet wird.

## Der entscheidende Punkt

**Jobs sind keine Funktionen dieser Anwendung.** Sie stecken in der jeweiligen
Steuergerätebeschreibung (SGBD), und jedes Steuergerät bietet einen anderen Satz.
`FS_LESEN` liest bei fast jedem Steuergerät den Fehlerspeicher, aber die
Motorsteuerung eines E90 kennt Jobs, von denen das Türmodul nie gehört hat.

Die angezeigte Jobliste wird also aus der geladenen Datei ausgelesen und stammt
nicht aus einer Liste, die BimmerStudio mitbringt. Deshalb sind Jobnamen auch
deutsche Protokollbezeichner von BMW und werden immer exakt so angezeigt, wie die
SGBD sie deklariert.

## Sprache

Die Anzeigesprache wird links ausgewählt und wirkt sofort. Sprachen sind Pakete:
Mitgeliefert sind Englisch und Deutsch, eine weitere Sprache ist eine einzelne
JSON-Datei im Ordner `languages` neben der Anwendung — ohne Neuinstallation.

Zwei Arten von Text verhalten sich unterschiedlich. Die Beschriftungen der
Anwendung werden vollständig übersetzt. Text aus den Fahrzeugdaten —
Jobbeschreibungen, Argumenthinweise — ist an der Quelle deutsch; im deutschen
Sprachpaket erscheint er unverändert, im englischen werden die häufigen Wendungen
übersetzt und das Original als Tooltip gezeigt. Jobnamen werden in keiner Sprache
übersetzt: sie sind das Protokoll und müssen zu dem passen, was jedes andere
Werkzeug und jedes Forum zeigt.

## Vorerst nur Lesen

Die Anwendung sperrt derzeit alles, was das Fahrzeug verändern könnte. Lesen ist
ungefährlich, Schreiben nicht — und die Schreibpfade entstehen mit der nötigen
Sorgfalt. Siehe [Sicherheit](safety).
