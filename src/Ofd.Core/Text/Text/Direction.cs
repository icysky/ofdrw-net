using System.Globalization;

namespace Ofd.Core.Text.Text;

/// <summary>方向角度（11.3 表 47）：0/90/180/270，默认 0（水平）。</summary>
public enum Direction
{
    Angle_0,
    Angle_90,
    Angle_180,
    Angle_270,
}

public static class DirectionExtensions
{
    /// <summary>获取方向，空值默认 Angle_0，未知角度抛异常（与 Java 一致）。</summary>
    public static Direction GetInstance(string? angleStr)
    {
        if (angleStr == null || angleStr.Trim().Length == 0) angleStr = "0";
        return angleStr switch
        {
            "" or "0" => Direction.Angle_0,
            "90" => Direction.Angle_90,
            "180" => Direction.Angle_180,
            "270" => Direction.Angle_270,
            _ => throw new ArgumentException("未知旋转角度：" + angleStr),
        };
    }

    public static Direction GetInstance(int angle) => GetInstance(angle.ToString(CultureInfo.InvariantCulture));

    /// <summary>XML 取值（角度数字）。</summary>
    public static string ToXmlValue(this Direction direction) => direction switch
    {
        Direction.Angle_90 => "90",
        Direction.Angle_180 => "180",
        Direction.Angle_270 => "270",
        _ => "0",
    };
}
