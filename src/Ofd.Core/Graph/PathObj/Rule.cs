namespace Ofd.Core.Graph.PathObj;

/// <summary>图形的填充规则（9.1 表 35），默认 NonZero。</summary>
public enum Rule
{
    NonZero,
    Even_Odd,
}

public static class RuleExtensions
{
    /// <summary>获取填充规则，空值默认 NonZero，未知类型抛异常（与 Java 一致）。</summary>
    public static Rule GetInstance(string? type)
    {
        type = (type ?? "").Trim();
        if (type.Length == 0 || type.Equals("NonZero", StringComparison.OrdinalIgnoreCase)) return Rule.NonZero;
        if (type.Equals("Even-Odd", StringComparison.OrdinalIgnoreCase)) return Rule.Even_Odd;
        throw new ArgumentException("未知的图形的填充规则类型：" + type);
    }

    /// <summary>XML 取值（Even_Odd → "Even-Odd"）。</summary>
    public static string ToXmlValue(this Rule rule) => rule == Rule.Even_Odd ? "Even-Odd" : "NonZero";
}
