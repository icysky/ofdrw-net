namespace Ofd.Core.Text.Font;

/// <summary>字形适用的字符分类（11.1 表 44，附录 A.5 CT_Font），默认 unicode。</summary>
public enum Charset
{
    /// <summary>符号</summary>
    symbol,
    prc,
    big5,
    shift_jis,
    wansung,
    johab,
    /// <summary>默认值</summary>
    unicode,
}

public static class CharsetExtensions
{
    /// <summary>获取字符分类，空值默认 unicode，未知类型抛异常（与 Java 一致）。</summary>
    public static Charset GetInstance(string? name)
    {
        name = (name ?? "").Trim();
        if (name.Equals("symbol", StringComparison.OrdinalIgnoreCase)) return Charset.symbol;
        if (name.Equals("prc", StringComparison.OrdinalIgnoreCase)) return Charset.prc;
        if (name.Equals("big5", StringComparison.OrdinalIgnoreCase)) return Charset.big5;
        if (name.Equals("shift-jis", StringComparison.OrdinalIgnoreCase)) return Charset.shift_jis;
        if (name.Equals("wansung", StringComparison.OrdinalIgnoreCase)) return Charset.wansung;
        if (name.Equals("johab", StringComparison.OrdinalIgnoreCase)) return Charset.johab;
        if (name.Length == 0 || name.Equals("unicode", StringComparison.OrdinalIgnoreCase)) return Charset.unicode;
        throw new ArgumentException("未知的字形适用的字符分类：" + name);
    }

    /// <summary>XML 取值（Java Charset.toString 特例：shift_jis 输出 "shift-jis"）。</summary>
    public static string ToXmlValue(this Charset charset) =>
        charset == Charset.shift_jis ? "shift-jis" : charset.ToString();
}
