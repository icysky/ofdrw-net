using SkiaSharp;

namespace Ofd.Font;

/// <summary>
/// 字体（Java org.ofdrw.font.Font）：名称 + 族名 + 字体文件 + 可打印字符宽度映射。
/// <para>C# 侧以 SkiaSharp 的 <see cref="SKTypeface"/> 替代 AWT Font。</para>
/// </summary>
public class Font
{
    /// <summary>字体名称。</summary>
    private string? _name;

    /// <summary>字体族名称。</summary>
    private string? _familyName;

    /// <summary>字体文件路径。</summary>
    private string? _fontFile;

    /// <summary>可打印字符宽度映射（索引 = 字符码 - 32）。</summary>
    private double[]? _printableAsciiWidthMap;

    /// <summary>缓存的 Skia 字体。</summary>
    private SKTypeface? _fontObj;

    /// <summary>是否可嵌入 OFD 文件包。</summary>
    private bool _embeddable = true;

    /// <summary>默认字体（宋体）。</summary>
    /// <summary>
    /// 默认字体：优先使用随包内置的**仿宋**（内嵌字形，跨平台一致，
    /// Linux 容器无需安装中文字体）；内置文件缺失时回退系统宋体。
    /// <para>
    /// 说明：不采用 Noto 子集——实测数科等阅读器对子集字体渲染异常（字符叠加）。
    /// </para>
    /// </summary>
    public static Font GetDefault() => BuiltinFonts.FangSong() ?? FontName.SimSun.Font();

    public Font(string name, string familyName)
    {
        _name = name;
        _familyName = familyName;
    }

    public Font(string name, string familyName, string? fontFile)
    {
        _name = name;
        _familyName = familyName;
        if (fontFile == null || !File.Exists(fontFile))
            throw new ArgumentException("字体文件(fontFile)不存在");
        SetFontFile(fontFile);
    }

    /// <summary>仅指定字体名与字体文件创建字体（对应 Java 的 Font(String, Path) 重载）。</summary>
    public static Font FromFile(string name, string? fontFile)
    {
        if (fontFile == null || !File.Exists(fontFile))
            throw new ArgumentException("字体文件(fontFile)不存在");

        var font = new Font(name, (string?)null);
        font.SetFontFile(fontFile);
        return font;
    }

    /// <summary>创建字体并指定可打印字符宽度映射表（不做文件存在性校验，与 Java 一致）。</summary>
    public Font(string name, string? familyName, string? fontFile, double[]? printableAsciiWidthMap)
    {
        _name = name;
        _familyName = familyName;
        _fontFile = fontFile;
        _printableAsciiWidthMap = printableAsciiWidthMap;
    }

    /// <summary>是否存在预设的字符宽度映射表。</summary>
    public bool HasWidthMath() => _printableAsciiWidthMap is { Length: > 0 };

    /// <summary>获取字符宽度占比（0~1）。</summary>
    public double GetCharWidthScale(char txt)
    {
        if (_printableAsciiWidthMap != null)
            return txt is >= (char)32 and <= (char)126 ? _printableAsciiWidthMap[txt - 32] : 1;

        // 无映射：ASCII 半角 0.5，其余 1
        return txt is >= (char)32 and <= (char)126 ? 0.5 : 1;
    }

    /// <summary>设置可打印字符宽度映射表。</summary>
    public Font SetPrintableAsciiWidthMap(double[]? map)
    {
        _printableAsciiWidthMap = map;
        return this;
    }

    /// <summary>获取字体全名（name-family）。</summary>
    public string GetCompleteFontName() => _familyName == null ? _name ?? "" : _name + "-" + _familyName;

    public string? GetName() => _name;

    public Font SetName(string name)
    {
        _name = name;
        return this;
    }

    public string? GetFamilyName() => _familyName;

    public Font SetFamilyName(string familyName)
    {
        _familyName = familyName;
        return this;
    }

    public string? GetFontFile() => _fontFile;

    /// <summary>获取字体文件名称。</summary>
    public string GetFontFileName() => Path.GetFileName(_fontFile ?? "");

    /// <summary>设置字体文件（加载 Skia 字体，格式错误抛异常）。</summary>
    public Font SetFontFile(string fontFile)
    {
        _fontFile = fontFile;
        SKTypeface? typeface = null;
        try
        {
            typeface = SKTypeface.FromFile(fontFile);
        }
        catch (Exception)
        {
            throw new ArgumentException("字体文件(fontFile)格式错误");
        }

        if (typeface == null) throw new ArgumentException("字体文件(fontFile)格式错误");
        _fontObj = typeface;
        return this;
    }

    /// <summary>直接设置 Skia 字体（用于系统中已有字体，无字体文件场景）。</summary>
    public Font SetTypeface(SKTypeface typeface)
    {
        _fontObj = typeface;
        return this;
    }

    /// <summary>获取 Skia 字体对象，未提供字体文件时为 null。</summary>
    public SKTypeface? GetFontObj() => _fontObj;

    /// <summary>字体是否嵌入 OFD 文件包。</summary>
    public bool IsEmbeddable() => _embeddable;

    /// <summary>设置是否嵌入 OFD 文件包（默认 true）。</summary>
    public Font SetEmbeddable(bool embeddable)
    {
        _embeddable = embeddable;
        return this;
    }
}
