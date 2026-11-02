using System.IO.Compression;
using Ofd.Core.Attachment;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.BasicType;
using Ofd.Core.Text.Font;
using Ofd.Pkg.Container;
using Ofd.Reader;
using Xunit;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;
using PageNode = Ofd.Core.BasicStructure.PageTree.Page;
using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;

namespace Ofd.Tests.Reader;

/// <summary>Reader 层：OFDReader 端到端读取、ResourceLocator 路径解析、ResourceManage 资源、ZipUtil 防护。</summary>
public class ReaderTests : IDisposable
{
    private readonly string _root;

    public ReaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_reader_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    /// <summary>构建一个包含模板页、双页面、公共资源、附件的 OFD 文件。</summary>
    private string BuildSampleOfd()
    {
        string root = Path.Combine(_root, "sample");
        string fontSrc = Path.Combine(_root, "font.ttf");
        File.WriteAllBytes(fontSrc, new byte[] { 9, 9, 9 });
        string imgSrc = Path.Combine(_root, "img.png");
        File.WriteAllBytes(imgSrc, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        string attachSrc = Path.Combine(_root, "readme.txt");
        File.WriteAllText(attachSrc, "attachment-content");

        var ofdDir = new OFDDir(root);
        var doc = ofdDir.NewDoc();

        // 公共资源：字体 + 图片
        var publicRes = new Res()
            .SetBaseLoc(new ST_Loc("/Doc_0"))
            .AddResource(new Fonts().AddFont(new CT_Font().SetID(new ST_ID(10)).SetFontFile(new ST_Loc("Res/font.ttf"))))
            .AddResource(new MultiMedias().AddMultiMedia(new CT_MultiMedia()
                .SetID(new ST_ID(11))
                .SetType(MediaType.Image)
                .SetMediaFile(new ST_Loc("Res/img.png"))));
        doc.SetPublicRes(publicRes);

        // CommonData：页面区域 + 公共资源 + 模板页
        var commonData = new CT_CommonData()
            .SetMaxUnitID(1000)
            .SetPageArea(new CT_PageArea(0, 0, 210, 297))
            .AddPublicRes(new ST_Loc("PublicRes.xml"))
            .AddTemplatePage(new CT_TemplatePage()
                .SetID(new ST_ID(500))
                .SetTemplatePageName("封面")
                .SetBaseLoc(new ST_Loc("Temps/Temp_0.xml")));

        // 文档结构：两页 + 附件入口
        doc.SetDocument(new Document()
            .SetCommonData(commonData)
            .SetPages(new Pages()
                .AddPage(new PageNode().SetID(new ST_ID(1)).SetBaseLoc(new ST_Loc("Pages/Page_0/Content.xml")))
                .AddPage(new PageNode().SetID(new ST_ID(2)).SetBaseLoc(new ST_Loc("Pages/Page_1/Content.xml"))))
            .SetAttachments(new ST_Loc("Attachments.xml")));

        // 模板页（背景层）
        var tplLayer = new CT_Layer().SetType(LayerType.Background);
        tplLayer.SetObjID(30);
        doc.ObtainTemps().Add(new PageObjType().SetContent(new Content().AddLayer(tplLayer)));

        // Page_0：自带页面区域 + 模板引用 + 正文层
        var pagesDir = doc.ObtainPages();
        var bodyLayer = new CT_Layer().SetType(LayerType.Body);
        bodyLayer.SetObjID(20);
        var page0 = pagesDir.NewPageDir();
        page0.SetContent(new PageObjType()
            .SetArea(new CT_PageArea(0, 0, 100, 200))
            .AddTemplate(new Template().SetTemplateID(ST_RefID.GetInstance(500)).SetZOrder(LayerType.Background))
            .SetContent(new Content().AddLayer(bodyLayer)));

        // Page_1：无页面区域（回退 CommonData）
        var page1 = pagesDir.NewPageDir();
        page1.SetContent(new PageObjType());

        // 附件
        doc.PutObj("Attachments.xml", new Attachments().AddAttachment(new CT_Attachment()
            .SetId("a1")
            .SetAttachmentName("readme.txt")
            .SetSize(1)
            .SetFileLoc(new ST_Loc("Attachs/readme.txt"))));

        ofdDir.PutFileAbs("/Doc_0/Res/font.ttf", fontSrc);
        ofdDir.PutFileAbs("/Doc_0/Res/img.png", imgSrc);
        ofdDir.PutFileAbs("/Doc_0/Attachs/readme.txt", attachSrc);
        ofdDir.SetOfd(new OFD().AddDocBody(new DocBody().SetDocRoot(new ST_Loc("Doc_0/Document.xml"))));
        ofdDir.Flush();

        string ofdFile = Path.Combine(_root, "sample.ofd");
        ofdDir.Jar(ofdFile);
        return ofdFile;
    }

    [Fact]
    public void OFDReader_reads_pages_templates_resources_and_attachments()
    {
        string ofdFile = BuildSampleOfd();
        var reader = new OFDReader(ofdFile);
        string workDir = reader.GetWorkDir();
        Assert.True(Directory.Exists(workDir));

        // 页面数量与文档结构
        Assert.Equal(2, reader.GetNumberOfPages());
        var docs = reader.GetDoc(0);
        Assert.Equal(2, docs.GetPages()!.GetSize());

        // Page_0：自带区域（100x200）+ 模板
        var info1 = reader.GetPageInfo(1);
        Assert.Equal(1, info1.GetIndex());
        Assert.Equal(1, info1.GetId()!.Id);
        Assert.Equal("0 0 100 200", info1.GetSize()!.ToString());
        Assert.Equal(0, info1.GetPageN());
        Assert.Equal("/Doc_0/Pages/Page_0/Content.xml", info1.GetPageAbsLoc()!.ToString());
        Assert.Single(info1.GetTemplates());
        Assert.Equal("封面", info1.GetTemplates()[0].GetTemplatePageName());

        // 图层顺序：模板背景层(1) 在页面正文层(3) 之前
        var layers = info1.GetAllLayer();
        Assert.Equal(2, layers.Count);
        Assert.Equal(LayerType.Background, layers[0].GetType());
        Assert.Equal(LayerType.Body, layers[1].GetType());

        // Page_1：无区域 → CommonData 的 210x297
        var info2 = reader.GetPageInfo(2);
        Assert.Equal("0 0 210 297", info2.GetSize()!.ToString());
        Assert.Equal(1, info2.GetPageN());
        Assert.Empty(info2.GetTemplates());

        // 页面列表 / 绝对路径 / 页面 ID
        Assert.Equal(2, reader.GetPageList().Count);
        Assert.Equal("/Doc_0/Pages/Page_0/Content.xml", reader.GetPageAbsLoc(1).ToString());
        Assert.Equal(2, reader.GetPageObjectId(2)!.Id);
        Assert.Equal("Page", reader.GetPage(1).Proxy.Name);
        Assert.Equal("0 0 210 297", reader.GetPageSize(2).ToString());

        // 签名：本样例无签名
        Assert.False(reader.HasSignature());
        Assert.Null(reader.GetDefaultSignatures());
        Assert.Null(reader.GetDefaultDocSignaturesPath());

        // 资源管理器：字体/图片（路径已转绝对路径，媒体字节可取）
        var resMgt = reader.GetResMgt();
        Assert.Single(resMgt.GetFonts());
        var font = resMgt.GetFont("10")!;
        Assert.Equal("/Doc_0/Res/font.ttf", font.GetFontFile()!.ToString());
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, resMgt.GetImageByteArray("11"));
        Assert.Null(resMgt.GetImageByteArray("10")); // 非图片类型

        // 附件：FileLoc 为绝对路径，字节一致
        var attachments = reader.GetAttachmentList();
        var att = Assert.Single(attachments);
        Assert.Equal("readme.txt", att.GetAttachmentName());
        Assert.Equal("/Doc_0/Attachs/readme.txt", att.GetFileLoc()!.ToString());
        string attFile = reader.GetAttachmentFile("readme.txt")!;
        Assert.Equal("attachment-content", File.ReadAllText(attFile));
        Assert.NotNull(reader.GetAttachment("readme.txt"));
        Assert.Null(reader.GetAttachment("nope.txt"));

        // Close 删除工作目录
        reader.Close();
        Assert.False(Directory.Exists(workDir));
    }

    [Fact]
    public void OFDReader_rejects_bad_inputs_and_page_numbers()
    {
        Assert.Throws<ArgumentException>(() => new OFDReader(Path.Combine(_root, "not-exist.ofd")));

        string ofdFile = BuildSampleOfd();
        using var reader = new OFDReader(ofdFile);
        Assert.Throws<FormatException>(() => reader.GetPageInfo(0));
        Assert.Throws<FormatException>(() => reader.GetPageInfo(99));
        Assert.Throws<FormatException>(() => reader.GetPageAbsLoc(-1));

        // 外部目录模式：Close 不删除
        string external = Path.Combine(_root, "sample");
        using var reader2 = new OFDReader(external, deleteOnClose: false);
        Assert.Equal(2, reader2.GetNumberOfPages());
        reader2.Close();
        Assert.True(Directory.Exists(external));
    }

    [Fact]
    public void ResourceLocator_resolves_paths_and_restores()
    {
        string root = Path.Combine(_root, "rl");
        var ofdDir = new OFDDir(root);
        var docDir = ofdDir.NewDoc();
        docDir.SetDocument(new Document());
        docDir.ObtainPages(); // 创建 Doc_0/Pages 目录
        ofdDir.Flush();

        var rl = new ResourceLocator(ofdDir);
        Assert.Equal("/", rl.Pwd());
        Assert.Equal("/Doc_0/A.xml", rl.ToAbsolutePath("Doc_0/A.xml"));

        rl.Cd("/Doc_0");
        Assert.Equal("/Doc_0", rl.Pwd());
        Assert.Equal("/Doc_0/A.xml", rl.ToAbsolutePath("A.xml"));
        Assert.Equal("/A.xml", rl.ToAbsolutePath("../A.xml"));
        Assert.Equal("/Doc_0/A.xml", rl.ToAbsolutePath("./A.xml"));
        Assert.Equal("/Doc_0", rl.ToString());

        // Save/Restore
        rl.Save();
        rl.Cd("/Doc_0/Pages");
        Assert.Equal("/Doc_0/Pages", rl.Pwd());
        rl.Restore();
        Assert.Equal("/Doc_0", rl.Pwd());
        rl.Restore(); // 无保存时无副作用
        Assert.Equal("/Doc_0", rl.Pwd());

        // GetAbsTo
        Assert.Equal("/Doc_0/Document.xml", rl.GetAbsTo(new ST_Loc("Document.xml")).ToString());
        Assert.Equal("/A.xml", rl.GetAbsTo(new ST_Loc("/A.xml")).ToString());
        Assert.Equal("/Doc_0", rl.GetAbsTo(null).ToString());

        // 存在性 / 不存在的目录
        Assert.True(rl.Exist("/Doc_0"));
        Assert.False(rl.Exist("/Doc_0/nope.xml"));
        Assert.Throws<ErrorPathException>(() => rl.Cd("/NotExist"));

        // 空路径获取对象
        Assert.Throws<FileNotFoundException>(() => rl.Get("", e => e.Name));
    }

    [Fact]
    public void ZipUtil_blocks_path_traversal()
    {
        string zipPath = Path.Combine(_root, "evil.zip");
        using (var fs = File.Create(zipPath))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("../evil.txt");
            using var s = entry.Open();
            s.WriteByte(1);
        }

        string desc = Path.Combine(_root, "unzip-evil");
        Assert.Throws<IOException>(() => ZipUtil.UnZipFiles(zipPath, desc));
        Assert.False(File.Exists(Path.Combine(_root, "evil.txt")));

        // 正常解压
        string okZip = Path.Combine(_root, "ok.zip");
        using (var fs = File.Create(okZip))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            var dirEntry = zip.CreateEntry("Doc_0/");
            _ = dirEntry;
            var e = zip.CreateEntry("Doc_0/Document.xml");
            using var s = e.Open();
            var bytes = System.Text.Encoding.UTF8.GetBytes("<ofd:Document/>");
            s.Write(bytes, 0, bytes.Length);
        }

        string okDesc = Path.Combine(_root, "unzip-ok");
        ZipUtil.UnZipFiles(okZip, okDesc);
        Assert.True(File.Exists(Path.Combine(okDesc, "Doc_0", "Document.xml")));
    }
}
