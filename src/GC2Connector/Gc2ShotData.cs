namespace GC2Connector;

public sealed class Gc2ShotData
{
    public uint Counter { get; init; }
    public uint SerialNumber { get; init; }
    public double SpeedMph { get; init; }
    public double AzimuthDeg { get; init; }
    public double ElevationDeg { get; init; }
    public double TotalSpinRpm { get; init; }
    public double SideSpinRpm { get; init; }
    public double BackSpinRpm { get; init; }
    public double SmashFactor { get; init; }

    public bool IsMisread =>
        (BackSpinRpm == 0 && SideSpinRpm == 0) ||
        Math.Abs(BackSpinRpm - 2222.0) < 0.1;

    public bool HasSpin => BackSpinRpm != 0 || SideSpinRpm != 0;

    public double SpinAxisDeg
    {
        get
        {
            if (BackSpinRpm == 0 && SideSpinRpm == 0) return 0;
            if (BackSpinRpm == 0) return SideSpinRpm > 0 ? 90 : -90;
            return Math.Atan2(SideSpinRpm, BackSpinRpm) * (180.0 / Math.PI);
        }
    }
}
