using Ofd.Core.BasicType;

namespace Ofd.Core.Signatures.Appearance;

/// <summary>签名的外观（18.2.3 图 88 表 69）：一个签名可关联 0~多个外观。</summary>
public class StampAnnot : OFDElement
{
    public StampAnnot(Dom.DomElement proxy) : base(proxy) { }

    public StampAnnot() : base("StampAnnot") { }

    /// <summary>【必选属性】设置 签章注释标识（推荐 "sNNN"；空白抛异常）。</summary>
    public StampAnnot SetId(string id)
    {
        if (id == null || id.Trim().Length == 0) throw new ArgumentException("签章注释的标识（ID）为空");
        return (StampAnnot)AddAttribute("ID", id);
    }

    /// <summary>【必选属性】获取 签章注释标识，缺失抛异常。</summary>
    public string GetId()
    {
        var str = AttributeValue("ID");
        if (str == null || str.Trim().Length == 0) throw new InvalidOperationException("签章注释的标识（ID）为空");
        return str;
    }

    /// <summary>【必选属性】设置 引用外观注释所在页面标识符（null 抛异常）。</summary>
    public StampAnnot SetPageRef(ST_RefID pageRef)
    {
        if (pageRef == null) throw new ArgumentException("引用外观注释所在的页面的标识符（PageRef）为空");
        return (StampAnnot)AddAttribute("PageRef", pageRef.ToString());
    }

    /// <summary>【必选属性】获取 引用页面标识符，缺失抛异常。</summary>
    public ST_RefID GetPageRef()
    {
        var str = AttributeValue("PageRef");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("引用外观注释所在的页面的标识符（PageRef）为空");
        return ST_RefID.GetInstance(str)!;
    }

    /// <summary>【必选属性】设置 外观边框位置（null 抛异常）。</summary>
    public StampAnnot SetBoundary(ST_Box boundary)
    {
        if (boundary == null) throw new ArgumentException("签章注释的外观边框位置（Boundary）为空");
        return (StampAnnot)AddAttribute("Boundary", boundary.ToString());
    }

    /// <summary>【必选属性】获取 外观边框位置，不存在 null。</summary>
    public ST_Box? GetBoundary() => ST_Box.GetInstance(AttributeValue("Boundary"));

    /// <summary>【可选属性】设置 外观裁剪设置（null 删除属性）。</summary>
    public StampAnnot SetClip(ST_Box? clip)
    {
        if (clip == null)
        {
            RemoveAttr("Clip");
            return this;
        }
        return (StampAnnot)AddAttribute("Clip", clip.ToString());
    }

    /// <summary>【可选属性】获取 外观裁剪设置，不存在 null。</summary>
    public ST_Box? GetClip() => ST_Box.GetInstance(AttributeValue("Clip"));
}
