using Ofd.Core.Annotation.PageAnnot;
using Ofd.Core.BasicType;

namespace Ofd.Layout.Edit;

/// <summary>
/// 水印（Java Watermark）：一种特殊的注释，绘制平铺旋转文字。
/// </summary>
public class Watermark : Annotation
{
    /// <summary>水印绘制器。</summary>
    private WatermarkDrawer _watermarkDrawer;

    /// <summary>按页面布局构建水印（画布比页面大 50mm，保证可见区域效果）。</summary>
    public Watermark(PageLayout style)
        : this(0, 0, (style.GetWidth() ?? 0) + 50, (style.GetHeight() ?? 0) + 50) { }

    /// <summary>按绘制区域构建水印。</summary>
    public Watermark(double x, double y, double width, double height)
        : base(new ST_Box(x, y, width, height), AnnotType.Watermark, new WatermarkDrawer())
    {
        _watermarkDrawer = (WatermarkDrawer)base.GetDrawer()!;
        _watermarkDrawer.SetAnnotation(this);
    }

    /// <summary>获取水印绘制器。</summary>
    public new WatermarkDrawer GetDrawer() => _watermarkDrawer;

    /// <summary>设置水印绘制器。</summary>
    public Watermark SetDrawer(WatermarkDrawer drawer)
    {
        _watermarkDrawer = drawer;
        _watermarkDrawer.SetAnnotation(this);
        base.SetDrawer(drawer);
        return this;
    }

    /// <summary>设置文字水印内容。</summary>
    public Watermark SetValue(string? value)
    {
        _watermarkDrawer.SetValue(value);
        return this;
    }

    public string? GetValue() => _watermarkDrawer.GetValue();

    public string? GetColor() => _watermarkDrawer.GetColor();

    public Watermark SetColor(string? color)
    {
        _watermarkDrawer.SetColor(color);
        return this;
    }

    public string? GetFontName() => _watermarkDrawer.GetFontName();

    public Watermark SetFontName(string? fontName)
    {
        _watermarkDrawer.SetFontName(fontName);
        return this;
    }

    public double GetFontSize() => _watermarkDrawer.GetFontSize();

    public Watermark SetFontSize(double fontSize)
    {
        _watermarkDrawer.SetFontSize(fontSize);
        return this;
    }

    public bool? GetBold() => _watermarkDrawer.GetBold();

    public Watermark SetBold(bool? bold)
    {
        _watermarkDrawer.SetBold(bold);
        return this;
    }

    public string? GetFontWeight() => _watermarkDrawer.GetFontWeight();

    public Watermark SetFontWeight(string? fontWeight)
    {
        _watermarkDrawer.SetFontWeight(fontWeight);
        return this;
    }

    public bool? GetItalic() => _watermarkDrawer.GetItalic();

    public Watermark SetItalic(bool? italic)
    {
        _watermarkDrawer.SetItalic(italic);
        return this;
    }

    public double? GetLetterSpacing() => _watermarkDrawer.GetLetterSpacing();

    public Watermark SetLetterSpacing(double? letterSpacing)
    {
        _watermarkDrawer.SetLetterSpacing(letterSpacing);
        return this;
    }

    /// <summary>水印透明度（0.0~1.0，默认 0.5）。</summary>
    public double? GetGlobalAlpha() => _watermarkDrawer.GetGlobalAlpha();

    public Watermark SetGlobalAlpha(double? globalAlpha)
    {
        _watermarkDrawer.SetGlobalAlpha(globalAlpha);
        return this;
    }

    /// <summary>旋转角度（0~360，默认 330，对应数学 30 度）。</summary>
    public double? GetAngle() => _watermarkDrawer.GetAngle();

    public Watermark SetAngle(double? angle)
    {
        _watermarkDrawer.SetAngle(angle);
        return this;
    }

    public double? GetIntervalX() => _watermarkDrawer.GetIntervalX();

    public Watermark SetIntervalX(double? intervalX)
    {
        _watermarkDrawer.SetIntervalX(intervalX);
        return this;
    }

    public double? GetIntervalY() => _watermarkDrawer.GetIntervalY();

    public Watermark SetIntervalY(double? intervalY)
    {
        _watermarkDrawer.SetIntervalY(intervalY);
        return this;
    }

    /// <summary>设置外部字体（字体文件将嵌入 OFD）。</summary>
    public Watermark SetFont(string fontName, string? fontPath)
    {
        _watermarkDrawer.SetFont(fontName, fontPath);
        return this;
    }

    /// <summary>开启调试。</summary>
    public Watermark Debug()
    {
        _watermarkDrawer.SetDEBUG(true);
        return this;
    }
}
