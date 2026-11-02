using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Layout.AreaHolder;
using Ofd.Layout.Element;
using Ofd.Pkg.Container;

namespace Ofd.Layout.Engine.Render;

/// <summary>
/// 区域占位区块渲染器（Java AreaHolderBlockRender）：页面内加占位页块，并在
/// <c>AreaHolderBlocks.xml</c> 登记区域（名称/边界/页面/页块引用）。
/// </summary>
public class AreaHolderBlockRender : IProcessor
{
    public void Render(ST_Loc pageLoc, CT_PageBlock layer, ResManager resManager, Div e, AtomicCounter maxUnitId)
    {
        if (e is AreaHolderBlock block)
            Render(resManager.GetDocDir(), pageLoc, layer, block, maxUnitId);
    }

    /// <summary>渲染区域占位区块。</summary>
    public static void Render(DocDir docDir, ST_Loc pageLoc, CT_PageBlock layer, AreaHolderBlock ahBlock,
        AtomicCounter maxUnitId)
    {
        // 页面内占位页块
        var block = new CT_PageBlock();
        var objId = new ST_ID(maxUnitId.IncrementAndGet());
        block.SetObjID(objId);
        layer.AddPageBlock(block);

        try
        {
            double[] border = ahBlock.GetBorder();
            double[] padding = ahBlock.GetPadding();

            var blocks = AreaHolderBlocksProcess.Obtain(docDir);

            // 与 Java 一致：使用 border[0]/padding[0]、border[1]/padding[1] 作为 X/Y 偏移
            var boundary = new ST_Box(
                (ahBlock.GetX() ?? 0) + border[0] + padding[0],
                (ahBlock.GetY() ?? 0) + border[1] + padding[1],
                ahBlock.GetWidth() ?? 0,
                ahBlock.GetHeight() ?? 0);

            var obj = new CT_AreaHolderBlock(ahBlock.GetAreaName() ?? "")
                .SetBoundary(boundary)
                .SetPageFile(pageLoc)
                .SetPageBlockID(objId.Ref());
            obj.SetObjID(maxUnitId.IncrementAndGet());

            blocks.AddAreaHolderBlock(obj);
        }
        catch (Exception e)
        {
            throw new RenderException("区域占位区块列表文件获取失败 ", e);
        }
    }
}
