using System.Globalization;

namespace GC2Connector;

public static class Gc2LineParser
{
    /// <summary>
    /// Parses a comma-separated GC2 line like "CT=1259299,SN=2638,SP=8.39,AZ=-1.2,..."
    /// Returns null if the line doesn't start with "CT" or is missing required fields.
    /// </summary>
    public static Gc2ShotData? Parse(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("CT", StringComparison.OrdinalIgnoreCase))
            return null;

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in trimmed.Split(','))
        {
            var eqIdx = pair.IndexOf('=');
            if (eqIdx <= 0) continue;
            fields[pair[..eqIdx].Trim()] = pair[(eqIdx + 1)..].Trim();
        }

        if (!TryGetDouble(fields, "SP", out var speed) || speed <= 0)
            return null;

        TryGetUint(fields, "CT", out var ct);
        TryGetUint(fields, "SN", out var sn);
        TryGetDouble(fields, "AZ", out var az);
        TryGetDouble(fields, "EL", out var el);
        TryGetDouble(fields, "TS", out var ts);
        TryGetDouble(fields, "SS", out var ss);
        TryGetDouble(fields, "BS", out var bs);
        TryGetDouble(fields, "SM", out var sm);

        return new Gc2ShotData
        {
            Counter = ct,
            SerialNumber = sn,
            SpeedMph = speed,
            AzimuthDeg = az,
            ElevationDeg = el,
            TotalSpinRpm = ts,
            SideSpinRpm = ss,
            BackSpinRpm = bs,
            SmashFactor = sm
        };
    }

    private static bool TryGetDouble(Dictionary<string, string> f, string key, out double val)
    {
        val = 0;
        return f.TryGetValue(key, out var s) &&
               double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
    }

    private static bool TryGetUint(Dictionary<string, string> f, string key, out uint val)
    {
        val = 0;
        return f.TryGetValue(key, out var s) &&
               uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out val);
    }
}
