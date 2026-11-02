using Ofd.Core.BasicType;

using Ofd.Core;
using Ofd.Core.Dom;

namespace Ofd.Layout.AreaHolder;

/// <summary>
/// 【OFDRW 扩展】区域占位区块：记录预置绘制区域的位置、页面与页块引用。
/// </summary>
public class CT_AreaHolderBlock : OFDElement
{
    public CT_AreaHolderBlock(Ofd.Core.Dom.DomElement proxy) : base(proxy) { }

    public CT_AreaHolderBlock() : base("AreaHolderBlock") { }

    public CT_AreaHolderBlock(string areaName) : this() => SetAreaName(areaName);

    /// <summary>【必选属性】设置区域名称。</summary>
    public CT_AreaHolderBlock SetAreaName(string? areaName)
    {
        if (areaName == null) throw new ArgumentException("区域名称（AreaName）不能为空");
        return (CT_AreaHolderBlock)AddAttribute("AreaName", areaName);
    }

    /// <summary>【必选属性】获取区域名称。</summary>
    public string? GetAreaName() => AttributeValue("AreaName");

    /// <summary>【必选属性】设置页块引用（通过该 ID 在页面中定位容器元素）。</summary>
    public CT_AreaHolderBlock SetPageBlockID(ST_RefID? pageBlockID)
    {
        if (pageBlockID == null) throw new ArgumentException("页块引用（pageBlockID）不能为空");
        return (CT_AreaHolderBlock)AddAttribute("PageBlockID", pageBlockID.ToString());
    }

    /// <summary>【必选属性】获取页块引用。</summary>
    public ST_RefID? GetPageBlockID() => ST_RefID.GetInstance(AttributeValue("PageBlockID"));

    /// <summary>【必选属性】设置区域外接矩形。</summary>
    public CT_AreaHolderBlock SetBoundary(ST_Box? boundary)
    {
        if (boundary == null) throw new ArgumentException("区域外接矩形（boundary）为空");
        return (CT_AreaHolderBlock)AddAttribute("Boundary", boundary.ToString());
    }

    /// <summary>【必选属性】获取区域外接矩形。</summary>
    public ST_Box? GetBoundary() => ST_Box.GetInstance(AttributeValue("Boundary"));

    /// <summary>【必选元素】设置页面文件（所在页面的容器绝对路径）。</summary>
    public CT_AreaHolderBlock SetPageFile(ST_Loc? pageLoc)
    {
        if (pageLoc == null || pageLoc.IsEmpty()) throw new ArgumentException("页面对象路径（pageLoc）为空");
        return (CT_AreaHolderBlock)SetOFDEntity("PageFile", pageLoc.ToString());
    }

    /// <summary>【必选元素】获取页面文件路径。</summary>
    public ST_Loc? GetPageFile() => ST_Loc.GetInstance(GetOFDElementText("PageFile"));
}
