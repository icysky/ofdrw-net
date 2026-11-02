using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.Dom;
using Ofd.Layout.Edit;
using Ofd.Layout.Element.Canvas;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Layout.AreaHolder;

/// <summary>
/// 区域占位区块上下文（Java AreaHolderContext）：按区域名称取回占位画布/单元格并自动加入文档。
/// </summary>
public class AreaHolderContext
{
    private readonly OFDDoc _doc;
    private readonly DocDir _docDir;
    private readonly ResourceLocator _rl;

    /// <summary>按文档序号创建上下文。</summary>
    public AreaHolderContext(OFDDoc doc, int n)
    {
        _rl = new ResourceLocator(doc.GetOfdDir());
        _doc = doc;
        _docDir = doc.GetOfdDir().GetDocByIndex(n);
    }

    /// <summary>按默认文档创建上下文。</summary>
    public AreaHolderContext(OFDDoc doc)
    {
        _rl = new ResourceLocator(doc.GetOfdDir());
        _doc = doc;
        _docDir = doc.GetOfdDir().ObtainDocDefault();
    }

    /// <summary>获取指定区域的单元格（区域不存在返回 null）。</summary>
    public CellContentDrawer? GetCell(string? areaName)
    {
        if (areaName == null) return null;

        var canvas = Get(areaName);
        return canvas == null ? null : new CellContentDrawer(canvas);
    }

    /// <summary>
    /// 获取指定区域的占位画布（区域不存在返回 null）。
    /// <para>画布的图层由区域占位区块决定，不可更改；画布已自动加入文档。</para>
    /// </summary>
    public Canvas? Get(string? areaName)
    {
        if (areaName == null) return null;
        if (!AreaHolderBlocksProcess.Exist(_docDir)) return null;

        AreaHolderBlocks blocks;
        try
        {
            blocks = AreaHolderBlocksProcess.Get(_docDir);
        }
        catch (Exception)
        {
            throw new ArgumentException("区域占位区块列表文件获取失败 ");
        }

        var holder = AreaHolderBlocksProcess.Find(blocks, areaName);
        if (holder == null) return null;

        var blockId = holder.GetPageBlockID();
        var pageLoc = holder.GetPageFile();
        var boundary = holder.GetBoundary();

        if (blockId == null || pageLoc == null || boundary == null) return null;
        if (boundary.Height <= 0 || boundary.Width <= 0) return null; // 无效绘制区域

        PageObj? page;
        try
        {
            page = _rl.Get(pageLoc, e => new PageObj(e));
        }
        catch (Exception)
        {
            return null;
        }
        if (page == null) return null;

        // 通过 ID 在页面中定位容器元素
        var element = FindById(page.GetProxy(), blockId.ToString());
        if (element == null) return null;

        var block = new CT_PageBlock(element);

        // 创建绘制器并绑定占位页块
        var canvas = new Canvas(boundary.TopLeftX, boundary.TopLeftY, boundary.Width, boundary.Height);
        canvas.SetPreferBlock(block);

        // 追加到页面并自动加入文档
        var virtualPage = new AdditionVPage(page, pageLoc);
        virtualPage.Add(canvas);
        _doc.AddVPage(virtualPage);

        return canvas;
    }

    /// <summary>在 DOM 树中按 ID 属性查找元素（深度优先）。</summary>
    private static DomElement? FindById(DomNode? root, string id)
    {
        if (root is not DomElement el) return null;
        if (el.Attr("ID") == id) return el;

        foreach (var kid in el.Kids)
        {
            var found = FindById(kid, id);
            if (found != null) return found;
        }
        return null;
    }
}
