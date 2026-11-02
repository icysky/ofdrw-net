using SkiaSharp;

namespace Ofd.Font;

/// <summary>
/// 字体度量适配层（Java AWT FontMetrics 的 SkiaSharp 对应）。
/// 统一以 100pt 为基准字号取值，调用方按实际字号缩放——
/// 这样与 AWT "size 1 font 派生" 的口径一致，golden 基准即按此对照：
/// tests/Ofd.Tests/TestData/font-java.txt（8 字体 × 360 字符 0 偏差）。
/// </summary>
public static class FontMetrics
{
    private static readonly Dictionary<string, SKFont?> Cache = new();

    /// <summary>
    /// 按字体族名解析 SKFont（100pt 基准）。解析失败或命中系统兜底字体
    /// ("Last Resort") 时返回 null——调用方应走字体别名回退链（P4 实现）。
    /// </summary>
    public static SKFont? Resolve(string familyName)
    {
        if (Cache.TryGetValue(familyName, out var cached)) return cached;
        SKFont? f = null;
        var tf = SKTypeface.FromFamilyName(familyName,
            SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        if (tf != null && !tf.FamilyName.Equals("Last Resort", StringComparison.OrdinalIgnoreCase))
            f = new SKFont(tf, 100f);
        Cache[familyName] = f;
        return f;
    }

    /// <summary>文本宽度（基准字号归一，对应 Java FontMetrics.stringWidth 语义域）。</summary>
    public static float MeasureWidth(string familyName, string text)
    {
        var f = Resolve(familyName);
        return f == null ? 0f : f.MeasureText(text) / 100f;
    }

    /// <summary>行高三元组（ascent/descent/height，均为正值），失败返回 null。</summary>
    public static (double Ascent, double Descent, double Height)? GetLineMetrics(string familyName)
    {
        var f = Resolve(familyName);
        if (f == null) return null;
        var m = f.Metrics;
        double scale = 100f;
        return (-m.Ascent / scale, m.Descent / scale, (m.Descent - m.Ascent) / scale);
    }
}
