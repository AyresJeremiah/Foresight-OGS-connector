using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GC2Connector.Models;

namespace GC2Connector;

public sealed class OgsClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;

    public bool IsConnected => _client?.Connected == true;

    public OgsClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(_host, _port, ct);
        _stream = _client.GetStream();
        Console.WriteLine($"[OGS] Connected to {_host}:{_port}");
    }

    public async Task SendShotAsync(Gc2ShotData shot)
    {
        if (_stream == null) return;
        var msg = OgsMessage.CreateShot(shot);
        await SendJsonLine(msg);
        Console.WriteLine($"[OGS] Sent shot");
    }

    public async Task SendStatusAsync(string status)
    {
        if (_stream == null) return;
        await SendJsonLine(OgsMessage.CreateDeviceStatus(status));
    }

    private async Task SendJsonLine<T>(T obj)
    {
        if (_stream == null) return;
        var json = JsonSerializer.Serialize(obj) + "\n";
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
