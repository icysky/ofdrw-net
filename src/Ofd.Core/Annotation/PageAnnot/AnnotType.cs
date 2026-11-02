namespace Ofd.Core.Annotation.PageAnnot;

/// <summary>注释类型取值（15.2 表 62）。</summary>
public enum AnnotType
{
    /// <summary>连接注释</summary>
    Link,

    /// <summary>路径注释（矩形、多边形、贝塞尔曲线等图形对象）</summary>
    Path,

    /// <summary>高亮注释</summary>
    Highlight,

    /// <summary>签章注释</summary>
    Stamp,

    /// <summary>水印注释</summary>
    Watermark,
}

public static class AnnotTypeExtensions
{
    /// <summary>获取注释类型，未知值抛异常（与 Java 一致）。</summary>
    public static AnnotType GetInstance(string? str)
    {
        str = (str ?? "").Trim();
        if (str.Equals("Link", StringComparison.OrdinalIgnoreCase)) return AnnotType.Link;
        if (str.Equals("Path", StringComparison.OrdinalIgnoreCase)) return AnnotType.Path;
        if (str.Equals("Highlight", StringComparison.OrdinalIgnoreCase)) return AnnotType.Highlight;
        if (str.Equals("Stamp", StringComparison.OrdinalIgnoreCase)) return AnnotType.Stamp;
        if (str.Equals("Watermark", StringComparison.OrdinalIgnoreCase)) return AnnotType.Watermark;
        throw new ArgumentException("未知的注释类型取值：" + str);
    }
}
