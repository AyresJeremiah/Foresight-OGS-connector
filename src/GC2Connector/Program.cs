using GC2Connector;

// Parse args
string? portName = null;
int baudRate = 115200;
string simulator = "gspro";
string host = "127.0.0.1";
int? simPort = null;
bool listPorts = false;
bool useBluetooth = false;
bool listBt = false;
string? btName = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--port" when i + 1 < args.Length: portName = args[++i]; break;
        case "--baud" when i + 1 < args.Length: baudRate = int.Parse(args[++i]); break;
        case "--simulator" when i + 1 < args.Length: simulator = args[++i].ToLower(); break;
        case "--host" when i + 1 < args.Length: host = args[++i]; break;
        case "--sim-port" when i + 1 < args.Length: simPort = int.Parse(args[++i]); break;
        case "--list-ports": listPorts = true; break;
        case "--bluetooth" or "--bt": useBluetooth = true; break;
        case "--list-bt": listBt = true; break;
        case "--bt-name" when i + 1 < args.Length: btName = args[++i]; useBluetooth = true; break;
    }
}

if (listPorts)
{
    Console.WriteLine("Available serial ports:");
    foreach (var p in Gc2SerialReader.ListPorts())
        Console.WriteLine($"  {p}");
    return;
}

if (listBt)
{
    Gc2BluetoothReader.ListPairedDevices();
    return;
}

simPort ??= simulator == "ogs" ? 3111 : 921;

// If not bluetooth, need a serial port
if (!useBluetooth)
{
    portName ??= Gc2SerialReader.AutoDetect();
    if (portName == null)
    {
        Console.Error.WriteLine("No serial port specified and auto-detect failed.");
        Console.Error.WriteLine("Available ports:");
        foreach (var p in Gc2SerialReader.ListPorts())
            Console.Error.WriteLine($"  {p}");
        Console.Error.WriteLine("\nUsage: gc2connector --port COM3");
        Console.Error.WriteLine("       gc2connector --bluetooth");
        return;
    }
}

var transport = useBluetooth ? "Bluetooth" : $"{portName} @ {baudRate} baud";
Console.WriteLine($"GC2 Connector — {transport} → {simulator} ({host}:{simPort})");
Console.WriteLine();

// Connect to simulator
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

GsProClient? gsProClient = null;
OgsClient? ogsClient = null;

try
{
    if (simulator == "ogs")
    {
        ogsClient = new OgsClient(host, simPort.Value);
        await ogsClient.ConnectAsync(cts.Token);
    }
    else
    {
        gsProClient = new GsProClient(host, simPort.Value);
        await gsProClient.ConnectAsync(cts.Token);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to connect to {simulator}: {ex.Message}");
    Console.Error.WriteLine("Make sure the simulator is running and accepting connections.");
    return;
}

// Set up aggregator
var aggregator = new ShotAggregator();
aggregator.ShotReady += async shot =>
{
    Console.WriteLine($"  Shot: {shot.SpeedMph:F1} mph | HLA: {shot.AzimuthDeg:F1}° | VLA: {shot.ElevationDeg:F1}° | Spin: {shot.TotalSpinRpm:F0} rpm (axis: {shot.SpinAxisDeg:F1}°)");
    try
    {
        if (gsProClient != null) await gsProClient.SendShotAsync(shot);
        if (ogsClient != null) await ogsClient.SendShotAsync(shot);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  Failed to send shot: {ex.Message}");
    }
};

Action<string> onLine = line =>
{
    var shot = Gc2LineParser.Parse(line);
    if (shot != null)
        aggregator.Feed(shot);
};

// Start the appropriate reader
Gc2SerialReader? serialReader = null;
Gc2BluetoothReader? btReader = null;

try
{
    if (useBluetooth)
    {
        btReader = new Gc2BluetoothReader();
        btReader.LineReceived += onLine;
        btReader.Start(btName);
        Console.WriteLine("Reading from GC2 via Bluetooth... (Ctrl+C to quit)");
    }
    else
    {
        serialReader = new Gc2SerialReader(portName!, baudRate);
        serialReader.LineReceived += onLine;
        serialReader.Start();
        Console.WriteLine($"Reading from GC2 on {portName}... (Ctrl+C to quit)");
    }
    Console.WriteLine();

    // Heartbeat loop
    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            if (gsProClient != null) await gsProClient.SendHeartbeatAsync(true);
            if (ogsClient != null) await ogsClient.SendStatusAsync("ready");
        }
        catch { /* simulator may have disconnected */ }

        await Task.Delay(10_000, cts.Token);
    }
}
catch (OperationCanceledException) { }
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}
finally
{
    serialReader?.Stop();
    serialReader?.Dispose();
    btReader?.Stop();
    btReader?.Dispose();
    gsProClient?.Dispose();
    ogsClient?.Dispose();
    Console.WriteLine("\nDisconnected.");
}
