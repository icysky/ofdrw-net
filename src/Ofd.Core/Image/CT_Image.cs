using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription;

namespace Ofd.Core.Image;

/// <summary>图像（第 10 章 图 57 表 43）。</summary>
public class CT_Image : CT_GraphicUnit<CT_Image>
{
    public CT_Image(Dom.DomElement proxy) : base(proxy) { }

    public CT_Image() : base("Image") { }

    protected CT_Image(string name) : base(name) { }

    /// <summary>【必选属性】设置 引用资源文件中定义的多媒体标识。</summary>
    public CT_Image SetResourceID(ST_RefID resourceId) => (CT_Image)AddAttribute("ResourceID", resourceId.ToString());

    /// <summary>【必选属性】获取 多媒体标识，不存在 null。</summary>
    public ST_RefID? GetResourceID() => ST_RefID.GetInstance(AttributeValue("ResourceID"));

    /// <summary>【可选属性】设置 可替换图像标识（null 删除属性）。</summary>
    public CT_Image SetSubstitution(ST_RefID? substitution)
    {
        if (substitution == null)
        {
            RemoveAttr("Substitution");
            return this;
        }
        return (CT_Image)AddAttribute("Substitution", substitution.ToString());
    }

    /// <summary>【可选属性】获取 可替换图像标识，不存在 null。</summary>
    public ST_RefID? GetSubstitution() => ST_RefID.GetInstance(AttributeValue("Substitution"));

    /// <summary>【可选属性】设置 图像蒙版引用（null 删除属性）。</summary>
    public CT_Image SetImageMask(ST_RefID? imageMask)
    {
        if (imageMask == null)
        {
            RemoveAttr("ImageMask");
            return this;
        }
        return (CT_Image)AddAttribute("ImageMask", imageMask.ToString());
    }

    /// <summary>【可选属性】获取 图像蒙版引用，不存在 null。</summary>
    public ST_RefID? GetImageMask() => ST_RefID.GetInstance(AttributeValue("ImageMask"));

    /// <summary>【可选】设置 图像边框（null 忽略）。</summary>
    public CT_Image SetBorder(Border? border)
    {
        if (border == null) return this;
        Set(border);
        return this;
    }

    /// <summary>【可选】获取 图像边框，不存在 null。</summary>
    public Border? GetBorder()
    {
        var e = GetOFDElement("Border");
        return e == null ? null : new Border(e);
    }

    /// <summary>构造图片对象（改名 ImageObject 并设置 ID）。</summary>
    public ImageObject ToObj(ST_ID id)
    {
        SetOFDName("ImageObject");
        SetObjID(id);
        return new ImageObject(Proxy);
    }
}
