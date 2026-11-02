using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Layout.Element;

namespace Ofd.Layout.Engine.Render;

/// <summary>
/// 图片渲染器（Java ImgRender）：登记图片资源并输出 ImageObject（Boundary + CTM）。
/// </summary>
public class ImgRender : IProcessor
{
    public void Render(ST_Loc pageLoc, CT_PageBlock layer, ResManager resManager, Div e, AtomicCounter maxUnitId)
    {
        if (e is Img img)
            Render(layer, resManager, img, maxUnitId);
    }

    /// <summary>渲染图片对象。</summary>
    public static void Render(CT_PageBlock layer, ResManager resManager, Img? e, AtomicCounter maxUnitId)
    {
        if (e == null) return;

        string? src = e.GetSrc();
        if (src == null || !File.Exists(src))
            throw new ArgumentException("图片对象(Img)路径非法");

        ST_ID id;
        try
        {
            id = resManager.AddImage(src)!;
        }
        catch (IOException ex)
        {
            throw new RenderException("渲染图片复制失败：" + ex.Message, ex);
        }

        var imgObj = new ImageObject(maxUnitId.IncrementAndGet());
        imgObj.SetResourceID(new ST_RefID(id));

        double x = (e.GetX() ?? 0) + e.GetMarginLeft() + e.GetBorderLeft() + e.GetPaddingLeft();
        double y = (e.GetY() ?? 0) + e.GetMarginTop() + e.GetBorderTop() + e.GetPaddingTop();
        double w = e.GetWidth() ?? 0;
        double h = e.GetHeight() ?? 0;

        imgObj.SetBoundary(x, y, w, h);
        imgObj.SetCTM(new ST_Array(w, 0, 0, h, 0, 0));

        if (e.GetOpacity() is { } opacity) imgObj.SetAlpha((int)(opacity * 255));

        layer.AddPageBlock(imgObj);
    }
}
