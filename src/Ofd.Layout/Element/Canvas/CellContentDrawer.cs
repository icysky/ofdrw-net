using Ofd.Core.BasicType;
using SkiaSharp;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 单元格专用 Canvas 绘制器（Java CellContentDrawer）：文字自动换行、水平/垂直对齐、
/// 下划线/删除线、单元格内图片。
/// </summary>
public class CellContentDrawer : IDrawer
{
    /// <summary>调试开关：开启后绘制辅助线。</summary>
    public static bool DEBUG;

    /// <summary>关联的 Canvas。</summary>
    private readonly Canvas _canvas;

    /// <summary>单元格文字内容。</summary>
    private string? _value;

    /// <summary>文字水平对齐（默认左）。</summary>
    private CanvasTextAlign _textAlign = CanvasTextAlign.Left;

    /// <summary>文字垂直对齐（默认居中）。</summary>
    private VerticalAlign _verticalAlign = VerticalAlign.Center;

    /// <summary>文字颜色（默认 #000000）。</summary>
    private string? _color = "#000000";

    /// <summary>字体名称（默认宋体）。</summary>
    private string? _fontName = "宋体";

    /// <summary>字号（默认 3mm）。</summary>
    private double _fontSize = 3;

    /// <summary>行间距（默认 0.6mm）。</summary>
    private double? _lineSpace = 0.6;

    /// <summary>是否加粗。</summary>
    private bool? _bold = false;

    /// <summary>字体宽度（CSS3：normal | bold | ... | 100~900）。</summary>
    private string? _fontWeight = "normal";

    /// <summary>是否斜体。</summary>
    private bool? _italic = false;

    /// <summary>字间距。</summary>
    private double? _letterSpacing = 0d;

    /// <summary>图片。</summary>
    private ImgInfo? _img;

    /// <summary>是否有下划线。</summary>
    private bool _underline;

    /// <summary>是否删除线。</summary>
    private bool _deleteLine;

    /// <summary>外部字体路径。</summary>
    private string? _extFontPath;

    /// <summary>通过已有 Canvas 构造（会替换 Canvas 的绘制器）。</summary>
    public CellContentDrawer(Canvas canvas)
    {
        _canvas = canvas ?? throw new ArgumentException("Canvas 不能为空");
        canvas.SetDrawer(this);
    }

    /// <summary>创建单元格（x/y 为左下角坐标）。</summary>
    public CellContentDrawer(double x, double y, double width, double height)
    {
        _canvas = new Canvas(x, y, width, height);
        _canvas.SetDrawer(this);
    }

    /// <summary>文字行。</summary>
    private sealed class TextLine
    {
        public string Text { get; }
        public double Width { get; }

        public TextLine(string text, double width)
        {
            Text = text;
            Width = width;
            if (DEBUG) Console.WriteLine($">> text:{text} width:{width}");
        }
    }

    /// <summary>单元格图片信息。</summary>
    private sealed class ImgInfo
    {
        public string Path { get; }
        public double Width { get; set; }
        public double Height { get; set; }

        public ImgInfo(string path, double width, double height)
        {
            Path = path;
            Width = width;
            Height = height;
        }
    }

    /// <summary>单元格内部绘制。</summary>
    public void Draw(DrawContext ctx)
    {
        ctx.Save();
        try
        {
            if (_img != null)
            {
                DrawImg(ctx);
            }
            else
            {
                if (_extFontPath != null) ctx.AddFont(_fontName ?? "", _extFontPath);
                DrawText(ctx);
            }
        }
        finally
        {
            ctx.Restore();
        }
    }

    /// <summary>单元格图片绘制。</summary>
    private void DrawImg(DrawContext ctx)
    {
        if (_img == null) return;

        if (_img.Width <= 0 || _img.Height <= 0)
        {
            // 未指定宽高 → 按图片像素与 PPM 换算
            using var bmp = SKBitmap.Decode(_img.Path);
            if (bmp != null)
            {
                _img.Width = ctx.Mm(bmp.Width);
                _img.Height = ctx.Mm(bmp.Height);
            }
        }

        double canvasWidth = _canvas.GetWidth() ?? 0;
        double canvasHeight = _canvas.GetHeight() ?? 0;

        double x = _textAlign switch
        {
            CanvasTextAlign.Right or CanvasTextAlign.End => canvasWidth - _img.Width,
            CanvasTextAlign.Center => (canvasWidth - _img.Width) / 2d,
            _ => 0d,
        };

        double y = _verticalAlign switch
        {
            VerticalAlign.Bottom => canvasHeight - _img.Height,
            VerticalAlign.Center => (canvasHeight - _img.Height) / 2d,
            _ => 0d,
        };

        ctx.DrawImage(_img.Path, x, y, _img.Width, _img.Height);

        if (DEBUG) DebugBorder(ctx);
    }

    /// <summary>单元格文字绘制。</summary>
    private void DrawText(DrawContext ctx)
    {
        if (string.IsNullOrEmpty(_value)) return;

        // 字体样式字符串：[italic] [bold|weight] {size}mm {family}
        string fontStr = "";
        if (_italic == true) fontStr += "italic ";
        if (_bold == true) fontStr += "bold ";
        else if (!string.IsNullOrEmpty(_fontWeight)) fontStr += _fontWeight + " ";

        ctx.Font = fontStr + STBase.Fmt(_fontSize) + "mm " + _fontName;

        if (_letterSpacing != 0)
            ctx.GetFont().SetLetterSpacing(_letterSpacing ?? 0);

        if (!string.IsNullOrEmpty(_color))
            ctx.FillStyle = _color;

        double canvasWidth = _canvas.GetWidth() ?? 0;
        double canvasHeight = _canvas.GetHeight() ?? 0;
        double lineSpace = _lineSpace ?? 0;

        var metrics = ctx.MeasureText(_value);

        var lines = new List<TextLine>();
        double textLineWidth = 0;
        int offset = 0;

        for (int i = 0; i < _value.Length; i++)
        {
            char c = _value[i];

            // 换行符
            if (c == '\n')
            {
                lines.Add(new TextLine(_value[offset..i], textLineWidth));
                offset = i + 1;
                textLineWidth = 0;
                continue;
            }

            double cWidth = i == _value.Length - 1
                ? ctx.MeasureText(c.ToString()).Width ?? 0
                : metrics.Offset![i];

            if (textLineWidth + cWidth > canvasWidth)
            {
                // 超出宽度 → 换行
                lines.Add(new TextLine(_value[offset..i], textLineWidth));
                offset = i;
                textLineWidth = cWidth;
            }
            else
            {
                textLineWidth += cWidth;
            }
        }

        if (offset < _value.Length)
            lines.Add(new TextLine(_value[offset..], textLineWidth));

        // 内容高度：(字号 + 行间距) × 行数
        double contentHeight = (_fontSize + lineSpace) * lines.Count;

        // 首行 Y（文字坐标位于基线）
        double offsetY = _verticalAlign switch
        {
            VerticalAlign.Top => _fontSize,
            VerticalAlign.Center => canvasHeight / 2 - contentHeight / 2 + _fontSize,
            _ => canvasHeight - contentHeight + _fontSize,
        };

        foreach (var line in lines)
        {
            double offsetX = _textAlign switch
            {
                CanvasTextAlign.Center => (canvasWidth - line.Width) / 2d,
                CanvasTextAlign.Right or CanvasTextAlign.End => canvasWidth - line.Width,
                _ => 0d,
            };

            if (line.Text.Length > 0)
                ctx.FillText(line.Text, offsetX, offsetY);

            // 装饰线（比例为经验值，与上游一致）
            double fontLineWidth = _fontSize / 30;
            double underlineOffset = fontLineWidth * 3.2;

            if (_underline)
            {
                ctx.Save();
                ctx.SetLineWidth(fontLineWidth);
                ctx.BeginPath();
                ctx.MoveTo(offsetX, offsetY + underlineOffset);
                ctx.LineTo(offsetX + line.Width, offsetY + underlineOffset);
                ctx.Stroke();
                ctx.Restore();
            }

            if (_deleteLine)
            {
                ctx.Save();
                ctx.SetLineWidth(fontLineWidth);
                ctx.BeginPath();
                // 基线定位 → 删除线取 5/18 字高（经验值）
                ctx.MoveTo(offsetX, offsetY - _fontSize * 5 / 18);
                ctx.LineTo(offsetX + line.Width, offsetY - _fontSize * 5 / 18);
                ctx.Stroke();
                ctx.Restore();
            }

            if (DEBUG)
            {
                ctx.Save();
                ctx.SetLineDash(1.5d, 1.5d);
                ctx.SetLineWidth(0.1);
                ctx.SetGlobalAlpha(0.53);
                ctx.StrokeStyle = "rgb(255,0,0)";
                ctx.StrokeRect(offsetX, offsetY - _fontSize, line.Width, _fontSize + lineSpace);
                ctx.Restore();
            }

            offsetY += _fontSize + lineSpace;
        }

        if (DEBUG) DebugBorder(ctx);
    }

    /// <summary>绘制辅助线。</summary>
    private void DebugBorder(DrawContext ctx)
    {
        double width = _canvas.GetWidth() ?? 0;
        double height = _canvas.GetHeight() ?? 0;

        ctx.Save();
        ctx.SetLineDash(1.5d, 1.5d);
        ctx.SetLineWidth(0.353d);
        ctx.SetGlobalAlpha(0.53);
        ctx.StrokeStyle = "rgb(255,0,0)";
        ctx.MoveTo(0, 0);
        ctx.LineTo(width, height);
        ctx.MoveTo(width, 0);
        ctx.LineTo(0, height);
        ctx.Rect(0, 0, width, height);
        ctx.Stroke();
        ctx.Restore();
    }

    public string? GetValue() => _value;

    public CellContentDrawer SetValue(string? value)
    {
        _value = value;
        return this;
    }

    /// <summary>设置单元格图片（指定宽高，单位 mm）。</summary>
    public CellContentDrawer SetImage(string imgPath, double w, double h)
    {
        _img = new ImgInfo(imgPath, w, h);
        return this;
    }

    /// <summary>设置单元格图片（宽高由图片像素换算）。</summary>
    public CellContentDrawer SetImage(string imgPath)
    {
        _img = new ImgInfo(imgPath, 0, 0);
        return this;
    }

    public string? GetColor() => _color;

    public CellContentDrawer SetColor(string? color)
    {
        if (string.IsNullOrEmpty(color)) throw new ArgumentException("颜色(color)不能为空");
        _color = color;
        return this;
    }

    public string? GetFontName() => _fontName;

    public CellContentDrawer SetFontName(string? fontName)
    {
        _fontName = fontName;
        return this;
    }

    public double GetFontSize() => _fontSize;

    /// <summary>设置字号（必须大于 0）。</summary>
    public CellContentDrawer SetFontSize(double fontSize)
    {
        if (fontSize <= 0) throw new ArgumentException("字号(fontSize)必须大于0");
        _fontSize = fontSize;
        return this;
    }

    public Canvas GetCanvas() => _canvas;

    public CanvasTextAlign GetTextAlign() => _textAlign;

    public CellContentDrawer SetTextAlign(CanvasTextAlign textAlign)
    {
        _textAlign = textAlign;
        return this;
    }

    public VerticalAlign GetVerticalAlign() => _verticalAlign;

    public CellContentDrawer SetVerticalAlign(VerticalAlign verticalAlign)
    {
        _verticalAlign = verticalAlign;
        return this;
    }

    public double? GetLineSpace() => _lineSpace;

    public CellContentDrawer SetLineSpace(double? lineSpace)
    {
        _lineSpace = lineSpace;
        return this;
    }

    [Obsolete("单词拼写错误，请使用 GetBold()")]
    public bool? GetBlob() => _bold;

    [Obsolete("单词拼写错误，请使用 SetBold(bool?)")]
    public CellContentDrawer SetBlob(bool? blob)
    {
        _bold = blob;
        return this;
    }

    public bool? GetBold() => _bold;

    public CellContentDrawer SetBold(bool? bold)
    {
        _bold = bold;
        return this;
    }

    public bool? GetItalic() => _italic;

    public CellContentDrawer SetItalic(bool? italic)
    {
        _italic = italic;
        return this;
    }

    public double? GetLetterSpacing() => _letterSpacing;

    public CellContentDrawer SetLetterSpacing(double? letterSpacing)
    {
        _letterSpacing = letterSpacing;
        return this;
    }

    public string? GetImgPath() => _img?.Path;

    public double GetImgWidth() => _img?.Width ?? 0;

    public double GetImgHeight() => _img?.Height ?? 0;

    public CellContentDrawer SetUnderline(bool underline)
    {
        _underline = underline;
        return this;
    }

    public bool GetUnderline() => _underline;

    public CellContentDrawer SetDeleteLine(bool deleteLine)
    {
        _deleteLine = deleteLine;
        return this;
    }

    public bool GetDeleteLine() => _deleteLine;

    public string? GetFontWeight() => _fontWeight;

    public CellContentDrawer SetFontWeight(string? fontWeight)
    {
        _fontWeight = fontWeight;
        return this;
    }

    /// <summary>
    /// 设置单元格使用的外部字体（字体文件会直接嵌入 OFD）。
    /// <para>注：Java 原实现返回 null，此处修正为返回 this。</para>
    /// </summary>
    public CellContentDrawer SetFont(string fontName, string? fontPath)
    {
        if (string.IsNullOrEmpty(fontName)) throw new ArgumentException("字体名称(fontName)不能为空");
        if (fontPath == null || !File.Exists(fontPath)) throw new ArgumentException("字体文件(fontPath)不存在");

        SetFontName(fontName);
        _extFontPath = fontPath;
        return this;
    }
}
