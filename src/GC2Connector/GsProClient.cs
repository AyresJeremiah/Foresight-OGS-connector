using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GC2Connector.Models;

namespace GC2Connector;

public sealed class GsProClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _shotNumber;

    public bool IsConnected => _client?.Connected == true;

    public GsProClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(_host, _port, ct);
        _stream = _client.GetStream();
        Console.WriteLine($"[GSPro] Connected to {_host}:{_port}");
    }

    public async Task SendShotAsync(Gc2ShotData shot)
    {
        if (_stream == null) return;
        var msg = OpenConnectMessage.CreateShot(++_shotNumber, shot);
        await SendJson(msg);
        Console.WriteLine($"[GSPro] Sent shot #{_shotNumber}");
    }

    public async Task SendHeartbeatAsync(bool ready)
    {
        if (_stream == null) return;
        await SendJson(OpenConnectMessage.CreateHeartbeat(ready));
    }

    private async Task SendJson<T>(T obj)
    {
        if (_stream == null) return;
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _stream.WriteAsync(bytes);
        await _stream.FlushAsync();
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _client?.Dispose();
    }
}
