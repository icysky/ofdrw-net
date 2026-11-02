using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;

namespace Ofd.Layout.Engine.Render;

/// <summary>
/// Canvas 渲染器（Java CanvasRender）：创建页块 → 构建绘制上下文 → 调用绘制器。
/// </summary>
public class CanvasRender : IProcessor
{
    public void Render(ST_Loc pageLoc, CT_PageBlock layer, ResManager resManager, Div e, AtomicCounter maxUnitId)
    {
        if (e is Canvas canvas)
            Render(layer, resManager, canvas, maxUnitId);
    }

    /// <summary>渲染 Canvas 到图层。</summary>
    public static void Render(CT_PageBlock layer, ResManager resManager, Canvas canvas, AtomicCounter maxUnitId)
    {
        IDrawer? drawer = canvas.GetDrawer();
        if (drawer == null) return;

        // 优先页块，不存在则创建并加入图层
        var block = canvas.GetPreferBlock();
        if (block == null)
        {
            block = new CT_PageBlock();
            block.SetObjID(maxUnitId.IncrementAndGet());
            canvas.SetPreferBlock(block);
            layer.AddPageBlock(block);
        }

        // 盒式模型内边界
        var boundary = new ST_Box(
            (canvas.GetX() ?? 0) + canvas.GetMarginLeft() + canvas.GetBorderLeft() + canvas.GetPaddingLeft(),
            (canvas.GetY() ?? 0) + canvas.GetMarginTop() + canvas.GetBorderTop() + canvas.GetPaddingTop(),
            canvas.GetWidth() ?? 0,
            canvas.GetHeight() ?? 0);

        try
        {
            using var ctx = new DrawContext(block, boundary, maxUnitId, resManager);
            drawer.Draw(ctx);
        }
        catch (IOException e)
        {
            throw new RenderException("Canvas绘制过程中发生异常", e);
        }
    }
}
