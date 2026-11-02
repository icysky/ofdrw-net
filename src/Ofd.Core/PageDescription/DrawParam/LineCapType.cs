namespace Ofd.Core.PageDescription.DrawParam;

/// <summary>线端点样式（表 24），默认 Butt。</summary>
public enum LineCapType
{
    Butt,
    Round,
    Square,
}

public static class LineCapTypeExtensions
{
    /// <summary>获取线端点样式，空值默认 Butt，未知类型抛异常（与 Java 一致）。</summary>
    public static LineCapType GetInstance(string? type)
    {
        type = (type ?? "").Trim();
        if (type.Length == 0 || type.Equals("Butt", StringComparison.OrdinalIgnoreCase)) return LineCapType.Butt;
        if (type.Equals("Round", StringComparison.OrdinalIgnoreCase)) return LineCapType.Round;
        if (type.Equals("Square", StringComparison.OrdinalIgnoreCase)) return LineCapType.Square;
        throw new ArgumentException("未知的线端点样式：" + type);
    }
}
