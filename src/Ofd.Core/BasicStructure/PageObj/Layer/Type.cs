namespace Ofd.Core.BasicStructure.PageObj.Layer;

/// <summary>
/// 图层类型（7.7 页对象，图 16 图层渲染顺序）。
/// 前景层、正文层、背景层按出现顺序依次渲染，每层默认颜色透明。
/// </summary>
public enum Type
{
    /// <summary>前景层（次序 5）</summary>
    Foreground,

    /// <summary>正文层（次序 3）</summary>
    Body,

    /// <summary>背景层（次序 1）</summary>
    Background,
}

public static class TypeExtensions
{
    /// <summary>获取图层类型，空串默认 Body，未知类型抛异常（与 Java 一致）。</summary>
    public static Type GetInstance(string? type)
    {
        type = (type ?? "").Trim();
        if (type.Equals("Body", StringComparison.OrdinalIgnoreCase) || type.Length == 0) return Type.Body;
        if (type.Equals("Foreground", StringComparison.OrdinalIgnoreCase)) return Type.Foreground;
        if (type.Equals("Background", StringComparison.OrdinalIgnoreCase)) return Type.Background;
        throw new ArgumentException("未知的图层类型：" + type);
    }

    /// <summary>获取图层次序（Foreground=5, Body=3, Background=1）。</summary>
    public static int Order(this Type type) => type switch
    {
        Type.Foreground => 5,
        Type.Body => 3,
        _ => 1,
    };
}
