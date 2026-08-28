---
id: connection
title: Connections
keywords: connection, cable, K+DCAN, ENET, ELM327, simulation, COM port, serial, adapter
---

# Connections

A connection profile describes how to reach a car. You can keep several and switch
between them.

## K+DCAN cable

The classic E-series interface: an FTDI USB cable presenting a serial port.

- **Windows** — a `COM` port, for example `COM4`.
- **Linux** — `/dev/ttyUSB0`. Your user needs to be in the `dialout` group.
- **macOS** — `/dev/tty.usbserial-XXXX`.

BimmerStudio uses the virtual serial port rather than FTDI's proprietary driver,
because that is what exists on all three platforms. One consequence is worth
knowing: the 5-baud "slow init" that the oldest K-line ECUs need is not available
outside Windows. D-CAN cars (roughly 2007 onward) are unaffected.

On Linux, K-line timing benefits from lowering the FTDI latency timer:

```
echo 1 | sudo tee /sys/bus/usb-serial/devices/ttyUSB0/latency_timer
```

## ENET

Ethernet (DoIP), how F- and G-series cars are reached. Leave the host set to
`auto` to find the gateway by broadcast, or give an address directly.

Present now so the path to newer cars is real rather than theoretical — the link
works; what is missing for full F/G support is the coding-data layer.

## ELM327

Cheap Bluetooth and WiFi adapters. Supported, with a caveat that is about the
hardware rather than this app: stock ELM327 firmware cannot speak the K-line
protocols older E-series ECUs use, and is marginal even on D-CAN. Prefer a K+DCAN
cable where timing matters.

Bluetooth adapters are reached as ordinary serial ports (`/dev/rfcomm0`, or the
outgoing COM port Windows creates when you pair the device).

## Simulation

Replays recorded traffic from EDIABAS `.sim` files. No hardware, no car, nothing to
damage. A simulation only answers requests its file actually contains, so it proves
that the tooling works rather than that a given ECU behaves a certain way.

## One connection at a time

Only one connection can be open per running instance. This is a constraint of the
EDIABAS interpreter, which keeps state shared across the whole process; using two
at once corrupts both in ways that surface as unpredictable failures.
