namespace Ofd.Core.PageDescription.Color.ColorSpace;

/// <summary>颜色空间的类型（8.3.1 表 25）。</summary>
public enum OFDColorSpaceType
{
    /// <summary>灰度</summary>
    GRAY,

    /// <summary>红绿蓝</summary>
    RGB,

    /// <summary>印刷颜色</summary>
    CMYK,
}

public static class OFDColorSpaceTypeExtensions
{
    /// <summary>获取颜色空间类型，未知类型抛异常（与 Java 一致）。</summary>
    public static OFDColorSpaceType GetInstance(string? type)
    {
        type = (type ?? "").Trim();
        if (type.Equals("GRAY", StringComparison.OrdinalIgnoreCase)) return OFDColorSpaceType.GRAY;
        if (type.Equals("RGB", StringComparison.OrdinalIgnoreCase)) return OFDColorSpaceType.RGB;
        if (type.Equals("CMYK", StringComparison.OrdinalIgnoreCase)) return OFDColorSpaceType.CMYK;
        throw new ArgumentException("未知的颜色空间类型：" + type);
    }
}
