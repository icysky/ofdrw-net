using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;
using Ofd.Core.BasicType;
using Ofd.Layout.Engine;

namespace Ofd.Layout.Edit;

/// <summary>
/// 专用于向已有页面追加内容的虚拟页面（Java AdditionVPage）。
/// </summary>
public class AdditionVPage : VirtualPage
{
    /// <summary>反序列化后的页面对象。</summary>
    private readonly PageObj _pageObj;

    /// <summary>页面对象在容器中的绝对路径（以 / 开头，可能为 null）。</summary>
    private ST_Loc? _pageLoc;

    /// <summary>创建追加内容的虚拟页面。</summary>
    public AdditionVPage(PageObj pageObj, ST_Loc? pageLoc)
    {
        _pageObj = pageObj ?? throw new ArgumentException("页面对象（pageObj）不能为空");
        _pageLoc = pageLoc;
    }

    /// <summary>
    /// 获取处于页面最上层的图层；页面无任何图层时创建一个。
    /// </summary>
    public CT_Layer? ObtainTopLayer(AtomicCounter maxUnitId)
    {
        CT_Layer? topLayer = null;
        int currentOrder = LayerType.Background.Order(); // 默认最底层

        var content = _pageObj.GetContent();
        if (content == null)
        {
            topLayer = new CT_Layer();
            topLayer.SetObjID(maxUnitId.IncrementAndGet());
            content = new Content().AddLayer(topLayer);
            _pageObj.SetContent(content);
            return topLayer;
        }

        var layers = content.GetLayers();
        foreach (var layer in layers)
        {
            if (layer == null) continue;
            if (layer.GetType().Order() >= currentOrder) topLayer = layer;
        }
        return topLayer;
    }

    /// <summary>创建新图层并加入页面。</summary>
    public CT_Layer? NewLayer(AtomicCounter maxUnitId)
    {
        var content = _pageObj.GetContent();
        if (content == null)
        {
            _pageObj.SetContent(new Content());
            return null; // 与 Java 一致：此分支返回 null
        }

        var layer = new CT_Layer();
        layer.SetObjID(maxUnitId.IncrementAndGet());
        content.AddLayer(layer);
        return layer;
    }

    /// <summary>获取页面对象。</summary>
    public PageObj GetPageObj() => _pageObj;

    /// <summary>获取页面绝对路径（可能为空）。</summary>
    public ST_Loc? GetPageLoc() => _pageLoc;

    /// <summary>设置页面绝对路径。</summary>
    public void SetPageLoc(ST_Loc? pageLoc) => _pageLoc = pageLoc;
}
