namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>
/// 轴线延长线方向是否继续绘制（8.3.4.2 表 29）：
/// 0 不延伸（默认）/ 1 结束点至起始点方向 / 2 起始点至结束点方向 / 3 两侧。
/// </summary>
public enum Extend
{
    _0 = 0,
    _1 = 1,
    _2 = 2,
    _3 = 3,
}

public static class ExtendExtensions
{
    /// <summary>获取延伸类型，空值默认 _0，未知值抛异常（与 Java 一致）。</summary>
    public static Extend GetInstance(string? value)
    {
        value = (value ?? "").Trim();
        return value switch
        {
            "" or "0" => Extend._0,
            "1" => Extend._1,
            "2" => Extend._2,
            "3" => Extend._3,
            _ => throw new ArgumentException("未知的轴线延长线方向是否继续绘制类型：" + value),
        };
    }

    /// <summary>XML 取值（数字）。</summary>
    public static string ToXmlValue(this Extend extend) => ((int)extend).ToString();
}
