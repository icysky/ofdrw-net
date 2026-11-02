namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>三角单元切换的方向标志（8.1.4.4 表 31 附录 A.13）。</summary>
public enum EdgeFlag
{
    _0 = 0,
    _1 = 1,
    _2 = 2,
}

public static class EdgeFlagExtensions
{
    /// <summary>获取方向标志，空值默认 _0，未知值抛异常（与 Java 一致）。</summary>
    public static EdgeFlag GetInstance(string? flagStr)
    {
        flagStr = (flagStr ?? "").Trim();
        return flagStr switch
        {
            "" or "0" => EdgeFlag._0,
            "1" => EdgeFlag._1,
            "2" => EdgeFlag._2,
            _ => throw new ArgumentException("未知的三角单元切换的方向标志：" + flagStr),
        };
    }

    /// <summary>XML 取值（数字）。</summary>
    public static string ToXmlValue(this EdgeFlag flag) => ((int)flag).ToString();
}
