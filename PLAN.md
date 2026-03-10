# GC2 Foresight → OpenGolfSim Connector

## Overview

Standalone .NET CLI app that runs on a PC (Windows/Mac/Linux). Reads shot data from a Foresight GC2 launch monitor over USB serial and forwards it to golf simulators via the GSPro OpenConnect API (port 921) and/or OpenGolfSim API (port 3111).

The GC2 presents as a serial port over USB. On Windows it creates a COM port automatically; on Mac/Linux it appears as a `/dev/tty*` device. No special drivers needed beyond OS defaults.

## Why a separate project

The GC2 is USB-only — no Bluetooth. iOS can't do USB host, and even on Android USB OTG is finicky. The practical setup is: GC2 is plugged into the same PC running the golf simulator. This app bridges the two.

## Reference implementation

[matthew-johnston/gspro-gc2-connector](https://github.com/matthew-johnston/gspro-gc2-connector) — Rust CLI that does exactly this. Serial port at 115200 baud, reads comma-delimited `KEY=VALUE` lines prefixed with `CT`, forwards to GSPro. We follow the same approach in .NET.

## GC2 Serial Protocol

The GC2 sends ASCII lines over serial at **115200 baud, 8N1**. Each line is `\n`-terminated and contains comma-separated `KEY=VALUE` fields:

```
CT=1259299,SN=2638,HW=3,SW=4.0.0,ID=2,TM=1259299,SP=8.39,AZ=-1.2,EL=12.5,TS=3200,SS=-450,BS=3100,CY=0,TL=0,SM=1.42,HMT=0
```

### Fields

| Key | Type   | Description |
|-----|--------|-------------|
| CT  | uint   | Counter/timestamp |
| SN  | uint   | Device serial number |
| HW  | uint   | Hardware version |
| SW  | string | Software version |
| ID  | uint   | Message ID |
| TM  | uint   | Timestamp |
| SP  | float  | Ball speed (mph) |
| AZ  | float  | Azimuth / horizontal launch angle (deg) |
| EL  | float  | Elevation / vertical launch angle (deg) |
| TS  | float  | Total spin (rpm) |
| SS  | float  | Side spin (rpm) |
| BS  | float  | Back spin (rpm) |
| CY  | float  | Carry (yards?) |
| TL  | float  | Total (yards?) |
| SM  | float  | Smash factor |
| HMT | float  | HMT club data flag |

Only lines starting with `CT` contain shot data. Other output is ignored.

### Misread detection

- `BS == 0 && SS == 0` → misread (no spin data)
- `BS == 2222` → sentinel for misread

### Two-phase shots

The GC2 may send an early reading (no spin) followed by a final reading (with spin). Same `SP` value, second message has spin populated. Use a 1500ms window: if a second message with the same speed arrives with spin data, use it; otherwise fall back to the first.

## Architecture

```
Serial Port (115200 8N1)
    │
    ▼
Gc2SerialReader ──reads lines──▶ Gc2LineParser ──parses CT lines──▶ Gc2ShotData
                                                                       │
                                                          ┌────────────┴────────────┐
                                                          ▼                         ▼
                                                   GsProClient              OgsClient
                                                  (TCP port 921)        (TCP port 3111)
                                                   OpenConnect API      Newline-delimited JSON
```

## Project Structure

```
GC2ForesightToOGSConnector/
├── GC2ForesightToOGSConnector.sln
├── src/
│   └── GC2Connector/
│       ├── GC2Connector.csproj          # Console app, net10.0
│       ├── Program.cs                   # CLI entry point (System.CommandLine)
│       ├── Gc2SerialReader.cs           # Opens serial port, reads lines, raises events
│       ├── Gc2LineParser.cs             # Parses "CT=...,SP=...,AZ=..." into Gc2ShotData
│       ├── Gc2ShotData.cs              # Shot data model (speed, angles, spin, misread detection)
│       ├── ShotAggregator.cs           # Two-phase shot handling (early/final, 1500ms timeout)
│       ├── GsProClient.cs             # GSPro OpenConnect API client (TCP, JSON)
│       ├── OgsClient.cs               # OpenGolfSim API client (TCP, newline-delimited JSON)
│       └── Models/
│           ├── OpenConnectMessage.cs    # GSPro API message envelope
│           └── OgsMessage.cs           # OGS API message envelope
└── tests/
    └── GC2Connector.Tests/
        ├── GC2Connector.Tests.csproj
        ├── Gc2LineParserTests.cs
        ├── ShotAggregatorTests.cs
        └── Gc2ShotDataTests.cs
```

## Dependencies

| Package | Purpose |
|---------|---------|
| System.IO.Ports | Serial port access (built into .NET) |
| System.CommandLine | CLI argument parsing |

No other external dependencies. The GSPro/OGS clients are raw `TcpClient` — no need for a library.

## CLI Interface

```bash
# Auto-detect GC2 serial port, connect to GSPro on localhost:921
gc2connector

# Specify serial port and simulator
gc2connector --port COM3 --baud 115200 --simulator gspro --host 127.0.0.1

# Connect to OpenGolfSim instead
gc2connector --port /dev/ttyUSB0 --simulator ogs --host 127.0.0.1 --sim-port 3111

# List available serial ports
gc2connector --list-ports
```

### Arguments

| Flag | Default | Description |
|------|---------|-------------|
| `--port` | auto-detect | Serial port (COM3, /dev/ttyUSB0, etc.) |
| `--baud` | 115200 | Baud rate |
| `--simulator` | gspro | Target simulator: `gspro` or `ogs` |
| `--host` | 127.0.0.1 | Simulator host |
| `--sim-port` | 921 (gspro) / 3111 (ogs) | Simulator port |
| `--list-ports` | — | List available serial ports and exit |

### Auto-detect

Enumerate serial ports and look for one with a device description or VID/PID matching the GC2 (VID `0x2C79`, PID `0x0110`). If exactly one match, use it. If none or multiple, prompt user or error with `--list-ports` hint.

## Key Design Decisions

1. **Serial port, not USB HID** — The reference implementation and forum evidence show the GC2 presents as a serial device on desktop OSes. COM port on Windows, `/dev/tty*` on Mac/Linux. `System.IO.Ports` handles this natively in .NET.

2. **No reuse of ProPutt's Gc2Connect library** — That library was designed for Android USB HID (64-byte packets, `\n\t` terminators). The serial protocol is different: comma-delimited `KEY=VALUE` on `\n`-terminated lines. A fresh parser is simpler and more correct than adapting the HID accumulator.

3. **Two simulator targets** — GSPro (OpenConnect API, port 921, JSON) and OpenGolfSim (port 3111, newline-delimited JSON). Both are simple TCP protocols. Support both from day one since the code is nearly identical.

4. **Heartbeat** — Send periodic heartbeat to the simulator (every 5-10s) to keep the connection alive and report launch monitor readiness + ball detection status.

5. **Console output** — Print shot data to stdout as it arrives, so the user can see what's happening. No GUI.

## Implementation Order

1. `Gc2ShotData` model + `Gc2LineParser` (parse comma-separated lines)
2. `Gc2LineParserTests` — verify parsing with sample GC2 output
3. `ShotAggregator` (two-phase handling) + tests
4. `Gc2SerialReader` (serial port wrapper, line-based reading)
5. `GsProClient` + `OgsClient` (TCP, JSON, heartbeat)
6. `Program.cs` (CLI wiring with System.CommandLine)
7. `OpenConnectMessage` + `OgsMessage` models
8. End-to-end testing with a real GC2

## What about ProPutt's Android GC2 code?

The Android implementation in ProPutt (`Gc2Connect` library, `AndroidGc2UsbTransport`, `Gc2Service`) was built for USB HID on Android. It should be **removed or gated** since:
- The GC2 is a serial device on desktop, not USB HID
- iOS can't use it at all
- Android USB OTG serial would need a different transport anyway
- This standalone connector is the intended way to use a GC2

Recommendation: remove the GC2 code from ProPutt in a follow-up, or keep it behind a flag if Android USB serial testing is planned later.
