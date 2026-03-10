using System.Text.Json.Serialization;

namespace GC2Connector.Models;

public class OgsMessage
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }
    [JsonPropertyName("unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Unit { get; set; }
    [JsonPropertyName("shot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OgsShotData? Shot { get; set; }

    public static OgsMessage CreateDeviceStatus(string status) => new()
    {
        Type = "device",
        Status = status
    };

    public static OgsMessage CreateShot(Gc2ShotData shot) => new()
    {
        Type = "shot",
        Unit = "imperial",
        Shot = new()
        {
            BallSpeed = shot.SpeedMph,
            HorizontalLaunchAngle = shot.AzimuthDeg,
            VerticalLaunchAngle = shot.ElevationDeg,
            SpinSpeed = shot.TotalSpinRpm,
            SpinAxis = shot.SpinAxisDeg
        }
    };
}

public class OgsShotData
{
    [JsonPropertyName("ballSpeed")] public double BallSpeed { get; set; }
    [JsonPropertyName("verticalLaunchAngle")] public double VerticalLaunchAngle { get; set; }
    [JsonPropertyName("horizontalLaunchAngle")] public double HorizontalLaunchAngle { get; set; }
    [JsonPropertyName("spinSpeed")] public double SpinSpeed { get; set; }
    [JsonPropertyName("spinAxis")] public double SpinAxis { get; set; }
}
