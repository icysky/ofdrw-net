using Ofd.Core.BasicType;

namespace Ofd.Core.Signatures;

/// <summary>数字签名或安全签章在列表中的注册信息（18.1 图 85 表 66）。</summary>
public class Signature : OFDElement
{
    public Signature(Dom.DomElement proxy) : base(proxy) { }

    public Signature() : base("Signature") { }

    /// <summary>【必选属性】设置 签名或签章标识（推荐 "sNNN"；空白抛异常）。</summary>
    public Signature SetId(string id)
    {
        if (id == null || id.Trim().Length == 0) throw new ArgumentException("签名或签章的标识（ID）为空");
        return (Signature)AddAttribute("ID", id);
    }

    /// <summary>【必选属性】获取 签名或签章标识，缺失抛异常。</summary>
    public string GetId()
    {
        var str = AttributeValue("ID");
        if (str == null || str.Trim().Length == 0) throw new InvalidOperationException("签名或签章的标识（ID）为空");
        return str;
    }

    /// <summary>【可选属性】设置 签名节点类型（null 删除属性，缺省 Seal）。</summary>
    public Signature SetType(SigType? type)
    {
        if (type == null)
        {
            RemoveAttr("Type");
            return this;
        }
        return (Signature)AddAttribute("Type", type.ToString());
    }

    /// <summary>【可选属性】获取 签名节点类型，缺省 Seal。</summary>
    public SigType GetType() => SigTypeExtensions.GetInstance(AttributeValue("Type"));

    /// <summary>【可选属性 OFD 2.0】设置 基于的签名 ID（null 删除属性）。</summary>
    public Signature SetRelative(string? id)
    {
        if (id == null)
        {
            RemoveAttr("Relative");
            return this;
        }
        return (Signature)AddAttribute("Relative", id);
    }

    /// <summary>【可选属性 OFD 2.0】获取 基于的签名 ID，不存在 null。</summary>
    public string? GetRelative() => AttributeValue("Relative");

    /// <summary>【必选属性】设置 指向包内的签名描述文件路径（null 抛异常）。</summary>
    public Signature SetBaseLoc(ST_Loc baseLoc)
    {
        if (baseLoc == null) throw new ArgumentException("指向包内的签名描述文件（BaseLoc）为空");
        return (Signature)AddAttribute("BaseLoc", baseLoc.ToString());
    }

    /// <summary>【必选属性】获取 指向包内的签名描述文件路径，缺失抛异常。</summary>
    public ST_Loc GetBaseLoc()
    {
        var str = AttributeValue("BaseLoc");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("指向包内的签名描述文件（BaseLoc）为空");
        return ST_Loc.GetInstance(str)!;
    }
}
