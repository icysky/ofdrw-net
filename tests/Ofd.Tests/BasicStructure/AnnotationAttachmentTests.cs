using System.Text;
using Ofd.Core.Annotation;
using Ofd.Core.Annotation.PageAnnot;
using Ofd.Core.Attachment;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Xunit;

namespace Ofd.Tests.BasicStructure;

/// <summary>注释（第 15 章）与附件（第 20 章）验证。</summary>
public class AnnotationAttachmentTests
{
    [Fact]
    public void Annotations_index_by_page_id()
    {
        var annotations = new Annotations()
            .AddPage(new AnnPage().SetPageID(new ST_ID(101)).SetFileLoc(new ST_Loc("Annots/Page_0/Annot.xml")))
            .AddPage(new AnnPage().SetPageID(ST_RefID.GetInstance(102)).SetFileLoc(new ST_Loc("Annots/Page_1/Annot.xml")))
            .AddPage(null);

        Assert.Equal(2, annotations.GetPages().Count);
        var page = annotations.GetByPageId("102")!;
        Assert.Equal("Annots/Page_1/Annot.xml", page.GetFileLoc()!.ToString());
        Assert.Null(annotations.GetByPageId("999"));
        Assert.Equal(101, annotations.GetPages()[0].GetPageID()!.GetRefId()!.Id);
        Assert.Throws<ArgumentException>(() => new AnnPage().SetPageID((ST_RefID)null!));
    }

    [Fact]
    public void Annot_full_structure_round_trip()
    {
        var appearance = new Appearance(new ST_Box(0, 0, 100, 50));
        var textObj = new TextObject(new ST_ID(9));
        textObj.SetFont(ST_RefID.GetInstance(1));
        textObj.SetSize(2);
        appearance.AddPageBlock(textObj);

        var annot = new Annot()
            .SetId(new ST_ID(5))
            .SetType(AnnotType.Highlight)
            .SetCreator("测试用户")
            .SetLastModDate(new DateOnly(2026, 10, 21))
            .SetSubtype("sub")
            .SetVisible(true)
            .SetPrint(false)
            .SetNoZoom(true)
            .SetNoRotate(true)
            .SetReadOnly(false)
            .SetRemark("这是注释说明")
            .SetAppearance(appearance);

        annot.AddParameter("color", "#FF0000");
        annot.AddParameter("opacity", "0.5");
        annot.AddParameter("color", "#00FF00"); // 同名覆盖

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(annot.Proxy));
        var parsed = new Annot(DomParser.Parse(xml));

        Assert.Equal(5, parsed.GetId()!.Id);
        Assert.Equal(AnnotType.Highlight, parsed.GetType());
        Assert.Equal("测试用户", parsed.GetCreator());
        Assert.Equal(new DateOnly(2026, 10, 21), parsed.GetLastModDate());
        Assert.Equal("sub", parsed.GetSubtype());
        Assert.True(parsed.GetVisible());
        Assert.False(parsed.GetPrint());
        Assert.True(parsed.GetNoZoom());
        Assert.True(parsed.GetNoRotate());
        Assert.False(parsed.GetReadOnly());
        Assert.Equal("这是注释说明", parsed.GetRemark());

        var parameters = parsed.GetParameters();
        Assert.Equal(2, parameters.Count);
        Assert.Equal("#00FF00", parameters["color"]); // 同名覆盖生效
        Assert.Equal("0.5", parameters["opacity"]);

        var app = parsed.GetAppearance()!;
        Assert.Equal("0 0 100 50", app.GetBoundary()!.ToString());
        Assert.IsType<TextObject>(app.GetPageBlocks()[0]);
    }

    [Fact]
    public void Annot_required_fields_and_defaults()
    {
        var annot = new Annot();
        Assert.Throws<InvalidOperationException>(() => annot.GetCreator());     // Creator 必选
        Assert.Throws<InvalidOperationException>(() => annot.GetLastModDate()); // LastModDate 必选
        Assert.Throws<ArgumentException>(() => annot.SetId(null!));
        Assert.Throws<ArgumentException>(() => annot.SetAppearance(null!));

        Assert.True(annot.GetVisible());   // 缺省 true
        Assert.Equal(true, annot.GetPrint());     // 缺省 true
        Assert.Equal(false, annot.GetNoZoom());   // 缺省 false
        Assert.Equal(false, annot.GetNoRotate());
        Assert.Equal(true, annot.GetReadOnly());
        Assert.Empty(annot.GetParameters());
        Assert.Null(annot.GetAppearance());
        Assert.Null(annot.GetRemark());

        // null 设置删除属性
        annot.SetPrint(true).SetPrint(null);
        Assert.Null(annot.Proxy.Attr("Print"));
        annot.SetRemark("x").SetRemark(null);
        Assert.Null(annot.GetOFDElement("Remark"));
    }

    [Fact]
    public void PageAnnot_container()
    {
        var pageAnnot = new PageAnnot()
            .AddAnnot(new Annot().SetId(new ST_ID(1)).SetType(AnnotType.Stamp).SetCreator("c").SetLastModDate(new DateOnly(2026, 1, 1)))
            .AddAnnot(null);
        Assert.Single(pageAnnot.GetAnnots());
        Assert.Equal(AnnotType.Stamp, pageAnnot.GetAnnots()[0].GetType());
    }

    [Fact]
    public void Attachments_round_trip_with_date_formats()
    {
        var attachment = new CT_Attachment()
            .SetId("att-1")
            .SetAttachmentName("说明书.pdf")
            .SetFormat("pdf")
            .SetSize(1024.5)
            .SetVisible(true)
            .SetUsage("source")
            .SetCreationDate(new DateTime(2026, 10, 21, 8, 30, 0))
            .SetModDate(new DateOnly(2026, 10, 22))
            .SetFileLoc(new ST_Loc("Attachs/att-1.pdf"));

        var attachments = new Attachments().AddAttachment(attachment).AddAttachment(null);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(attachments.Proxy));
        var parsed = new Attachments(DomParser.Parse(xml));
        var a = Assert.Single(parsed.GetAttachments());

        Assert.Equal("att-1", a.GetId());
        Assert.Equal("说明书.pdf", a.GetAttachmentName());
        Assert.Equal("pdf", a.GetFormat());
        Assert.Equal(1024.5, a.GetSize());
        Assert.True(a.GetVisible());
        Assert.Equal("source", a.GetUsage());
        Assert.Equal(new DateTime(2026, 10, 21, 8, 30, 0), a.GetCreationDateTime());
        Assert.Equal(new DateOnly(2026, 10, 22), a.GetModDate()); // 仅日期写入按 00:00:00
        Assert.Equal("Attachs/att-1.pdf", a.GetFileLoc()!.ToString());

        // 多格式兼容解析（yyyy/MM/dd HH:mm:ss 与仅日期）
        a.Proxy.SetAttr("CreationDate", "2026/10/21 08:30:00");
        Assert.Equal(new DateOnly(2026, 10, 21), a.GetCreationDate());
        a.Proxy.SetAttr("CreationDate", "2026-10-21");
        Assert.Equal(new DateTime(2026, 10, 21, 0, 0, 0), a.GetCreationDateTime());

        // 必选校验与缺省
        Assert.Throws<InvalidOperationException>(() => new CT_Attachment().GetId());
        Assert.Throws<InvalidOperationException>(() => new CT_Attachment().GetAttachmentName());
        Assert.Throws<ArgumentException>(() => new CT_Attachment().SetId(" "));
        Assert.Equal("none", new CT_Attachment().GetUsage());
        Assert.True(new CT_Attachment().GetVisible());
        Assert.Null(new CT_Attachment().GetSize());
        Assert.Null(new CT_Attachment().GetCreationDate());
    }
}
