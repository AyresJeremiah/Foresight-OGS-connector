using System.Text.Json.Serialization;

namespace GC2Connector.Models;

public class OpenConnectMessage
{
    [JsonPropertyName("DeviceID")]
    public string DeviceID => "GC2Connector";

    [JsonPropertyName("Units")]
    public string Units => "Yards";

    [JsonPropertyName("ShotNumber")]
    public int ShotNumber { get; set; }

    [JsonPropertyName("APIversion")]
    public string APIVersion => "1";

    [JsonPropertyName("BallData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenConnectBallData? BallData { get; set; }

    [JsonPropertyName("ShotDataOptions")]
    public OpenConnectOptions Options { get; set; } = new();

    public static OpenConnectMessage CreateHeartbeat(bool ready = false) => new()
    {
        Options = new()
        {
            ContainsBallData = false,
            LaunchMonitorIsReady = ready,
            LaunchMonitorBallDetected = ready,
            IsHeartBeat = true
        }
    };

    public static OpenConnectMessage CreateShot(int shotNumber, Gc2ShotData shot) => new()
    {
        ShotNumber = shotNumber,
        BallData = new()
        {
            Speed = shot.SpeedMph,
            HLA = shot.AzimuthDeg,
            VLA = shot.ElevationDeg,
            TotalSpin = shot.TotalSpinRpm,
            BackSpin = shot.BackSpinRpm,
            SideSpin = shot.SideSpinRpm,
            SpinAxis = shot.SpinAxisDeg
        },
        Options = new() { ContainsBallData = true }
    };
}

public class OpenConnectBallData
{
    [JsonPropertyName("Speed")] public double Speed { get; set; }
    [JsonPropertyName("SpinAxis")] public double SpinAxis { get; set; }
    [JsonPropertyName("TotalSpin")] public double TotalSpin { get; set; }
    [JsonPropertyName("BackSpin")] public double BackSpin { get; set; }
    [JsonPropertyName("SideSpin")] public double SideSpin { get; set; }
    [JsonPropertyName("HLA")] public double HLA { get; set; }
    [JsonPropertyName("VLA")] public double VLA { get; set; }
}

public class OpenConnectOptions
{
    [JsonPropertyName("ContainsBallData")] public bool ContainsBallData { get; set; }
    [JsonPropertyName("ContainsClubData")] public bool ContainsClubData { get; set; }
    [JsonPropertyName("LaunchMonitorIsReady")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LaunchMonitorIsReady { get; set; }
    [JsonPropertyName("LaunchMonitorBallDetected")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LaunchMonitorBallDetected { get; set; }
    [JsonPropertyName("IsHeartBeat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsHeartBeat { get; set; }
}
