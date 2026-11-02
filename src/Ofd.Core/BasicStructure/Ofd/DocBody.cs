using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.Ofd;

/// <summary>文件对象入口，可以存在多个，以便在一个文档中包含多个版式文档。</summary>
public class DocBody : OFDElement
{
    /// <summary>文档根节点文档名称。</summary>
    public const string DOC_ROOT = "DocRoot";

    public DocBody(Dom.DomElement proxy) : base(proxy) { }

    public DocBody() : base("DocBody") { }

    /// <summary>设置文档元数据信息描述。</summary>
    public DocBody SetDocInfo(BasicStructure.Ofd.DocInfo.CT_DocInfo ctDocInfo)
    {
        Set(ctDocInfo);
        return this;
    }

    /// <summary>获取文档元数据信息描述，null 表示不存在。</summary>
    public BasicStructure.Ofd.DocInfo.CT_DocInfo? GetDocInfo()
    {
        var e = GetOFDElement("DocInfo");
        return e == null ? null : new BasicStructure.Ofd.DocInfo.CT_DocInfo(e);
    }

    /// <summary>设置指向文档根节点文档。</summary>
    public DocBody SetDocRoot(ST_Loc docRoot)
    {
        Set(docRoot.GetElement(DOC_ROOT));
        return this;
    }

    /// <summary>获取指向文档根节点文档路径，null 表示不存在。</summary>
    public ST_Loc? GetDocRoot()
    {
        var locStr = GetOFDElementText("DocRoot");
        if (locStr == null || locStr.Trim().Length == 0)
            return null;
        return new ST_Loc(locStr);
    }

    /// <summary>设置版本序列（第 19 章）。</summary>
    public DocBody SetVersions(global::Ofd.Core.Versions.Versions versions)
    {
        Set(versions);
        return this;
    }

    /// <summary>获取版本描述序列，null 表示不存在。</summary>
    public global::Ofd.Core.Versions.Versions? GetVersions()
    {
        var e = GetOFDElement("Versions");
        return e == null ? null : new global::Ofd.Core.Versions.Versions(e);
    }

    /// <summary>设置指向该文档中签名和签章结构的路径（第 18 章）。</summary>
    public DocBody SetSignatures(ST_Loc signatures)
    {
        SetOFDEntity("Signatures", signatures.ToString());
        return this;
    }

    /// <summary>获取指向该文档中签名和签章结构的路径，null 表示不存在。</summary>
    public ST_Loc? GetSignatures()
    {
        var locStr = GetOFDElementText("Signatures");
        if (locStr == null || locStr.Trim().Length == 0)
            return null;
        return new ST_Loc(locStr);
    }
}
