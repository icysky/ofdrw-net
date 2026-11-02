using Ofd.Core.BasicType;

namespace Ofd.Core.Annotation.PageAnnot;

/// <summary>注释所在页（15.1 图 80 表 60）。</summary>
public class AnnPage : OFDElement
{
    public AnnPage(Dom.DomElement proxy) : base(proxy) { }

    public AnnPage() : base("Page") { }

    /// <summary>【必选属性】设置 引用注释所在页面的标识（null 抛异常）。</summary>
    public AnnPage SetPageID(ST_RefID pageId)
    {
        if (pageId == null) throw new ArgumentException("引用注释所在页面的标识（PageID）不能为空");
        return (AnnPage)AddAttribute("PageID", pageId.ToString());
    }

    /// <summary>【必选属性】设置 引用注释所在页面的标识（ST_ID 形式）。</summary>
    public AnnPage SetPageID(ST_ID pageId) => SetPageID(pageId.Ref());

    /// <summary>【必选属性】获取 引用注释所在页面的标识，不存在 null。</summary>
    public ST_RefID? GetPageID() => ST_RefID.GetInstance(AttributeValue("PageID"));

    /// <summary>【必选】设置 指向包内的分页注释文件（null 抛异常）。</summary>
    public AnnPage SetFileLoc(ST_Loc fileLoc) => (AnnPage)AddOFDEntity("FileLoc", fileLoc);

    /// <summary>【必选】获取 指向包内的分页注释文件，不存在 null。</summary>
    public ST_Loc? GetFileLoc()
    {
        var e = GetOFDElement("FileLoc");
        return e == null ? null : ST_Loc.GetInstance(ElementText(e).Trim());
    }
}
