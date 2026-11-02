using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.PageObj;

/// <summary>页面内容描述（7.7 表 12）：该节点不存在表示空白页。</summary>
public class Content : OFDElement
{
    public Content(Dom.DomElement proxy) : base(proxy) { }

    public Content() : base("Content") { }

    /// <summary>【必选】增加 层节点（层必须设置 ID 属性）。</summary>
    public Content AddLayer(CT_Layer layer)
    {
        ST_ID? id = layer.GetObjID();
        if (id == null)
            throw new ArgumentException("加入的图层对象（CT_Layer）没有设置ID属性");
        Add(layer);
        return this;
    }

    /// <summary>【必选】获取 层节点列表。</summary>
    public List<CT_Layer> GetLayers() => GetOFDElements("Layer", e => new CT_Layer(e));

    /// <summary>
    /// 【必选】获取 按图层次序排序的层节点列表
    /// （背景层 1 → 正文层 3 → 前景层 5，同序保持出现顺序）。
    /// </summary>
    public List<CT_Layer> GetOrderedLayers() =>
        GetLayers().OrderBy(p => p.GetType().Order()).ToList();
}
