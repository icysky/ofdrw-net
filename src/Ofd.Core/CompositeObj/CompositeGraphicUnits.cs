using Ofd.Core.BasicStructure.Res;

namespace Ofd.Core.CompositeObj;

/// <summary>包含了一组矢量图像的描述（7.9 图 20 表 18）。</summary>
public class CompositeGraphicUnits : OFDElement, IOFDResource
{
    public CompositeGraphicUnits(Dom.DomElement proxy) : base(proxy) { }

    public CompositeGraphicUnits() : base("CompositeGraphicUnits") { }

    /// <summary>【必选】增加 矢量图像（必须有 ID；null 忽略）。</summary>
    public CompositeGraphicUnits AddCompositeGraphicUnit(CT_VectorG? vectorG)
    {
        if (vectorG == null) return this;
        if (vectorG.GetID() == null)
            throw new ArgumentException("矢量图像ID不能为空");
        Add(vectorG);
        return this;
    }

    /// <summary>【必选】获取 矢量图像列表。</summary>
    public List<CT_VectorG> GetCompositeGraphicUnits() => GetOFDElements("VectorG", e => new CT_VectorG(e));
}
