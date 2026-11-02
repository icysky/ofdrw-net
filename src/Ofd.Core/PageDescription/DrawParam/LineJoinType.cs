namespace Ofd.Core.PageDescription.DrawParam;

/// <summary>线条连接样式（表 22），默认 Miter。</summary>
public enum LineJoinType
{
    Miter,
    Round,
    Bevel,
}

public static class LineJoinTypeExtensions
{
    /// <summary>获取线条连接样式，空值默认 Miter，未知类型抛异常（与 Java 一致）。</summary>
    public static LineJoinType GetInstance(string? type)
    {
        type = (type ?? "").Trim();
        if (type.Length == 0 || type.Equals("Miter", StringComparison.OrdinalIgnoreCase)) return LineJoinType.Miter;
        if (type.Equals("Round", StringComparison.OrdinalIgnoreCase)) return LineJoinType.Round;
        if (type.Equals("Bevel", StringComparison.OrdinalIgnoreCase)) return LineJoinType.Bevel;
        throw new ArgumentException("未知线条连接样式：" + type);
    }
}
