namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 线条元素（Java Line）：快速构建一条线段；复杂图形请使用 Canvas + Drawer。
/// </summary>
public class Line : CanvasBase
{
    /// <summary>线条起点坐标 [x, y]。</summary>
    private double[] _beginPoint = { 0, 0 };

    /// <summary>线条终点坐标 [x, y]。</summary>
    private double[] _endPoint = { 0, 0 };

    /// <summary>线条颜色（#000000 / rgb() / rgba() / 颜色名）。</summary>
    private string? _lineColor = "#000000";

    /// <summary>线条宽度（默认 0.353mm）。</summary>
    private double _lineWidth = 0.353d;

    /// <summary>线条透明度 [0,1]，默认 1。</summary>
    private double _lineOpacity = 1.0d;

    public Line(double? width, double? height) : base(width, height) { }

    public Line(double x, double y, double w, double h) : base(x, y, w, h) { }

    public double[]? GetBeginPoint() => _beginPoint;

    public Line SetBeginPoint(double[]? beginPoint)
    {
        _beginPoint = beginPoint ?? Array.Empty<double>();
        return this;
    }

    public Line SetBeginPoint(double beginX, double beginY)
    {
        _beginPoint = new[] { beginX, beginY };
        return this;
    }

    public double[]? GetEndPoint() => _endPoint;

    public Line SetEndPoint(double[]? endPoint)
    {
        _endPoint = endPoint ?? Array.Empty<double>();
        return this;
    }

    public Line SetEndPoint(double endX, double endY)
    {
        _endPoint = new[] { endX, endY };
        return this;
    }

    public string? GetLineColor() => _lineColor;

    public Line SetLineColor(string? lineColor)
    {
        _lineColor = lineColor;
        return this;
    }

    public double GetLineWidth() => _lineWidth;

    /// <summary>设置线宽（mm）。</summary>
    public Line SetLineWidth(double lineWidth)
    {
        _lineWidth = lineWidth;
        return this;
    }

    public double GetLineOpacity() => _lineOpacity;

    /// <summary>设置透明度 [0,1]。</summary>
    public Line SetLineOpacity(double lineOpacity)
    {
        _lineOpacity = lineOpacity;
        return this;
    }

    /// <summary>绘制线条。</summary>
    public override void Draw(DrawContext ctx)
    {
        _lineColor ??= "#000000";

        // 线宽 <= 0 不绘制
        if (_lineWidth <= 0d) return;

        if (_beginPoint == null || _beginPoint.Length < 2) return;
        if (_endPoint == null || _endPoint.Length < 2) return;

        // 起终点相同不绘制
        if (_beginPoint[0] == _endPoint[0] && _beginPoint[1] == _endPoint[1]) return;

        ctx.Save();
        try
        {
            ctx.SetLineWidth(_lineWidth);
            ctx.BeginPath();
            ctx.SetGlobalAlpha(_lineOpacity);
            ctx.StrokeStyle = _lineColor;
            ctx.MoveTo(_beginPoint[0], _beginPoint[1]);
            ctx.LineTo(_endPoint[0], _endPoint[1]);
            ctx.Stroke();
        }
        finally
        {
            ctx.Restore();
        }
    }
}
