using System.Text;
using Ofd.Core;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.Ofd.DocInfo;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Core.Versions;
using Xunit;

namespace Ofd.Tests.BasicStructure;

/// <summary>OFD/DocBody/CT_DocInfo 及 versions 包：建树、序列化结构、解析回读。</summary>
public class OFDBasicStructureTests
{
    [Fact]
    public void OFD_tree_builds_and_serializes_with_default_header()
    {
        var ofd = new OFD();
        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(ofd.Proxy));

        // 无子内容时为自闭合元素（dom4j 行为一致）
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<ofd:OFD xmlns:ofd=\"http://www.ofdspec.org/2016\" Version=\"1.2\" DocType=\"OFD\"", xml);
        Assert.Contains("DocType=\"OFD\"", xml);
        Assert.Equal("1.2", ofd.GetVersion());
        Assert.Equal("OFD", ofd.GetDocType());
        Assert.Null(new OFD(DomParser.Parse("<ofd:OFD xmlns:ofd=\"http://www.ofdspec.org/2016\"/>")).GetDocBody());
    }

    [Fact]
    public void Full_document_round_trip()
    {
        var docInfo = new CT_DocInfo()
            .SetTitle("测试文档")
            .SetAuthor("张三")
            .SetDocID(new Guid("0123456789abcdef0123456789abcdef"))
            .SetCreationDate(new DateOnly(2026, 10, 21))
            .SetDocUsage(DocUsage.EBook);
        docInfo.AddKeyword("公文");
        docInfo.AddKeyword("OFD");

        var docBody = new DocBody()
            .SetDocInfo(docInfo)
            .SetDocRoot(new ST_Loc("/Doc_0/Document.xml"))
            .SetSignatures(new ST_Loc("/Doc_0/Signs/Signs.xml"));

        var versions = new Versions().AddVersion(new global::Ofd.Core.Versions.Version().SetID("v1").SetBaseLoc(new ST_Loc("/Doc_0/V1.xml")));
        docBody.SetVersions(versions);

        var ofd = new OFD(docBody);

        // 序列化 → 解析 → 回读
        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(ofd.Proxy));
        var parsed = new OFD(DomParser.Parse(xml));

        Assert.Equal("1.2", parsed.GetVersion());
        var body = parsed.GetDocBody()!;
        var info = body.GetDocInfo()!;
        Assert.Equal("测试文档", info.GetTitle());
        Assert.Equal("张三", info.GetAuthor());
        Assert.Equal("0123456789abcdef0123456789abcdef", info.GetDocID());
        Assert.Equal(new DateOnly(2026, 10, 21), info.GetCreationDate());
        Assert.Equal(DocUsage.EBook, info.GetDocUsage());
        Assert.Equal(new[] { "公文", "OFD" }, info.GetKeywords());
        Assert.Equal("/Doc_0/Document.xml", body.GetDocRoot()!.ToString());
        Assert.Equal("/Doc_0/Signs/Signs.xml", body.GetSignatures()!.ToString());
        Assert.Equal("v1", body.GetVersions()!.GetVersions()[0].GetID());
        // 构造器默认值（Creator/CreatorVersion）未被显式 setter 覆盖
        Assert.Equal("ofdrw1.0.0-SNAPSHOT", info.GetCreator() + info.GetCreatorVersion());
    }

    [Fact]
    public void DocInfo_defaults_match_java()
    {
        var info = new CT_DocInfo();
        Assert.Equal("??", info.GetTitle());
        Assert.Equal("ofdrw", info.GetAuthor());
        Assert.Equal("ofdrw", info.GetCreator());
        Assert.Equal("1.0.0-SNAPSHOT", info.GetCreatorVersion());
        Assert.Null(info.GetAbstract());
        Assert.Null(info.GetCover());
        Assert.Empty(info.GetKeywords());
    }

    [Fact]
    public void Keywords_container_round_trip()
    {
        var keywords = new Keywords();
        keywords.AddKeyword("档案").AddKeyword("GB/T 33190");
        Assert.Equal(new[] { "档案", "GB/T 33190" }, keywords.GetKeywords()); // 上游 bug 已修：应查 Keyword 子节点
    }

    [Fact]
    public void CustomDatas_lookup()
    {
        var datas = new CustomDatas()
            .AddCustomData("Owner", "某单位")
            .AddCustomData("Level", "绝密");
        Assert.Equal("某单位", datas.GetCustomDataValue("Owner"));
        Assert.Equal("绝密", datas.GetCustomDataValue("Level"));
        Assert.Null(datas.GetCustomDataValue("NotExist"));
    }

    [Fact]
    public void Versions_and_files_round_trip()
    {
        var fileList = new FileList()
            .AddFile("f1", new ST_Loc("Document.xml"))
            .AddFile("f2", new ST_Loc("Signs.xml"));
        var dv = new DocVersion("dver1", fileList)
            .SetVersion("1.2")
            .SetDocVersionName("初版")
            .SetCreationDate(new DateOnly(2026, 1, 1))
            .SetDocRoot(new ST_Loc("/V/Document.xml"));

        var versions = new Versions();
        versions.AddVersion(new global::Ofd.Core.Versions.Version().SetID("entry").SetBaseLoc(new ST_Loc("/V/DocVersion.xml")));

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(dv.Proxy));
        var parsed = new DocVersion(DomParser.Parse(xml));
        Assert.Equal("dver1", parsed.GetID());
        Assert.Equal("1.2", parsed.GetVersion());
        Assert.Equal("初版", parsed.GetDocVersionName());
        Assert.Equal(new DateOnly(2026, 1, 1), parsed.GetCreationDate());
        Assert.Equal("/V/Document.xml", parsed.GetDocRoot()!.ToString());
        Assert.Equal(2, parsed.GetFileList()!.GetFiles().Count);
        Assert.Equal("Signs.xml", parsed.GetFileList()!.GetFiles()[1].GetFile().ToString());

        // SetVersion(null) 删除属性（上游 bug 已修）
        parsed.SetVersion(null);
        Assert.Null(parsed.GetVersion());
    }

    [Fact]
    public void DocUsage_parse_is_case_insensitive_with_default()
    {
        Assert.Equal(DocUsage.EBook, DocUsageExtensions.GetInstance("ebook"));
        Assert.Equal(DocUsage.EMagzine, DocUsageExtensions.GetInstance("EMagzine"));
        Assert.Equal(DocUsage.Normal, DocUsageExtensions.GetInstance("whatever"));
        Assert.Equal(DocUsage.Normal, DocUsageExtensions.GetInstance(null));
    }

    [Fact]
    public void StLoc_GetElement_produces_simple_element_for_Set()
    {
        // DocBody.SetDocRoot 内部走 ST_Loc.GetElement → Set 替换语义
        var body = new DocBody().SetDocRoot(new ST_Loc("/A.xml"));
        body.SetDocRoot(new ST_Loc("/B.xml")); // 同名替换而非追加
        Assert.Equal("/B.xml", body.GetDocRoot()!.ToString());
        Assert.Single(body.Elements().Where(e => e.Name == "DocRoot"));
    }
}
