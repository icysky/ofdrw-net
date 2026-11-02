namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 单元格对象（Java Cell）：以 Canvas 为载体，内容绘制委托给 <see cref="CellContentDrawer"/>。
/// </summary>
public class Cell : Canvas
{
    /// <summary>单元格内容绘制器。</summary>
    private readonly CellContentDrawer _cellDrawer;

    public Cell(double? width, double? height) : base(width, height)
    {
        _cellDrawer = new CellContentDrawer(this);
        SetDrawer(_cellDrawer);
    }

    public Cell(double x, double y, double w, double h) : base(x, y, w, h)
    {
        _cellDrawer = new CellContentDrawer(this);
        SetDrawer(_cellDrawer);
    }

    /// <summary>Cell 的绘制器必须是 <see cref="CellContentDrawer"/>。</summary>
    public override Canvas SetDrawer(IDrawer? drawer)
    {
        if (drawer is not CellContentDrawer)
            throw new ArgumentException("Cell的绘制器必须是CellContentDrawer");

        base.SetDrawer(drawer);
        return this;
    }

    /// <summary>获取单元格内容绘制器。</summary>
    public new CellContentDrawer GetDrawer() => _cellDrawer;

    public string? GetValue() => _cellDrawer.GetValue();

    /// <summary>设置单元格文字内容。</summary>
    public Cell SetValue(string? value)
    {
        _cellDrawer.SetValue(value);
        return this;
    }

    /// <summary>设置单元格图片（指定宽高，mm）。</summary>
    public Cell SetImage(string imgPath, double w, double h)
    {
        _cellDrawer.SetImage(imgPath, w, h);
        return this;
    }

    /// <summary>设置单元格图片（宽高由图片像素换算）。</summary>
    public Cell SetImage(string imgPath)
    {
        _cellDrawer.SetImage(imgPath);
        return this;
    }

    public string? GetColor() => _cellDrawer.GetColor();

    /// <summary>设置单元格文字颜色。</summary>
    public Cell SetColor(string? color)
    {
        if (string.IsNullOrEmpty(color)) throw new ArgumentException("颜色(color)不能为空");
        _cellDrawer.SetColor(color);
        return this;
    }

    public string? GetFontName() => _cellDrawer.GetFontName();

    public Cell SetFontName(string? fontName)
    {
        _cellDrawer.SetFontName(fontName);
        return this;
    }

    /// <summary>设置单元格外部字体（字体文件将嵌入 OFD）。</summary>
    public Cell SetFont(string fontName, string? fontPath)
    {
        _cellDrawer.SetFont(fontName, fontPath);
        return this;
    }

    public double GetFontSize() => _cellDrawer.GetFontSize();

    public Cell SetFontSize(double fontSize)
    {
        _cellDrawer.SetFontSize(fontSize);
        return this;
    }

    public CanvasTextAlign GetTextAlign() => _cellDrawer.GetTextAlign();

    public Cell SetTextAlign(CanvasTextAlign textAlign)
    {
        _cellDrawer.SetTextAlign(textAlign);
        return this;
    }

    public VerticalAlign GetVerticalAlign() => _cellDrawer.GetVerticalAlign();

    public Cell SetVerticalAlign(VerticalAlign verticalAlign)
    {
        _cellDrawer.SetVerticalAlign(verticalAlign);
        return this;
    }

    public double? GetLineSpace() => _cellDrawer.GetLineSpace();

    public Cell SetLineSpace(double? lineSpace)
    {
        _cellDrawer.SetLineSpace(lineSpace);
        return this;
    }

    [Obsolete("单词拼写错误，请使用 GetBold()")]
    public bool? GetBlob() => _cellDrawer.GetBlob();

    [Obsolete("单词拼写错误，请使用 SetBold(bool?)")]
    public Cell SetBlob(bool? blob)
    {
        _cellDrawer.SetBlob(blob);
        return this;
    }

    public bool? GetBold() => _cellDrawer.GetBold();

    public Cell SetBold(bool? bold)
    {
        _cellDrawer.SetBold(bold);
        return this;
    }

    public bool? GetItalic() => _cellDrawer.GetItalic();

    public Cell SetItalic(bool? italic)
    {
        _cellDrawer.SetItalic(italic);
        return this;
    }

    public double? GetLetterSpacing() => _cellDrawer.GetLetterSpacing();

    public Cell SetLetterSpacing(double? letterSpacing)
    {
        _cellDrawer.SetLetterSpacing(letterSpacing);
        return this;
    }

    public Cell SetUnderline(bool underline)
    {
        _cellDrawer.SetUnderline(underline);
        return this;
    }

    public bool GetUnderline() => _cellDrawer.GetUnderline();

    public Cell SetDeleteLine(bool deleteLine)
    {
        _cellDrawer.SetDeleteLine(deleteLine);
        return this;
    }

    public bool GetDeleteLine() => _cellDrawer.GetDeleteLine();

    public string? GetImgPath() => _cellDrawer.GetImgPath();

    public double GetImgWidth() => _cellDrawer.GetImgWidth();

    public double GetImgHeight() => _cellDrawer.GetImgHeight();

    public string? GetFontWeight() => _cellDrawer.GetFontWeight();

    public Cell SetFontWeight(string? fontWeight)
    {
        _cellDrawer.SetFontWeight(fontWeight);
        return this;
    }
}
