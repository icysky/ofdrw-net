namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// Canvas 内容绘制器（Java Drawer 函数式接口）。
/// </summary>
public interface IDrawer
{
    /// <summary>执行绘制。</summary>
    void Draw(DrawContext ctx);
}
