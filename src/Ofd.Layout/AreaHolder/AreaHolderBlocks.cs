using Ofd.Core;
using Ofd.Core.Dom;

namespace Ofd.Layout.AreaHolder;

/// <summary>
/// 【OFDRW 扩展】区域占位区块列表（AreaHolderBlocks.xml），通常位于 <c>/Doc_0/AreaHolderBlocks.xml</c>。
/// </summary>
public class AreaHolderBlocks : OFDElement
{
    /// <summary>区域占位区块列表文件名。</summary>
    public const string AreaHolderBlocksFile = "AreaHolderBlocks.xml";

    public AreaHolderBlocks(Ofd.Core.Dom.DomElement proxy) : base(proxy) { }

    public AreaHolderBlocks() : base("AreaHolderBlocks") { }

    /// <summary>增加区域占位区块。</summary>
    public AreaHolderBlocks AddAreaHolderBlock(params CT_AreaHolderBlock[] area)
    {
        if (area == null || area.Length == 0) return this;

        foreach (var block in area) Add(block);
        return this;
    }

    /// <summary>获取区域占位区块序列。</summary>
    public List<CT_AreaHolderBlock> GetAreaHolderBlocks() =>
        GetOFDElements("AreaHolderBlock", e => new CT_AreaHolderBlock(e));
}
