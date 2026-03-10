using System.Text;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace GC2Connector;

public sealed class Gc2BluetoothReader : IDisposable
{
    private BluetoothClient? _client;
    private CancellationTokenSource? _cts;

    public event Action<string>? LineReceived;

    /// <summary>
    /// Scans for paired Bluetooth devices and connects to one matching the GC2.
    /// The GC2 advertises with its serial number as the device name.
    /// </summary>
    public void Start(string? deviceNameFilter = null)
    {
        _client = new BluetoothClient();

        Console.WriteLine("[BT] Scanning for paired devices...");
        var paired = _client.PairedDevices;

        BluetoothDeviceInfo? gc2 = null;
        foreach (var device in paired)
        {
            Console.WriteLine($"  Found: {device.DeviceName} ({device.DeviceAddress})");
            if (deviceNameFilter != null)
            {
                if (device.DeviceName.Contains(deviceNameFilter, StringComparison.OrdinalIgnoreCase))
                {
                    gc2 = device;
                    break;
                }
            }
            else
            {
                // Accept any paired device if no filter — user can narrow with --bt-name
                gc2 ??= device;
            }
        }

        if (gc2 == null)
        {
            throw new InvalidOperationException(
                "No paired GC2 found. Pair the GC2 in your OS Bluetooth settings first." +
                (deviceNameFilter != null ? $" (filter: '{deviceNameFilter}')" : ""));
        }

        Console.WriteLine($"[BT] Connecting to {gc2.DeviceName} ({gc2.DeviceAddress})...");

        // SPP UUID — standard Serial Port Profile
        var sppUuid = BluetoothService.SerialPort;
        _client.Connect(gc2.DeviceAddress, sppUuid);

        Console.WriteLine($"[BT] Connected to {gc2.DeviceName}");

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ReadLoop(_client.GetStream(), _cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _client?.Close();
    }

    private void ReadLoop(System.IO.Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var line = reader.ReadLine();
                if (line == null) break; // stream closed
                if (!string.IsNullOrWhiteSpace(line))
                    LineReceived?.Invoke(line);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[BT] Read error: {ex.Message}");
                break;
            }
        }
    }

    public static void ListPairedDevices()
    {
        using var client = new BluetoothClient();
        var paired = client.PairedDevices;
        Console.WriteLine("Paired Bluetooth devices:");
        foreach (var d in paired)
            Console.WriteLine($"  {d.DeviceName} ({d.DeviceAddress})");
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _client?.Dispose();
    }
}
