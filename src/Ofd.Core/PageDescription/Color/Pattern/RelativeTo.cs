namespace Ofd.Core.PageDescription.Color.Pattern;

/// <summary>底纹单元起始绘制位置（8.3.4 表 28），默认 Object。</summary>
public enum RelativeTo
{
    /// <summary>相对于页面坐标的原点</summary>
    Page,

    /// <summary>相对于对象坐标系的原点</summary>
    Object,
}

public static class RelativeToExtensions
{
    /// <summary>获取起始绘制位置，空值默认 Object，未知值抛异常（与 Java 一致）。</summary>
    public static RelativeTo GetInstance(string? to)
    {
        to = (to ?? "").Trim();
        if (to.Length == 0 || to.Equals("Object", StringComparison.OrdinalIgnoreCase)) return RelativeTo.Object;
        if (to.Equals("Page", StringComparison.OrdinalIgnoreCase)) return RelativeTo.Page;
        throw new ArgumentException("未知的底纹单元起始绘制位置：" + to);
    }
}
