using Ofd.Core.BasicType;

namespace Ofd.Core.PageDescription.Color.ColorSpace;

/// <summary>
/// 颜色空间（8.3 图 24）：支持 GRAY/RGB/CMYK，可定义调色板或指向颜色配置文件。
/// </summary>
public class CT_ColorSpace : OFDElement
{
    public CT_ColorSpace(Dom.DomElement proxy) : base(proxy) { }

    public CT_ColorSpace() : base("ColorSpace") { }

    public CT_ColorSpace(OFDColorSpaceType type) : this()
    {
        SetType(type);
    }

    public CT_ColorSpace(OFDColorSpaceType type, long id) : this()
    {
        SetType(type).SetID(new ST_ID(id));
    }

    public ST_ID? GetID() => GetObjID();

    public CT_ColorSpace SetID(ST_ID id) => (CT_ColorSpace)SetObjID(id);

    /// <summary>【必选属性】设置 颜色空间的类型。</summary>
    public CT_ColorSpace SetType(OFDColorSpaceType type) => (CT_ColorSpace)AddAttribute("Type", type.ToString());

    /// <summary>【必选属性】获取 颜色空间的类型，未知抛异常。</summary>
    public OFDColorSpaceType GetType() => OFDColorSpaceTypeExtensions.GetInstance(AttributeValue("Type"));

    /// <summary>【可选属性】设置 每个颜色通道使用的位数（null 删除属性）。</summary>
    public CT_ColorSpace SetBitsPerComponent(BitsPerComponent? bitsPerComponent)
    {
        if (bitsPerComponent == null)
        {
            RemoveAttr("BitsPerComponent");
            return this;
        }
        return (CT_ColorSpace)AddAttribute("BitsPerComponent", bitsPerComponent.Value.ToXmlValue());
    }

    /// <summary>【可选属性】获取 每个颜色通道使用的位数，缺失/空返回 BIT_1（Java 上游行为）。</summary>
    public BitsPerComponent GetBitsPerComponent() => BitsPerComponentExtensions.GetInstance(AttributeValue("BitsPerComponent"));

    /// <summary>【可选属性】设置 指向包内颜色配置文件（null 删除属性）。</summary>
    public CT_ColorSpace SetProfile(ST_Loc? profile)
    {
        if (profile == null)
        {
            RemoveAttr("Profile");
            return this;
        }
        return (CT_ColorSpace)AddAttribute("Profile", profile.ToString());
    }

    /// <summary>【可选属性】获取 指向包内颜色配置文件，不存在 null。</summary>
    public ST_Loc? GetProfile() => ST_Loc.GetInstance(AttributeValue("Profile"));

    /// <summary>【可选】设置 调色板。</summary>
    public CT_ColorSpace SetPalette(Palette palette)
    {
        Set(palette);
        return this;
    }

    /// <summary>【可选】获取 调色板，不存在 null。</summary>
    public Palette? GetPalette()
    {
        var e = GetOFDElement("Palette");
        return e == null ? null : new Palette(e);
    }
}
