using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.Ofd.DocInfo;

/// <summary>
/// 文档元数据信息描述（7.4 图 12 表 4，Java CT_DocInfo）。
/// </summary>
public class CT_DocInfo : OFDElement
{
    public CT_DocInfo(Dom.DomElement proxy) : base(proxy) { }

    public CT_DocInfo() : base("DocInfo")
    {
        SetTitle("??")
            .SetAuthor("ofdrw")
            .SetCreator("ofdrw")
            .SetCreatorVersion("1.0.0-SNAPSHOT");
    }

    /// <summary>设置 文档标题。</summary>
    public CT_DocInfo SetTitle(string? title) => (CT_DocInfo)SetOFDEntity("Title", title);

    /// <summary>获取 文档标题。</summary>
    public string? GetTitle() => NonBlank(GetOFDElementText("Title"));

    /// <summary>设置 文档作者。</summary>
    public CT_DocInfo SetAuthor(string? author) => (CT_DocInfo)SetOFDEntity("Author", author);

    /// <summary>获取 文档作者。</summary>
    public string? GetAuthor() => NonBlank(GetOFDElementText("Author"));

    /// <summary>设置 文档主题（Java setTile/setSubject：Title 由 tile 名保留上游拼写）。</summary>
    public CT_DocInfo SetSubject(string? subject) => (CT_DocInfo)SetOFDEntity("Subject", subject);

    /// <summary>获取 文档主题。</summary>
    public string? GetSubject() => NonBlank(GetOFDElementText("Subject"));

    /// <summary>设置 文档摘要。</summary>
    public CT_DocInfo SetAbstract(string? str) => (CT_DocInfo)SetOFDEntity("Abstract", str);

    /// <summary>获取 文档摘要。</summary>
    public string? GetAbstract() => NonBlank(GetOFDElementText("Abstract"));

    /// <summary>设置 创建应用程序。</summary>
    public CT_DocInfo SetCreator(string? creator) => (CT_DocInfo)SetOFDEntity("Creator", creator);

    /// <summary>获取 创建应用程序。</summary>
    public string? GetCreator() => NonBlank(GetOFDElementText("Creator"));

    /// <summary>设置 创建应用程序版本。</summary>
    public CT_DocInfo SetCreatorVersion(string? version) => (CT_DocInfo)SetOFDEntity("CreatorVersion", version);

    /// <summary>获取 创建应用程序版本。</summary>
    public string? GetCreatorVersion() => NonBlank(GetOFDElementText("CreatorVersion"));

    /// <summary>设置 创建时间（ISO yyyy-MM-dd；null 删除）。</summary>
    public CT_DocInfo SetCreationDate(DateOnly? creationDate)
    {
        if (creationDate == null)
        {
            RemoveOFDElemByNames("CreationDate");
            return this;
        }
        return (CT_DocInfo)SetOFDEntity("CreationDate", creationDate.Value.ToString(Const.DATE_FORMAT));
    }

    /// <summary>获取 创建时间，null 表示不存在。</summary>
    public DateOnly? GetCreationDate() => ParseDate(GetOFDElementText("CreationDate"));

    /// <summary>设置 修改时间（ISO yyyy-MM-dd；null 删除）。</summary>
    public CT_DocInfo SetModDate(DateOnly? modDate)
    {
        if (modDate == null)
        {
            RemoveOFDElemByNames("ModDate");
            return this;
        }
        return (CT_DocInfo)SetOFDEntity("ModDate", modDate.Value.ToString(Const.DATE_FORMAT));
    }

    /// <summary>获取 修改时间，null 表示不存在。</summary>
    public DateOnly? GetModDate() => ParseDate(GetOFDElementText("ModDate"));

    /// <summary>设置 文档标识符。</summary>
    public CT_DocInfo SetDocID(string docID) => (CT_DocInfo)SetOFDEntity("DocID", docID);

    /// <summary>设置 文档标识符（去连字符 32 位十六进制，同 Java UUID 格式）。</summary>
    public CT_DocInfo SetDocID(Guid docID) => SetDocID(docID.ToString("N"));

    /// <summary>获取 文档标识符，null 表示不存在。</summary>
    public string? GetDocID() => NonBlank(GetOFDElementText("DocID"));

    /// <summary>设置 关键词序列（null 删除元素）。</summary>
    public CT_DocInfo SetKeywords(List<string>? keywords)
    {
        RemoveOFDElemByNames("Keywords");
        if (keywords == null) return this;
        var keyList = new Keywords();
        foreach (var kw in keywords)
            keyList.AddKeyword(kw);
        Add(keyList);
        return this;
    }

    /// <summary>增加关键词。</summary>
    public CT_DocInfo AddKeyword(string keyword)
    {
        var keywords = GetOFDElement("Keywords");
        if (keywords == null)
        {
            var keyList = new Keywords();
            keyList.AddKeyword(keyword);
            Add(keyList);
        }
        else
        {
            new Keywords(keywords).AddKeyword(keyword);
        }
        return this;
    }

    /// <summary>获取关键词列表。</summary>
    public List<string> GetKeywords()
    {
        var keywords = GetOFDElement("Keywords");
        return keywords == null ? new List<string>() : new Keywords(keywords).GetKeywords();
    }

    /// <summary>设置 自定义元数据序列。</summary>
    public CT_DocInfo SetCustomDatas(CustomDatas? customDatas)
    {
        if (customDatas == null)
        {
            RemoveOFDElemByNames("CustomDatas");
            return this;
        }
        return (CT_DocInfo)Set(customDatas);
    }

    /// <summary>获取 自定义元数据序列。</summary>
    public CustomDatas? GetCustomDatas()
    {
        var e = GetOFDElement("CustomDatas");
        return e == null ? null : new CustomDatas(e);
    }

    /// <summary>设置 文档分类（枚举）。</summary>
    public CT_DocInfo SetDocUsage(DocUsage usage) => SetDocUsage(usage.ToString());

    /// <summary>设置 文档分类（字符串，无校验，与 Java 一致）。</summary>
    public CT_DocInfo SetDocUsage(string usage) => (CT_DocInfo)SetOFDEntity("DocUsage", usage);

    /// <summary>获取 文档分类，null 表示不存在。</summary>
    public DocUsage? GetDocUsage()
    {
        var str = GetOFDElementText("DocUsage");
        return str == null ? null : DocUsageExtensions.GetInstance(str);
    }

    /// <summary>设置 文档封面路径。</summary>
    public CT_DocInfo SetCover(ST_Loc cover)
    {
        Set(cover.GetElement("Cover"));
        return this;
    }

    /// <summary>获取 文档封面路径，null 表示不存在。</summary>
    public ST_Loc? GetCover()
    {
        var locStr = GetOFDElementText("Cover");
        if (locStr == null || locStr.Trim().Length == 0)
            return null;
        return ST_Loc.GetInstance(locStr.Trim());
    }

    private static string? NonBlank(string? s) =>
        s == null || s.Trim().Length == 0 ? null : s;

    private static DateOnly? ParseDate(string? s) =>
        s == null ? null : DateOnly.ParseExact(s.Trim(), Const.DATE_FORMAT);
}
