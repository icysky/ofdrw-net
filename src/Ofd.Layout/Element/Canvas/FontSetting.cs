using SkiaSharp;
using Ofd.Font;
using Ofd.Layout.Element;
using FontType = Ofd.Font.Font;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 字体设置（Java FontSetting）：字体、字号(mm)、粗细、斜体、字间距、字符方向、阅读方向、对齐方式。
/// </summary>
public class FontSetting : ITextFontInfo
{
    /// <summary>字体对象。</summary>
    private FontType _fontObj;

    /// <summary>字号（mm）。</summary>
    private double _fontSize;

    /// <summary>是否斜体。</summary>
    private bool _italic;

    /// <summary>字体粗细（100~900，默认 400，加粗为 800）。</summary>
    private int _fontWeight = 400;

    /// <summary>字间距（mm）。</summary>
    private double _letterSpacing;

    /// <summary>字符方向（0/90/180/270）。</summary>
    private int _charDirection;

    /// <summary>阅读方向（0/90/180/270）。</summary>
    private int _readDirection;

    /// <summary>文本对齐方式（默认 start）。</summary>
    private CanvasTextAlign _textAlign = CanvasTextAlign.Start;

    /// <summary>默认字体配置（宋体，5mm）。</summary>
    public static FontSetting GetInstance() => new(5, Ofd.Font.FontName.SimSun.Font());

    /// <summary>指定字号的字体配置（宋体）。</summary>
    public static FontSetting GetInstance(double fontSize) => new(fontSize, Ofd.Font.FontName.SimSun.Font());

    public FontSetting(double fontSize, FontType fontObj)
    {
        _fontObj = fontObj ?? throw new ArgumentException("字体对象(fontObj)为空");
        _fontSize = fontSize;
    }

    public CanvasTextAlign GetTextAlign() => _textAlign;

    public FontSetting SetTextAlign(CanvasTextAlign? textAlign)
    {
        if (textAlign == null) return this;
        _textAlign = textAlign.Value;
        return this;
    }

    public FontType GetFont() => _fontObj;

    public FontSetting SetFont(FontType fontObj)
    {
        _fontObj = fontObj ?? throw new ArgumentException("字体对象(fontObj)为空");
        return this;
    }

    public double? GetFontSize() => _fontSize;

    /// <summary>设置字号（mm）。</summary>
    public FontSetting SetFontSize(double fontSize)
    {
        _fontSize = fontSize;
        return this;
    }

    public bool IsItalic() => _italic;

    public FontSetting SetItalic(bool italic)
    {
        _italic = italic;
        return this;
    }

    /// <summary>设为加粗（粗细 800）。</summary>
    public FontSetting SetBold() => SetFontWeight(800);

    public int? GetFontWeight() => _fontWeight;

    public int GetCharDirection() => _charDirection;

    /// <summary>设置字符方向（仅允许 0/90/180/270）。</summary>
    public FontSetting SetCharDirection(int charDirection)
    {
        if (charDirection is not (0 or 90 or 180 or 270))
            throw new ArgumentException("字符方向(charDirection) 允许值：0、90、180、270，错误值：" + charDirection);

        _charDirection = charDirection;
        return this;
    }

    public int GetReadDirection() => _readDirection;

    /// <summary>设置阅读方向（仅允许 0/90/180/270）。</summary>
    public FontSetting SetReadDirection(int readDirection)
    {
        if (readDirection is not (0 or 90 or 180 or 270))
            throw new ArgumentException("阅读方向(readDirection) 允许值：0、90、180、270，错误值：" + readDirection);

        _readDirection = readDirection;
        return this;
    }

    public double? GetLetterSpacing() => _letterSpacing;

    /// <summary>设置字间距（小于 0 自动修正为 0）。</summary>
    public FontSetting SetLetterSpacing(double letterSpacing)
    {
        _letterSpacing = letterSpacing < 0 ? 0 : letterSpacing;
        return this;
    }

    /// <summary>设置字体粗细（仅允许 100~900 的百位值）。</summary>
    public FontSetting SetFontWeight(int? fontWeight)
    {
        int w = fontWeight ?? 400;
        if (w % 100 != 0 || w < 100 || w > 900)
            throw new FormatException("字体宽度(fontWeight)可选值： 100、200、300、400、500、600、700、800、900");

        _fontWeight = w;
        return this;
    }

    /// <summary>字符宽度（mm）。</summary>
    /// <summary>是否启用光学字距（仅影响 ASCII 可打印字符）。</summary>
    private bool _opticalKerning;

    /// <summary>字形墨迹宽度缓存。</summary>
    private readonly Dictionary<char, double> _inkCache = new();

    /// <summary>
    /// 设置光学字距：按「字形墨迹 + 固定间隙」推进，而不是字体默认的等宽 advance。
    /// <para>
    /// 适用场景：中文等宽字体的 ASCII 为 0.5em 等宽设计，但数字字面窄、字母（如 M/X）墨迹宽，
    /// 等宽 advance 会导致「数字之后紧跟字母时视觉过挤」。开启后各字符间隙趋于一致。
    /// </para>
    /// </summary>
    public FontSetting SetOpticalKerning(bool opticalKerning)
    {
        _opticalKerning = opticalKerning;
        return this;
    }

    /// <summary>是否启用光学字距。</summary>
    public bool IsOpticalKerning() => _opticalKerning;

    /// <summary>字符宽度：启用光学字距时，ASCII 用「墨迹宽 + 间隙」。</summary>
    public double CharWidth(char c) =>
        _opticalKerning && IsOpticalTarget(c)
            ? InkWidth(c) + OpticalGap()
            : Box(c).Width;

    /// <summary>光学字距的固定间隙（字号的 12%，下限 0.2mm）。</summary>
    private double OpticalGap() => Math.Max(_fontSize * 0.12, 0.2);

    private static bool IsOpticalTarget(char c) => c >= '!' && c <= '~';

    /// <summary>字形墨迹宽度（mm），按字体文件即时测量并缓存。</summary>
    private double InkWidth(char c)
    {
        if (_inkCache.TryGetValue(c, out double cached)) return cached;

        double ink;
        string? path = _fontObj.GetFontFile();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            ink = Box(c).Width; // 无字体文件（系统字体）时退回原宽度
        }
        else
        {
            using var typeface = SKTypeface.FromFile(path);
            if (typeface == null)
            {
                ink = Box(c).Width;
            }
            else
            {
                using var font = new SKFont(typeface, (float)_fontSize);
                var glyphs = font.GetGlyphs(c.ToString());
                if (glyphs.Length == 0)
                {
                    ink = Box(c).Width;
                }
                else
                {
                    // 用字形轮廓的紧密边界作为“墨迹宽”，比 MeasureText 的 advance 更准
                    using var glyphPath = font.GetGlyphPath(glyphs[0]);
                    double width = glyphPath?.Bounds.Width ?? 0;
                    ink = width > 0 ? width : Box(c).Width;
                }
            }
        }

        _inkCache[c] = ink;
        return ink;
    }

    /// <summary>字符所占空间（mm）。</summary>
    public TextBounds Box(char c)
    {
        // 有预设宽度映射表 → 查表
        if (_fontObj.HasWidthMath())
            return new TextBounds(_fontObj.GetCharWidthScale(c) * _fontSize, _fontSize);

        // 否则按字体度量（Skia）
        return EnvFont.StrBounds(_fontObj.GetName(), _fontObj.GetFamilyName(), c.ToString(), _fontSize);
    }

    /// <summary>
    /// 克隆。
    /// <para>注：Java 原实现未复制 letterSpacing，会导致克隆后字间距丢失，此处补齐。</para>
    /// </summary>
    public FontSetting Clone() => new FontSetting(_fontSize, _fontObj)
        .SetFont(_fontObj)
        .SetItalic(_italic)
        .SetFontSize(_fontSize)
        .SetFontWeight(_fontWeight)
        .SetCharDirection(_charDirection)
        .SetReadDirection(_readDirection)
        .SetTextAlign(_textAlign)
        .SetLetterSpacing(_letterSpacing);
}
