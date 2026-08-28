---
id: connection
title: Verbindungen
keywords: verbindung, kabel, K+DCAN, ENET, ELM327, simulation, COM-Port, seriell, adapter
---

# Verbindungen

Ein Verbindungsprofil beschreibt, wie ein Fahrzeug erreicht wird. Mehrere Profile
sind möglich und lassen sich umschalten.

## K+DCAN-Kabel

Die klassische Schnittstelle der E-Reihe: ein FTDI-USB-Kabel, das sich als
serieller Port meldet.

- **Windows** — ein `COM`-Port, etwa `COM4`.
- **Linux** — `/dev/ttyUSB0`. Der Benutzer muss in der Gruppe `dialout` sein.
- **macOS** — `/dev/tty.usbserial-XXXX`.

BimmerStudio nutzt den virtuellen seriellen Port statt FTDIs proprietärem Treiber,
weil es diesen Weg auf allen drei Plattformen gibt. Eine Folge ist erwähnenswert:
die 5-Baud-Langsaminitialisierung, die die ältesten K-Line-Steuergeräte benötigen,
steht außerhalb von Windows nicht zur Verfügung. D-CAN-Fahrzeuge (etwa ab 2007)
sind nicht betroffen.

Unter Linux profitiert das K-Line-Timing von einem niedrigeren FTDI-Latency-Timer:

```
echo 1 | sudo tee /sys/bus/usb-serial/devices/ttyUSB0/latency_timer
```

## ENET

Ethernet (DoIP), über das F- und G-Modelle erreicht werden. Der Host bleibt auf
`auto`, um das Gateway per Rundruf zu finden, oder es wird eine Adresse angegeben.

Bereits vorhanden, damit der Weg zu neueren Fahrzeugen real und nicht theoretisch
ist: die Verbindung funktioniert; was für volle F/G-Unterstützung fehlt, ist die
Codierdatenschicht.

## ELM327

Günstige Bluetooth- und WLAN-Adapter. Unterstützt, mit einer Einschränkung, die an
der Hardware liegt und nicht an dieser Anwendung: Serienmäßige ELM327-Firmware
beherrscht die K-Line-Protokolle älterer E-Reihen-Steuergeräte nicht und ist selbst
bei D-CAN grenzwertig. Wo Timing zählt, ist ein K+DCAN-Kabel vorzuziehen.

Bluetooth-Adapter werden als gewöhnliche serielle Ports angesprochen
(`/dev/rfcomm0` oder der ausgehende COM-Port, den Windows beim Koppeln anlegt).

## Simulation

Gibt aufgezeichneten Verkehr aus EDIABAS-`.sim`-Dateien wieder. Keine Hardware,
kein Fahrzeug, nichts, das Schaden nehmen könnte. Eine Simulation beantwortet nur
Anfragen, die in ihrer Datei stehen; sie belegt also, dass die Werkzeugkette
funktioniert, nicht dass ein bestimmtes Steuergerät sich so verhält.

## Eine Verbindung zur Zeit

Pro laufender Instanz ist nur eine Verbindung möglich. Das ist eine Einschränkung
des EDIABAS-Interpreters, der prozessweiten Zustand hält; zwei gleichzeitig
benutzte Verbindungen beschädigen sich gegenseitig auf schwer nachvollziehbare Weise.
