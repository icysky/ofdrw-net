using Ofd.Core.BasicType;
using Ofd.Layout.Element.Canvas;

namespace Ofd.Layout.Edit;

/// <summary>
/// 水印绘制器（Java WatermarkDrawer）：按间距平铺旋转的文字水印。
/// </summary>
public class WatermarkDrawer : IDrawer
{
    /// <summary>是否启用调试（绘制辅助线）。</summary>
    private bool _debug;

    /// <summary>水印文字内容。</summary>
    private string? _value;

    /// <summary>所属注释对象。</summary>
    private Annotation? _annotation;

    /// <summary>外部字体文件路径。</summary>
    private string? _extFontPath;

    /// <summary>文字颜色（默认 #000000）。</summary>
    private string? _color = "#000000";

    /// <summary>字体名称（默认宋体）。</summary>
    private string? _fontName = "宋体";

    /// <summary>字号（mm，默认 4.5）。</summary>
    private double _fontSize = 4.5;

    /// <summary>是否加粗。</summary>
    private bool? _bold = false;

    /// <summary>字体宽度（CSS3）。</summary>
    private string? _fontWeight = "normal";

    /// <summary>是否斜体。</summary>
    private bool? _italic = false;

    /// <summary>字间距。</summary>
    private double? _letterSpacing = 0d;

    /// <summary>透明度（默认 0.5）。</summary>
    private double? _globalAlpha = 0.5d;

    /// <summary>旋转角度（0~360，默认 330）。</summary>
    private double? _angle = 330d;

    /// <summary>横向间距（mm，默认 30）。</summary>
    private double? _intervalX = 30d;

    /// <summary>纵向间距（mm，默认 30）。</summary>
    private double? _intervalY = 30d;

    /// <summary>绘制水印。</summary>
    public void Draw(DrawContext ctx)
    {
        if (string.IsNullOrEmpty(_value)) return;
        if (_annotation == null) return;

        if (_extFontPath != null) ctx.AddFont(_fontName ?? "", _extFontPath);

        ctx.FillStyle = _color;
        ctx.Font = GetFontString();

        if (_letterSpacing != 0) ctx.GetFont().SetLetterSpacing(_letterSpacing ?? 0);

        if (!string.IsNullOrEmpty(_color)) ctx.FillStyle = _color;

        var boundary = _annotation.GetBoundary();
        double width = boundary.Width;
        double height = boundary.Height;

        // 支持多行水印：按 \n 分行，逐行按行高错位绘制（Java 原实现仅单行）
        var lines = _value.Replace("\r\n", "\n").Split('\n');

        var metrics = ctx.MeasureText(_value);
        double lineHeight = metrics.FontSize ?? 0;

        // 防零步长死循环：间距 <= 0 时无法推进，直接按单次绘制处理
        double stepX = _intervalX is > 0 ? _intervalX.Value : width;
        double stepY = _intervalY is > 0 ? _intervalY.Value : height;

        double x = boundary.TopLeftX;
        while (x < width)
        {
            double y = boundary.TopLeftY;
            while (y < height)
            {
                ctx.Save();
                ctx.SetGlobalAlpha(_globalAlpha);
                ctx.Translate(x, y);
                ctx.Rotate(_angle ?? 0);

                for (int i = 0; i < lines.Length; i++)
                    ctx.FillText(lines[i], 0, (i + 1) * lineHeight);

                ctx.Restore();

                if (_debug) DebugBorder(ctx, metrics, x, y);

                y += stepY;
            }
            x += stepX;
        }
    }

    /// <summary>组装 CSS 字体描述。</summary>
    private string GetFontString()
    {
        string fontStr = "";
        if (_italic == true) fontStr += "italic ";
        if (_bold == true) fontStr += "bold ";
        else if (!string.IsNullOrEmpty(_fontWeight)) fontStr += _fontWeight + " ";

        return fontStr + STBase.Fmt(_fontSize) + "mm " + _fontName;
    }

    public string? GetValue() => _value;

    public void SetValue(string? value) => _value = value;

    public string? GetColor() => _color;

    public void SetColor(string? color) => _color = color;

    public string? GetFontName() => _fontName;

    public void SetFontName(string? fontName) => _fontName = fontName;

    public double GetFontSize() => _fontSize;

    public void SetFontSize(double fontSize) => _fontSize = fontSize;

    public bool? GetBold() => _bold;

    public void SetBold(bool? bold) => _bold = bold;

    public string? GetFontWeight() => _fontWeight;

    public void SetFontWeight(string? fontWeight) => _fontWeight = fontWeight;

    public bool? GetItalic() => _italic;

    public void SetItalic(bool? italic) => _italic = italic;

    public double? GetLetterSpacing() => _letterSpacing;

    public void SetLetterSpacing(double? letterSpacing) => _letterSpacing = letterSpacing;

    public double? GetGlobalAlpha() => _globalAlpha;

    /// <summary>设置透明度（越界抛异常）。</summary>
    public void SetGlobalAlpha(double? globalAlpha)
    {
        if (globalAlpha > 1 || globalAlpha < 0)
            throw new ArgumentException("透明度超出范围0.0~1.0");

        _globalAlpha = globalAlpha;
    }

    public double? GetAngle() => _angle;

    /// <summary>设置旋转角度（-360~360，负值转为 360+angle）。</summary>
    public void SetAngle(double? angle)
    {
        if (angle > 360 || angle < -360)
            throw new ArgumentException("旋转角度超出范围");

        if (angle < 0) angle = 360 + angle;
        _angle = angle;
    }

    public double? GetIntervalX() => _intervalX;

    public void SetIntervalX(double? intervalX) => _intervalX = intervalX;

    public double? GetIntervalY() => _intervalY;

    public void SetIntervalY(double? intervalY) => _intervalY = intervalY;

    public bool IsDEBUG() => _debug;

    public void SetDEBUG(bool debug) => _debug = debug;

    public Annotation? GetAnnotation() => _annotation;

    public void SetAnnotation(Annotation? annotation) => _annotation = annotation;

    /// <summary>
    /// 设置外部字体。
    /// <para>注：Java 原实现返回 null，此处返回 this。</para>
    /// </summary>
    public WatermarkDrawer SetFont(string fontName, string? fontPath)
    {
        if (string.IsNullOrEmpty(fontName)) throw new ArgumentException("字体名称(fontName)不能为空");
        if (fontPath == null || !File.Exists(fontPath)) throw new ArgumentException("字体文件(fontPath)不存在");

        SetFontName(fontName);
        _extFontPath = fontPath;
        return this;
    }

    /// <summary>绘制调试辅助线。</summary>
    private void DebugBorder(DrawContext ctx, TextMetrics metrics, double offsetX, double offsetY)
    {
        const double lineWidth = 0.353d;
        double width = (metrics.Width ?? 0) + lineWidth * 2;
        double height = (metrics.FontSize ?? 0) + lineWidth * 2;

        ctx.Save();
        ctx.Translate(offsetX, offsetY);
        ctx.SetLineDash(1.5d, 1.5d);
        ctx.SetLineWidth(lineWidth);
        ctx.SetGlobalAlpha(0.53);
        ctx.StrokeStyle = "rgb(255,0,0)";
        ctx.MoveTo(0, 0);
        ctx.LineTo(width, height);
        ctx.MoveTo(width, 0);
        ctx.LineTo(0, height);
        ctx.Rotate(_angle ?? 0);
        ctx.Rect(offsetX, offsetY, width, height);
        ctx.Stroke();
        ctx.Restore();
    }
}
