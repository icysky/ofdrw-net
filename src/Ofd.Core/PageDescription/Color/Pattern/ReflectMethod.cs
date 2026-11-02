namespace Ofd.Core.PageDescription.Color.Pattern;

/// <summary>翻转绘制效果（8.3.4 图 28），默认 Normal。</summary>
public enum ReflectMethod
{
    /// <summary>普通重复</summary>
    Normal,

    /// <summary>竖轴对称翻转</summary>
    Column,

    /// <summary>横轴对称翻转</summary>
    Row,

    /// <summary>十字轴对称翻转</summary>
    RowAndColumn,
}

public static class ReflectMethodExtensions
{
    /// <summary>获取翻转绘制效果，空值默认 Normal，未知值抛异常（与 Java 一致）。</summary>
    public static ReflectMethod GetInstance(string? method)
    {
        method = (method ?? "").Trim();
        if (method.Length == 0 || method.Equals("Normal", StringComparison.OrdinalIgnoreCase)) return ReflectMethod.Normal;
        if (method.Equals("Column", StringComparison.OrdinalIgnoreCase)) return ReflectMethod.Column;
        if (method.Equals("Row", StringComparison.OrdinalIgnoreCase)) return ReflectMethod.Row;
        if (method.Equals("RowAndColumn", StringComparison.OrdinalIgnoreCase)) return ReflectMethod.RowAndColumn;
        throw new ArgumentException("未知的翻转绘制效果：" + method);
    }
}
