---
id: workspace
title: Arbeitsumgebungen
keywords: arbeitsumgebung, umgebung, SP-Daten, EDIABAS, Ecu, DATEN, pfad, einrichtung
---

# Arbeitsumgebungen

Eine Arbeitsumgebung beschreibt, an welcher Fahrzeugart gearbeitet wird und wo
deren Daten liegen. Ohne sie ist keine Verbindung möglich.

## ECU-Datenordner

Verweist auf den **`Ecu`**-Ordner einer EDIABAS- oder SP-Daten-Installation — den
Ordner mit den `.prg`-Dateien (einzelne Steuergerätevarianten) und `d_*.grp`-Dateien
(Gruppendateien). In einer Standardinstallation ist das `C:\EDIABAS\Ecu`.

BimmerStudio bringt keine BMW-Daten mit und kopiert sie auch nicht. Die Dateien
werden dort gelesen, wo sie liegen; die SP-Daten werden also wie bisher gepflegt.

Sobald der Pfad gültig ist, meldet die Arbeitsumgebung, wie viele
Beschreibungsdateien gefunden wurden.

## Simulationsordner

Optional. Verweist auf EDIABAS-`.sim`-Dateien, die aufgezeichneten Verkehr
wiedergeben — so lässt sich die Anwendung ohne Fahrzeug und ohne Kabel benutzen.
Nützlich zum Kennenlernen und der einzige gefahrlose Weg, schreibende Jobs
auszuprobieren.

## Fahrzeugplattform

Heute die E-Reihe. Die Transportschicht unterstützt bereits ENET, über das F- und
G-Modelle erreicht werden, deren Codierdaten aber in einem anderen Format vorliegen
(PSdZData statt SP-Daten), das noch nicht umgesetzt ist.

## Warum keine Daten mitgeliefert werden

SP-Daten, die Steuergerätebeschreibungen und die NCS-Codiertabellen sind Eigentum
von BMW. Diese Anwendung ist ein unabhängiges Werkzeug zum Lesen bereits vorhandener
Dateien und verbreitet sie nicht weiter.
