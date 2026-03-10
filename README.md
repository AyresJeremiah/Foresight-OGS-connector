# GC2 Foresight → Golf Simulator Connector

Standalone CLI app that reads shot data from a Foresight GC2 launch monitor (via USB serial or Bluetooth) and forwards it to golf simulators using the GSPro OpenConnect API or OpenGolfSim API.

## Requirements

- .NET 10 SDK
- Foresight GC2 launch monitor
- A running golf simulator (GSPro or OpenGolfSim)

## Quick Start

```bash
# Build
dotnet build src/GC2Connector/GC2Connector.csproj

# Connect via Bluetooth to GSPro (default)
dotnet run --project src/GC2Connector -- --bluetooth

# Connect via USB serial
dotnet run --project src/GC2Connector -- --port COM3
```

## Usage

```
gc2connector [options]

Connection (pick one):
  --port <name>         Serial port (COM3, /dev/ttyUSB0, etc.)
  --bluetooth, --bt     Connect via Bluetooth SPP
  --bt-name <filter>    Bluetooth device name filter (implies --bluetooth)

Simulator:
  --simulator <type>    Target simulator: gspro (default) or ogs
  --host <ip>           Simulator host (default: 127.0.0.1)
  --sim-port <port>     Simulator port (default: 921 for gspro, 3111 for ogs)

Serial options:
  --baud <rate>         Baud rate (default: 115200)

Discovery:
  --list-ports          List available serial ports
  --list-bt             List paired Bluetooth devices
```

## Examples

```bash
# Bluetooth to GSPro on localhost
gc2connector --bluetooth

# Bluetooth, filter by GC2 serial number
gc2connector --bt-name "2638" --simulator gspro

# USB serial to OpenGolfSim
gc2connector --port COM3 --simulator ogs

# GSPro on a different machine
gc2connector --bluetooth --host 192.168.1.100 --sim-port 921
```

## Setup

### Bluetooth

1. Power on the GC2
2. Pair it in your OS Bluetooth settings (the device name is typically the GC2 serial number)
3. Run `gc2connector --list-bt` to verify it appears
4. Start your simulator, then run `gc2connector --bluetooth`

### USB Serial

1. Connect the GC2 via USB
2. Run `gc2connector --list-ports` to find the port
3. Start your simulator, then run `gc2connector --port COM3` (or whatever port it's on)

## Supported Simulators

| Simulator | Protocol | Default Port |
|-----------|----------|-------------|
| GSPro | OpenConnect API (JSON over TCP) | 921 |
| OpenGolfSim | Newline-delimited JSON over TCP | 3111 |

## GC2 Protocol

The GC2 sends comma-separated ASCII lines over serial/Bluetooth:

```
CT=1259299,SN=2638,HW=3,SW=4.0.0,ID=2,TM=1259299,SP=8.39,AZ=-1.2,EL=12.5,TS=3200,SS=-450,BS=3100,CY=0,TL=0,SM=1.42,HMT=0
```

Key fields: `SP` (ball speed mph), `AZ` (horizontal launch angle), `EL` (vertical launch angle), `TS` (total spin rpm), `SS` (side spin), `BS` (back spin).

The connector handles two-phase shots (early reading without spin, final reading with spin) and filters misreads automatically.

## Building

```bash
dotnet build
dotnet test
```
