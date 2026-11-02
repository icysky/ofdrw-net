using System.Globalization;

namespace Ofd.Core.PageDescription.Color.ColorSpace;

/// <summary>
/// 每个颜色通道使用的位数（有效取值 1/2/4/8/16）。
/// 注：Java 的 getInstance(String) 空值返回 BIT_1（与文档"默认 8"不一致），保持上游行为。
/// </summary>
public enum BitsPerComponent
{
    BIT_1 = 1,
    BIT_2 = 2,
    BIT_4 = 4,
    BIT_8 = 8,
    BIT_16 = 16,
}

public static class BitsPerComponentExtensions
{
    /// <summary>获取位数（整型），非法值抛异常。</summary>
    public static BitsPerComponent GetInstance(int bit) => bit switch
    {
        1 => BitsPerComponent.BIT_1,
        2 => BitsPerComponent.BIT_2,
        4 => BitsPerComponent.BIT_4,
        8 => BitsPerComponent.BIT_8,
        16 => BitsPerComponent.BIT_16,
        _ => throw new ArgumentException("每个颜色通道使用的位数，有效取值为：1，2，4，8，16"),
    };

    /// <summary>获取位数实例：空值返回 BIT_1（Java 上游行为），非数字抛异常。</summary>
    public static BitsPerComponent GetInstance(string? bitStr)
    {
        if (bitStr == null || bitStr.Trim().Length == 0) return BitsPerComponent.BIT_1;
        return GetInstance(int.Parse(bitStr.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture));
    }

    /// <summary>XML 取值（数字字符串）。</summary>
    public static string ToXmlValue(this BitsPerComponent bpc) => ((int)bpc).ToString(CultureInfo.InvariantCulture);
}
