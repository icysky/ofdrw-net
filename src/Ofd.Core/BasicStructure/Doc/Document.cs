using Ofd.Core.BasicStructure.Doc.Bookmark;
using Ofd.Core.BasicStructure.Doc.Permission;
using Ofd.Core.BasicStructure.Doc.VPreferences;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
// "Outlines" 在外层命名空间链下会被 Ofd.Core.BasicStructure.Outlines 命名空间名遮蔽
using OutlinesType = global::Ofd.Core.BasicStructure.Outlines.Outlines;

namespace Ofd.Core.BasicStructure.Doc;

/// <summary>文档根节点 Document.xml（图 5）。</summary>
public class Document : OFDElement
{
    public Document(Dom.DomElement proxy) : base(proxy) { }

    public Document() : base("Document") { }

    /// <summary>【必选】设置 文档公共数据。</summary>
    public Document SetCommonData(CT_CommonData commonData)
    {
        Set(commonData);
        return this;
    }

    /// <summary>【必选】获取 文档公共数据，不存在 null。</summary>
    public CT_CommonData? GetCommonData()
    {
        var e = GetOFDElement("CommonData");
        return e == null ? null : new CT_CommonData(e);
    }

    /// <summary>【必选】设置 页树。</summary>
    public Document SetPages(Pages pages)
    {
        Set(pages);
        return this;
    }

    /// <summary>【必选】获取 页树，不存在 null。</summary>
    public Pages? GetPages()
    {
        var e = GetOFDElement("Pages");
        return e == null ? null : new Pages(e);
    }

    /// <summary>【可选】设置 大纲。</summary>
    public Document SetOutlines(OutlinesType outlines)
    {
        Set(outlines);
        return this;
    }

    /// <summary>【可选】获取 大纲，不存在 null。</summary>
    public OutlinesType? GetOutlines()
    {
        var e = GetOFDElement("Outlines");
        return e == null ? null : new OutlinesType(e);
    }

    /// <summary>【可选】设置 文档权限声明。</summary>
    public Document SetPermissions(CT_Permission permission)
    {
        Set(permission);
        return this;
    }

    /// <summary>【可选】获取 文档权限声明，不存在 null。</summary>
    public CT_Permission? GetPermission()
    {
        var e = GetOFDElement("Permissions");
        return e == null ? null : new CT_Permission(e);
    }

    /// <summary>【可选】设置 文档关联的动作序列。</summary>
    public Document SetActions(global::Ofd.Core.Action.Actions actions)
    {
        Set(actions);
        return this;
    }

    /// <summary>【可选】获取 文档关联的动作序列，不存在 null。</summary>
    public global::Ofd.Core.Action.Actions? GetActions()
    {
        var e = GetOFDElement("Actions");
        return e == null ? null : new global::Ofd.Core.Action.Actions(e);
    }

    /// <summary>【可选】设置 文档视图首选项。</summary>
    public Document SetVPreferences(CT_VPreferences vPreferences)
    {
        Set(vPreferences);
        return this;
    }

    /// <summary>【可选】获取 文档视图首选项，不存在 null。</summary>
    public CT_VPreferences? GetVPreferences()
    {
        var e = GetOFDElement("VPreferences");
        return e == null ? null : new CT_VPreferences(e);
    }

    /// <summary>【可选】设置 文档书签集。</summary>
    public Document SetBookmarks(Bookmarks bookmarks)
    {
        Set(bookmarks);
        return this;
    }

    /// <summary>【可选】获取 文档书签集，不存在 null。</summary>
    public Bookmarks? GetBookmarks()
    {
        var e = GetOFDElement("Bookmarks");
        return e == null ? null : new Bookmarks(e);
    }

    /// <summary>【可选】设置 指向注释列表的文件路径。</summary>
    public Document SetAnnotations(ST_Loc annotations)
    {
        SetOFDEntity("Annotations", annotations);
        return this;
    }

    /// <summary>【可选】获取 指向注释列表的文件路径，不存在 null。</summary>
    public ST_Loc? GetAnnotations() => ST_Loc.GetInstance(GetOFDElementText("Annotations"));

    /// <summary>【可选】设置 指向自定义标引列表文件路径。</summary>
    public Document SetCustomTags(ST_Loc customTags)
    {
        SetOFDEntity("CustomTags", customTags);
        return this;
    }

    /// <summary>【可选】获取 指向自定义标引列表文件路径，不存在 null。</summary>
    public ST_Loc? GetCustomTags() => ST_Loc.GetInstance(GetOFDElementText("CustomTags"));

    /// <summary>【可选】设置 指向附件列表文件路径。</summary>
    public Document SetAttachments(ST_Loc attachments)
    {
        SetOFDEntity("Attachments", attachments);
        return this;
    }

    /// <summary>【可选】获取 指向附件列表文件路径，不存在 null。</summary>
    public ST_Loc? GetAttachments() => ST_Loc.GetInstance(GetOFDElementText("Attachments"));

    /// <summary>【可选】设置 指向扩展列表文件路径。</summary>
    public Document SetExtensions(ST_Loc extensions)
    {
        SetOFDEntity("Extensions", extensions);
        return this;
    }

    /// <summary>【可选】获取 指向扩展列表文件路径，不存在 null。</summary>
    public ST_Loc? GetExtensions() => ST_Loc.GetInstance(GetOFDElementText("Extensions"));
}
