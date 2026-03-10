using System.IO.Ports;

namespace GC2Connector;

public sealed class Gc2SerialReader : IDisposable
{
    private readonly string _portName;
    private readonly int _baudRate;
    private SerialPort? _port;
    private CancellationTokenSource? _cts;

    public event Action<string>? LineReceived;

    public Gc2SerialReader(string portName, int baudRate = 115200)
    {
        _portName = portName;
        _baudRate = baudRate;
    }

    public void Start()
    {
        _port = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 1000,
            NewLine = "\n"
        };
        _port.Open();

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ReadLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _port?.Close();
    }

    private void ReadLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var line = _port?.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                    LineReceived?.Invoke(line);
            }
            catch (TimeoutException) { }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Serial] Read error: {ex.Message}");
                break;
            }
        }
    }

    public static List<string> ListPorts() => [.. SerialPort.GetPortNames()];

    public static string? AutoDetect()
    {
        var ports = SerialPort.GetPortNames();
        // On Linux/Mac, GC2 often shows as /dev/ttyUSB* or /dev/ttyACM*
        // On Windows, it's a COM port. Try to find one that looks right.
        var candidate = ports.FirstOrDefault(p =>
            p.Contains("ttyUSB", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("ttyACM", StringComparison.OrdinalIgnoreCase));
        return candidate ?? (ports.Length == 1 ? ports[0] : null);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _port?.Dispose();
    }
}
