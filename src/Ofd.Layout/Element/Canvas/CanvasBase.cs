namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// Canvas 扩展基类（Java CanvasBase）：实现者只需实现 <see cref="IDrawer.Draw"/>，
/// 构造时自动把自己设为绘制器。
/// </summary>
public abstract class CanvasBase : Canvas, IDrawer
{
    public CanvasBase(double? width, double? height) : base(width, height)
    {
        SetDrawer(this);
    }

    public CanvasBase(double x, double y, double w, double h) : base(x, y, w, h)
    {
        SetDrawer(this);
    }

    public CanvasBase(PageLayout style) : base(style)
    {
        SetDrawer(this);
    }

    /// <summary>执行自定义绘制。</summary>
    public abstract void Draw(DrawContext ctx);
}
