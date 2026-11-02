namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>渐变绘制的方式（8.3.4.2 表 29），默认 Direct。</summary>
public enum MapType
{
    Direct,
    Repeat,
    Reflect,
}

public static class MapTypeExtensions
{
    /// <summary>获取渐变绘制方式，空值默认 Direct，未知值抛异常（与 Java 一致）。</summary>
    public static MapType GetInstance(string? type)
    {
        type = (type ?? "").Trim();
        if (type.Length == 0 || type.Equals("Direct", StringComparison.OrdinalIgnoreCase)) return MapType.Direct;
        if (type.Equals("Repeat", StringComparison.OrdinalIgnoreCase)) return MapType.Repeat;
        if (type.Equals("Reflect", StringComparison.OrdinalIgnoreCase)) return MapType.Reflect;
        throw new ArgumentException("位置渐变绘制的方式：" + type);
    }
}
