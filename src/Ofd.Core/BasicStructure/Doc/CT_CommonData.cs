using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.Doc;

/// <summary>文档公共数据结构（图 6）：页面区域、公共资源、模板页等。</summary>
public class CT_CommonData : OFDElement
{
    public CT_CommonData(Dom.DomElement proxy) : base(proxy) { }

    public CT_CommonData() : base("CommonData") { }

    /// <summary>【必选】设置 文档中所有对象使用标识的最大值（初始 0）。</summary>
    public CT_CommonData SetMaxUnitID(ST_ID maxUnitID)
    {
        SetOFDEntity("MaxUnitID", maxUnitID);
        return this;
    }

    public CT_CommonData SetMaxUnitID(long maxUnitID) => SetMaxUnitID(new ST_ID(maxUnitID));

    /// <summary>【必选】获取 对象标识最大值，不存在 null。</summary>
    public ST_ID? GetMaxUnitID() => ST_ID.GetInstance(GetOFDElementText("MaxUnitID"));

    /// <summary>【必选】设置 页面区域默认大小和位置。</summary>
    public CT_CommonData SetPageArea(CT_PageArea pageArea)
    {
        Set(pageArea);
        return this;
    }

    /// <summary>【必选】获取 页面区域默认大小和位置，不存在 null。</summary>
    public CT_PageArea? GetPageArea()
    {
        var e = GetOFDElement("PageArea");
        return e == null ? null : new CT_PageArea(e);
    }

    /// <summary>【可选】设置 公共资源序列路径（已存在则替换）。</summary>
    public CT_CommonData SetPublicRes(ST_Loc publicRes)
    {
        SetOFDEntity("PublicRes", publicRes);
        return this;
    }

    /// <summary>【可选】获取 公共资源序列（列表中的第一个），不存在 null。</summary>
    public ST_Loc? GetPublicRes() => ST_Loc.GetInstance(GetOFDElementText("PublicRes"));

    /// <summary>【可选】添加 公共资源序列路径（null 或空路径忽略）。</summary>
    public CT_CommonData AddPublicRes(ST_Loc? publicRes)
    {
        if (publicRes?.Loc == null) return this;
        AddOFDEntity("PublicRes", publicRes);
        return this;
    }

    /// <summary>【可选】获取 公共资源序列（0~n，可能含 null）。</summary>
    public List<ST_Loc?> GetPublicResList() =>
        GetOFDElements("PublicRes", ElementText).Select(ST_Loc.GetInstance).ToList();

    /// <summary>【可选】设置 文件资源序列路径（已存在则替换）。</summary>
    public CT_CommonData SetDocumentRes(ST_Loc documentRes)
    {
        SetOFDEntity("DocumentRes", documentRes);
        return this;
    }

    /// <summary>【可选】添加 文件资源序列路径（null 或空路径忽略）。</summary>
    public CT_CommonData AddDocumentRes(ST_Loc? documentRes)
    {
        if (documentRes?.Loc == null) return this;
        AddOFDEntity("DocumentRes", documentRes);
        return this;
    }

    /// <summary>【可选】获取 文件资源序列路径（第一个），不存在 null。</summary>
    public ST_Loc? GetDocumentRes() => ST_Loc.GetInstance(GetOFDElementText("DocumentRes"));

    /// <summary>【可选】获取 文件资源序列（0~n，可能含 null）。</summary>
    public List<ST_Loc?> GetDocumentResList() =>
        GetOFDElements("DocumentRes", ElementText).Select(ST_Loc.GetInstance).ToList();

    /// <summary>【可选】增加 模板页。</summary>
    public CT_CommonData AddTemplatePage(CT_TemplatePage templatePage)
    {
        Add(templatePage);
        return this;
    }

    /// <summary>【可选】获取 模板页序列。</summary>
    public List<CT_TemplatePage> GetTemplatePages() => GetOFDElements("TemplatePage", e => new CT_TemplatePage(e));

    /// <summary>【可选】设置 引用资源文件中的默认颜色空间标识（不存在时采用 RGB）。</summary>
    public CT_CommonData SetDefaultCS(ST_RefID defaultCS)
    {
        SetOFDEntity("DefaultCS", defaultCS);
        return this;
    }

    /// <summary>【可选】获取 默认颜色空间引用，不存在 null。</summary>
    public ST_RefID? GetDefaultCS() => ST_RefID.GetInstance(GetOFDElementText("DefaultCS"));
}
