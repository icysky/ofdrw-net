using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.Res;

/// <summary>多媒体（7.9 图 21 表 19）。</summary>
public class CT_MultiMedia : OFDElement
{
    public CT_MultiMedia(Dom.DomElement proxy) : base(proxy) { }

    public CT_MultiMedia() : base("MultiMedia") { }

    public ST_ID? GetID() => GetObjID();

    public CT_MultiMedia SetID(ST_ID id) => (CT_MultiMedia)SetObjID(id);

    /// <summary>【必选属性】设置 多媒体类型（null 抛异常）。</summary>
    public CT_MultiMedia SetType(MediaType type) => (CT_MultiMedia)AddAttribute("Type", type.ToString());

    /// <summary>【必选属性】获取 多媒体类型，未知抛异常。</summary>
    public MediaType GetType() => MediaTypeExtensions.GetInstance(AttributeValue("Type"));

    /// <summary>【可选属性】设置 资源格式（BMP/JPEG/PNG/TIFF/AVS 等；null 删除属性）。</summary>
    public CT_MultiMedia SetFormat(string? format)
    {
        if (format == null)
        {
            RemoveAttr("Format");
            return this;
        }
        return (CT_MultiMedia)AddAttribute("Format", format);
    }

    /// <summary>【可选属性】获取 资源格式，不存在 null。</summary>
    public string? GetFormat() => AttributeValue("Format");

    /// <summary>【必选】设置 指向包内多媒体文件位置（先清空子内容）。</summary>
    public CT_MultiMedia SetMediaFile(ST_Loc mediaFile)
    {
        RemoveAll();
        AddOFDEntity("MediaFile", mediaFile);
        return this;
    }

    /// <summary>【必选】获取 指向包内多媒体文件位置，不存在 null。</summary>
    public ST_Loc? GetMediaFile()
    {
        var e = GetOFDElement("MediaFile");
        return e == null ? null : ST_Loc.GetInstance(ElementText(e).Trim());
    }
}
