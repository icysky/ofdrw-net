namespace Ofd.Core.Signatures.Range;

/// <summary>签名的范围（18.2.2 图 87 表 68）。</summary>
public class References : OFDElement
{
    public References(Dom.DomElement proxy) : base(proxy) { }

    public References() : base("References") { }

    /// <summary>【可选属性】设置 摘要方法（空白删除属性）。</summary>
    public References SetCheckMethod(string? checkMethod)
    {
        if (checkMethod == null || checkMethod.Trim().Length == 0)
        {
            RemoveAttr("CheckMethod");
            return this;
        }
        return (References)AddAttribute("CheckMethod", checkMethod);
    }

    /// <summary>【可选属性】设置 摘要方法（枚举便捷重载）。</summary>
    public References SetCheckMethod(CheckMethod checkMethod) => SetCheckMethod(checkMethod.ToString());

    /// <summary>【可选属性】获取 摘要方法，不存在 null。</summary>
    public string? GetCheckMethod() => AttributeValue("CheckMethod");

    /// <summary>【必选】增加 针对一个文件的摘要节点（null 忽略）。</summary>
    public References AddReference(Reference? reference)
    {
        if (reference == null) return this;
        Add(reference);
        return this;
    }

    /// <summary>检查是否包含指定绝对路径的文件。</summary>
    public bool HasFile(string? absLoc)
    {
        if (absLoc == null || absLoc.Trim().Length == 0) return false;
        foreach (var refItem in GetReferences())
            if (absLoc == refItem.GetFileRef()?.Loc)
                return true;
        return false;
    }

    /// <summary>【必选】获取 针对一个文件的摘要节点列表。</summary>
    public List<Reference> GetReferences() => GetOFDElements("Reference", e => new Reference(e));
}
