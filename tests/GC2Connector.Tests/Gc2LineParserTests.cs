using GC2Connector;
using Xunit;

namespace GC2Connector.Tests;

public class Gc2LineParserTests
{
    [Fact]
    public void ParsesValidShotLine()
    {
        var line = "CT=1259299,SN=2638,HW=3,SW=4.0.0,ID=2,TM=1259299,SP=8.39,AZ=-1.2,EL=12.5,TS=3200,SS=-450,BS=3100,CY=0,TL=0,SM=1.42,HMT=0";
        var shot = Gc2LineParser.Parse(line);

        Assert.NotNull(shot);
        Assert.Equal(1259299u, shot.Counter);
        Assert.Equal(2638u, shot.SerialNumber);
        Assert.Equal(8.39, shot.SpeedMph, 2);
        Assert.Equal(-1.2, shot.AzimuthDeg, 2);
        Assert.Equal(12.5, shot.ElevationDeg, 2);
        Assert.Equal(3200, shot.TotalSpinRpm, 1);
        Assert.Equal(-450, shot.SideSpinRpm, 1);
        Assert.Equal(3100, shot.BackSpinRpm, 1);
        Assert.Equal(1.42, shot.SmashFactor, 2);
    }

    [Fact]
    public void ReturnsNullForNonCtLine()
    {
        Assert.Null(Gc2LineParser.Parse("some random output"));
        Assert.Null(Gc2LineParser.Parse(""));
        Assert.Null(Gc2LineParser.Parse("HW=3,SW=4.0.0"));
    }

    [Fact]
    public void ReturnsNullWhenSpeedMissing()
    {
        var line = "CT=100,SN=1,AZ=0,EL=0";
        Assert.Null(Gc2LineParser.Parse(line));
    }

    [Fact]
    public void ReturnsNullWhenSpeedZero()
    {
        var line = "CT=100,SN=1,SP=0,AZ=0,EL=0";
        Assert.Null(Gc2LineParser.Parse(line));
    }

    [Fact]
    public void HandlesMinimalValidLine()
    {
        var shot = Gc2LineParser.Parse("CT=1,SP=5.0");
        Assert.NotNull(shot);
        Assert.Equal(5.0, shot.SpeedMph);
        Assert.Equal(0, shot.AzimuthDeg);
    }
}

public class Gc2ShotDataTests
{
    [Fact]
    public void DetectsMisreadNoSpin()
    {
        var shot = new Gc2ShotData { SpeedMph = 10, BackSpinRpm = 0, SideSpinRpm = 0 };
        Assert.True(shot.IsMisread);
        Assert.False(shot.HasSpin);
    }

    [Fact]
    public void DetectsMisreadSentinel()
    {
        var shot = new Gc2ShotData { SpeedMph = 10, BackSpinRpm = 2222, SideSpinRpm = 100 };
        Assert.True(shot.IsMisread);
    }

    [Fact]
    public void ValidShotNotMisread()
    {
        var shot = new Gc2ShotData { SpeedMph = 10, BackSpinRpm = 3000, SideSpinRpm = -200 };
        Assert.False(shot.IsMisread);
        Assert.True(shot.HasSpin);
    }

    [Fact]
    public void SpinAxisCalculation()
    {
        var shot = new Gc2ShotData { BackSpinRpm = 3000, SideSpinRpm = 0 };
        Assert.Equal(0, shot.SpinAxisDeg, 1);

        var shot2 = new Gc2ShotData { BackSpinRpm = 0, SideSpinRpm = 500 };
        Assert.Equal(90, shot2.SpinAxisDeg, 1);

        var shot3 = new Gc2ShotData { BackSpinRpm = 0, SideSpinRpm = -500 };
        Assert.Equal(-90, shot3.SpinAxisDeg, 1);
    }
}
