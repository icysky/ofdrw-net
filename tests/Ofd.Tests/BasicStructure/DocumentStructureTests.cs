using System.Text;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Doc.Bookmark;
using Ofd.Core.BasicStructure.Doc.Permission;
using Ofd.Core.BasicStructure.Doc.VPreferences;
using Ofd.Core.BasicStructure.Outlines;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Xunit;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;
using PageNode = Ofd.Core.BasicStructure.PageTree.Page;

namespace Ofd.Tests.BasicStructure;

/// <summary>Document/CT_CommonData/Pages/Outlines 文档结构层验证。</summary>
public class DocumentStructureTests
{
    private static Document BuildDocument()
    {
        var pageArea = new CT_PageArea()
            .SetPhysicalBox(new ST_Box(0, 0, 210, 297))
            .SetApplicationBox(new ST_Box(0, 0, 210, 297))
            .SetContentBox(new ST_Box(10, 10, 190, 277));

        var commonData = new CT_CommonData()
            .SetMaxUnitID(999L)
            .SetPageArea(pageArea)
            .SetPublicRes(new ST_Loc("Res/PublicRes.xml"))
            .AddPublicRes(new ST_Loc("Res/PublicRes2.xml"))
            .SetDocumentRes(new ST_Loc("Res/DocumentRes.xml"))
            .SetDefaultCS(ST_RefID.GetInstance(8))
            .AddTemplatePage(new CT_TemplatePage().SetID(new ST_ID(100)).SetTemplatePageName("封面").SetZOrder(LayerType.Foreground).SetBaseLoc(new ST_Loc("Tpl/0/Content.xml")));

        var pages = new Pages()
            .AddPage(new PageNode(101, "Pages/Page_0/Content.xml"))
            .AddPage(new PageNode(102, "Pages/Page_1/Content.xml"));

        var outlines = new Outlines()
            .AddOutlineElem(new CT_OutlineElem("第一章").SetCount(2).SetExpanded(true)
                .AddOutlineElem(new CT_OutlineElem("1.1").SetCount(0)));

        var doc = new Document()
            .SetCommonData(commonData)
            .SetPages(pages)
            .SetOutlines(outlines)
            .SetPermissions(new CT_Permission().SetEdit(false))
            .SetVPreferences(new CT_VPreferences().SetPageMode(PageMode.UseOutlines))
            .SetBookmarks(new Bookmarks().AddBookmark(new Bookmark("书签1", new Ofd.Core.Action.ActionType.ActionGoto.CT_Dest().SetType(Ofd.Core.Action.ActionType.ActionGoto.DestType.Fit).SetPageID(ST_RefID.GetInstance(101)))))
            .SetAnnotations(new ST_Loc("Annots/Annots.xml"))
            .SetCustomTags(new ST_Loc("Tags/Tags.xml"))
            .SetAttachments(new ST_Loc("Attachs/Attachments.xml"))
            .SetExtensions(new ST_Loc("Extensions/Extensions.xml"));
        return doc;
    }

    [Fact]
    public void Document_full_round_trip()
    {
        var doc = BuildDocument();
        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(doc.Proxy));

        var parsed = new Document(DomParser.Parse(xml));

        var cd = parsed.GetCommonData()!;
        Assert.Equal(999, cd.GetMaxUnitID()!.Id);
        var area = cd.GetPageArea()!;
        Assert.Equal("0 0 210 297", area.GetPhysicalBox()!.ToString());
        Assert.Equal("0 0 210 297", area.GetApplicationBox()!.ToString());
        Assert.Equal("10 10 190 277", area.GetContentBox()!.ToString());
        Assert.Equal("0 0 210 297", area.GetBox()!.ToString()); // GetBox 优先 PhysicalBox
        Assert.Equal("Res/PublicRes.xml", cd.GetPublicRes()!.ToString());
        Assert.Equal(2, cd.GetPublicResList().Count);
        Assert.Equal("Res/PublicRes2.xml", cd.GetPublicResList()[1]!.ToString());
        Assert.Equal("Res/DocumentRes.xml", cd.GetDocumentRes()!.ToString());
        Assert.Equal(8, cd.GetDefaultCS()!.GetRefId()!.Id);

        var tpl = Assert.Single(cd.GetTemplatePages());
        Assert.Equal(100, tpl.GetID()!.Id);
        Assert.Equal("封面", tpl.GetTemplatePageName());
        Assert.Equal(LayerType.Foreground, tpl.GetZOrder());
        Assert.Equal("Tpl/0/Content.xml", tpl.GetBaseLoc()!.ToString());

        var pages = parsed.GetPages()!;
        Assert.Equal(2, pages.GetSize());
        Assert.Equal(101, pages.GetPageByIndex(0).GetID()!.Id);
        Assert.Equal("Pages/Page_1/Content.xml", pages.GetPageByIndex(1).GetBaseLoc()!.ToString());

        var ol = parsed.GetOutlines()!;
        var root = Assert.Single(ol.GetOutlineElems());
        Assert.Equal("第一章", root.GetTitle());
        Assert.Equal(2, root.GetCount());
        Assert.True(root.GetExpanded());
        Assert.Equal("1.1", Assert.Single(root.GetOutlineElems()).GetTitle());
        Assert.Equal("第一章", ol.First().GetTitle()); // IEnumerable 语义

        Assert.False(parsed.GetPermission()!.GetEdit());
        Assert.Equal(PageMode.UseOutlines, parsed.GetVPreferences()!.GetPageMode());
        Assert.Equal("书签1", parsed.GetBookmarks()!.GetBookmarks()[0].GetBookmarkName());
        Assert.Equal("Annots/Annots.xml", parsed.GetAnnotations()!.ToString());
        Assert.Equal("Tags/Tags.xml", parsed.GetCustomTags()!.ToString());
        Assert.Equal("Attachs/Attachments.xml", parsed.GetAttachments()!.ToString());
        Assert.Equal("Extensions/Extensions.xml", parsed.GetExtensions()!.ToString());
    }

    [Fact]
    public void CommonData_defaults_and_lists()
    {
        var cd = new CT_CommonData();
        Assert.Null(cd.GetMaxUnitID());
        Assert.Null(cd.GetPageArea());
        Assert.Null(cd.GetPublicRes());
        Assert.Empty(cd.GetPublicResList());
        Assert.Empty(cd.GetTemplatePages());
        Assert.Null(cd.GetDefaultCS());

        // AddPublicRes 仅对 null 忽略；空串路径 Java 同样会生成空节点（读回为 null 项）
        cd.AddPublicRes(null);
        Assert.Empty(cd.GetPublicResList());
        cd.AddPublicRes(new ST_Loc(""));
        Assert.Single(cd.GetPublicResList());
        cd.AddPublicRes(new ST_Loc("R1.xml"));
        Assert.Equal(2, cd.GetPublicResList().Count);
    }

    [Fact]
    public void TemplatePage_zorder_default_and_null_removal()
    {
        var tpl = new CT_TemplatePage();
        Assert.Equal(LayerType.Background, tpl.GetZOrder()); // 缺省 Background
        Assert.Null(tpl.GetID());
        tpl.SetZOrder(LayerType.Body);
        Assert.Equal(LayerType.Body, tpl.GetZOrder());
        tpl.SetZOrder(null);
        Assert.Equal(LayerType.Background, tpl.GetZOrder());

        Assert.Equal(5, LayerType.Foreground.Order());
        Assert.Equal(3, LayerType.Body.Order());
        Assert.Equal(1, LayerType.Background.Order());
        Assert.Throws<ArgumentException>(() => TypeExtensions.GetInstance("Bogus"));
        Assert.Equal(LayerType.Body, TypeExtensions.GetInstance("")); // 空串默认 Body
    }

    [Fact]
    public void OutlineElem_defaults()
    {
        var e = new CT_OutlineElem("T");
        Assert.Equal(0, e.GetCount());
        Assert.True(e.GetExpanded()); // 缺省 true
        e.SetExpanded(false);
        Assert.False(e.GetExpanded());
        Assert.Empty(e.GetOutlineElems());
    }
}
