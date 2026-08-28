---
id: safety
title: Sicherheit
keywords: sicherheit, nur lesen, schreiben, risiko, codierung, flash, gefahr
---

# Sicherheit

Aus einem Fahrzeug zu lesen ist ungefährlich. In eines zu schreiben nicht, und der
Unterschied verdient Ernst: ein falscher Codierwert hinterlässt ein fehlerhaft
konfiguriertes Steuergerät, und ein abgebrochener Flashvorgang kann es unbrauchbar
machen.

## Was die Anwendung derzeit zulässt

BimmerStudio ist **im Nur-Lese-Modus**. Jeder Job wird vor der Ausführung
eingestuft, und nur zwei Kategorien sind am realen Fahrzeug erlaubt:

| Kategorie | Bedeutung | Erlaubt |
|---|---|---|
| Lesen | Liest Daten. Verändert nichts. | Ja |
| Komm.-Init | Baut die Kommunikation auf. Verändert nichts. | Ja |
| Löscht Speicher | Löscht gespeicherte Daten, etwa den Fehlerspeicher. | Nein |
| Stellglied | Steuert einen physischen Ausgang an. | Nein |
| Codierung | Schreibt Codierdaten. | Nein |
| Flash | Programmiert Firmware neu. | Nein |
| Unbekannt | Nicht einstufbar. | Nein |

## Warum „Unbekannt“ gesperrt ist

Der Jobname ist das einzige Signal, das vor der Ausführung zur Verfügung steht —
und einen Job auszuführen, um herauszufinden, was er tut, ist genau das, was nicht
passieren darf. BMWs Benennung ist konsistent genug, um die meisten Jobs
einzustufen (`_LESEN` liest, `_LOESCHEN` löscht, `STEUERN_` steuert an), doch
ungefähr jeder sechste Name passt auf kein bekanntes Muster.

Diese werden als Schreibzugriff behandelt. Ein unbekannter Job ist höchstwahrscheinlich
harmlos, aber „höchstwahrscheinlich“ ist keine ausreichende Grundlage, um ein
Steuergerät anzufassen. Er wird deshalb gesperrt und als solcher gekennzeichnet.

Ein führender Unterstrich bedeutet übrigens **nicht**, dass ein Job sicher ist.
Reale Beschreibungsdateien enthalten Jobs wie `_COD_SCHREIBEN` und
`_FLASH_COMICRO`, die Codierdaten schreiben und Flash neu programmieren.

## Simulation

Gegen eine Simulation ist jeder Job erlaubt — es gibt kein Fahrzeug, das Schaden
nehmen könnte. Der Banner oben zeigt, welche Art von Verbindung aktiv ist, und
gesperrte Jobs begründen ihre Sperre.

## Wenn das Schreiben kommt

Codieren und Flashen sind geplant, hinter einer ausdrücklichen Bestätigung und
einer automatischen Sicherung des Ausgangszustands. Wann immer diese Funktionen
genutzt werden, mit welchem Werkzeug auch immer:

- Ladegerät anschließen. Codieren mit schwacher Batterie ist der klassische Weg,
  ein Steuergerät zu beschädigen.
- Den aktuellen Zustand vorher auslesen und sichern.
- Einen Flashvorgang nicht unterbrechen. Das ist der eine Vorgang ohne einfache
  Rückkehr.
