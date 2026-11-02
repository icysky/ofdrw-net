using System.IO.Compression;
using System.Text;
using Ofd.Archive.Check;
using Ofd.Archive.Check.Rule;
using Ofd.Archive.Convert;
using Ofd.Archive.Convert.Handler;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Archive;

/// <summary>OFD-A 转换器：DocType 转换、处理器管道、可达文件白名单（夹带排除）。</summary>
public class ArchiveConverterTests : IDisposable
{
    private readonly string _root;

    public ArchiveConverterTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_conv_" + Guid.NewGuid().ToString("N")[..8]);
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
            // ignore
        }
    }

    private string BuildDoc(string name = "src.ofd")
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);
        doc.Add(new Paragraph("convert me", 4));
        return path;
    }

    private static Dictionary<string, byte[]> ReadEntries(string ofdPath)
    {
        var res = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        using var zip = ZipFile.OpenRead(ofdPath);
        foreach (var e in zip.Entries)
        {
            using var ms = new MemoryStream();
            using var s = e.Open();
            s.CopyTo(ms);
            res[e.FullName] = ms.ToArray();
        }
        return res;
    }

    [Fact]
    public void Convert_sets_doc_type_to_ofd_a()
    {
        string src = BuildDoc();
        string dst = Path.Combine(_root, "out.ofd");

        new OFDArchiveConverter().Convert(src, dst);

        Assert.True(File.Exists(dst));

        var entries = ReadEntries(dst);
        Assert.Contains("OFD.xml", entries.Keys);
        Assert.Contains("Doc_0/Document.xml", entries.Keys);

        string ofdXml = Encoding.UTF8.GetString(entries["OFD.xml"]);
        Assert.Contains("OFD-A", ofdXml);

        // 检查器不再报告 DocType 违规
        var violations = new OFDArchiveChecker().Check(dst);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == DocTypeRule.RULE_NAME);
    }

    [Fact]
    public void Convert_preserves_page_content()
    {
        string src = BuildDoc("content.ofd");
        string dst = Path.Combine(_root, "content-out.ofd");

        new OFDArchiveConverter().Convert(src, dst);

        using var reader = new OFDReader(dst);
        var page = reader.GetPage(1);
        var texts = page.GetContent()!.GetLayers()
            .SelectMany(l => l.GetPageBlocks())
            .OfType<Ofd.Core.BasicStructure.PageObj.Layer.Block.TextObject>()
            .ToList();

        Assert.Contains(texts, t => t.GetTextCodes()[0].GetContent() == "convert me");
    }

    [Fact]
    public void Convert_excludes_unreferenced_files()
    {
        string src = BuildDoc("stray-src.ofd");

        // 向源包塞入未被引用的文件（模拟夹带）
        string tampered = Path.Combine(_root, "tampered.ofd");
        using (var srcZip = ZipFile.OpenRead(src))
        using (var dstStream = File.Create(tampered))
        using (var dstZip = new ZipArchive(dstStream, ZipArchiveMode.Create))
        {
            foreach (var e in srcZip.Entries)
            {
                var newEntry = dstZip.CreateEntry(e.FullName);
                using var input = e.Open();
                using var output = newEntry.Open();
                input.CopyTo(output);
            }

            var stray = dstZip.CreateEntry("stray.bin");
            using var strayStream = stray.Open();
            var payload = Encoding.UTF8.GetBytes("evil payload");
            strayStream.Write(payload, 0, payload.Length);
        }

        string dst = Path.Combine(_root, "clean.ofd");
        new OFDArchiveConverter().Convert(tampered, dst);

        var entries = ReadEntries(dst);
        Assert.DoesNotContain("stray.bin", entries.Keys);
        Assert.Contains("OFD.xml", entries.Keys);
        Assert.Contains("Doc_0/Document.xml", entries.Keys);
        Assert.Contains(entries.Keys, k => k.EndsWith("Content.xml"));
    }

    [Fact]
    public void Custom_pipeline_runs_only_given_handlers()
    {
        string src = BuildDoc("custom-src.ofd");
        string dst = Path.Combine(_root, "custom-out.ofd");

        // 空管道：不做任何转换
        new OFDArchiveConverter(new List<IArchiveHandler>()).Convert(src, dst);

        var entries = ReadEntries(dst);
        string ofdXml = Encoding.UTF8.GetString(entries["OFD.xml"]);
        Assert.DoesNotContain("OFD-A", ofdXml);
    }

    [Fact]
    public void Convert_validates_input_paths()
    {
        var converter = new OFDArchiveConverter();

        Assert.Throws<ArgumentException>(() =>
            converter.Convert(Path.Combine(_root, "nope.ofd"), Path.Combine(_root, "x.ofd")));
        Assert.Throws<ArgumentException>(() => converter.Convert(BuildDoc("v.ofd"), null!));
    }

    /// <summary>改写 OFD 包中某个 XML 条目内容（用于构造测试样本）。</summary>
    private string RewriteEntry(string srcOfd, string entryName, Func<string, string> rewrite)
    {
        string dst = Path.Combine(_root, "rw-" + Guid.NewGuid().ToString("N")[..6] + ".ofd");
        using (var srcZip = ZipFile.OpenRead(srcOfd))
        using (var dstStream = File.Create(dst))
        using (var dstZip = new ZipArchive(dstStream, ZipArchiveMode.Create))
        {
            foreach (var e in srcZip.Entries)
            {
                var entry = dstZip.CreateEntry(e.FullName);
                using var input = e.Open();
                using var output = entry.Open();
                using var ms = new MemoryStream();
                input.CopyTo(ms);

                if (e.FullName == entryName)
                {
                    string xml = Encoding.UTF8.GetString(ms.ToArray());
                    var bytes = Encoding.UTF8.GetBytes(rewrite(xml));
                    output.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    ms.Position = 0;
                    ms.CopyTo(output);
                }
            }
        }
        return dst;
    }

    private static string PageXml(string ofdPath) =>
        Encoding.UTF8.GetString(ReadEntries(ofdPath)["Doc_0/Pages/Page_0/Content.xml"]);

    [Fact]
    public void Default_pipeline_removes_non_goto_page_actions()
    {
        string src = BuildDoc("action-src.ofd");
        string injected = RewriteEntry(src, "Doc_0/Pages/Page_0/Content.xml", xml =>
            xml.Replace("</ofd:TextObject>",
                "<ofd:Actions><ofd:Action Event=\"CLICK\"><ofd:URI>http://evil.example</ofd:URI></ofd:Action></ofd:Actions></ofd:TextObject>"));

        // 注入生效
        Assert.Contains("URI", PageXml(injected));

        string dst = Path.Combine(_root, "action-out.ofd");
        new OFDArchiveConverter().Convert(injected, dst);

        string content = PageXml(dst);
        Assert.DoesNotContain("URI", content);
        Assert.DoesNotContain("Actions", content);
    }

    [Fact]
    public void Default_pipeline_keeps_goto_page_actions()
    {
        string src = BuildDoc("goto-src.ofd");
        string injected = RewriteEntry(src, "Doc_0/Pages/Page_0/Content.xml", xml =>
            xml.Replace("</ofd:TextObject>",
                "<ofd:Actions><ofd:Action Event=\"CLICK\"><ofd:Goto/></ofd:Action></ofd:Actions></ofd:TextObject>"));

        string dst = Path.Combine(_root, "goto-out.ofd");
        new OFDArchiveConverter().Convert(injected, dst);

        Assert.Contains("Actions", PageXml(dst));
        Assert.Contains("Goto", PageXml(dst));
    }

    [Fact]
    public void Default_pipeline_sets_image_interpolate_false()
    {
        string png = Path.Combine(_root, "img.png");
        using (var bmp = new SkiaSharp.SKBitmap(4, 4))
        using (var img = SkiaSharp.SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        string src = Path.Combine(_root, "img-src.ofd");
        using (var doc = new OFDDoc(src))
        {
            var canvas = new Ofd.Layout.Element.Canvas.Canvas(20, 20, new ImageOnlyDrawer(png));
            doc.Add(canvas);
        }

        string dst = Path.Combine(_root, "img-out.ofd");
        new OFDArchiveConverter().Convert(src, dst);

        string content = PageXml(dst);
        if (content.Contains("ImageObject"))
            Assert.Contains("Interpolate=\"false\"", content);
    }

    /// <summary>仅绘制图片的绘制器。</summary>
    private sealed class ImageOnlyDrawer : Ofd.Layout.Element.Canvas.IDrawer
    {
        private readonly string _path;

        public ImageOnlyDrawer(string path) => _path = path;

        public void Draw(Ofd.Layout.Element.Canvas.DrawContext ctx) => ctx.DrawImage(_path, 0, 0, 10, 10);
    }

    [Fact]
    public void Layer_names_are_made_unique()
    {
        string src = BuildDoc("layer-src.ofd");
        string injected = RewriteEntry(src, "Doc_0/Pages/Page_0/Content.xml", xml =>
        {
            // 给现有 Layer 命名，并追加一个同名 Layer
            string named = xml.Replace("<ofd:Layer", "<ofd:Layer Name=\"L\"");
            return named.Replace("</ofd:Content>", "<ofd:Layer Name=\"L\"></ofd:Layer></ofd:Content>");
        });

        string dst = Path.Combine(_root, "layer-out.ofd");
        new OFDArchiveConverter().Convert(injected, dst);

        string content = PageXml(dst);
        Assert.Contains("Name=\"L\"", content);
        Assert.Contains("Name=\"L_2\"", content);
    }

    [Fact]
    public void Zero_area_clip_is_removed_and_object_hidden()
    {
        string src = BuildDoc("clip-src.ofd");
        string injected = RewriteEntry(src, "Doc_0/Pages/Page_0/Content.xml", xml =>
            xml.Replace("</ofd:Content>",
                "<ofd:ClipArea ID=\"900\" Boundary=\"0 0 0 0\"></ofd:ClipArea>" +
                "<ofd:PathObject ID=\"901\" Clip=\"900\" Boundary=\"0 0 1 1\"></ofd:PathObject>" +
                "</ofd:Content>"));

        Assert.Contains("ClipArea", PageXml(injected));

        string dst = Path.Combine(_root, "clip-out.ofd");
        new OFDArchiveConverter().Convert(injected, dst);

        string content = PageXml(dst);
        Assert.DoesNotContain("ClipArea", content);
        Assert.Contains("Visible=\"false\"", content);
    }

    [Fact]
    public void Annotation_is_made_readonly_and_fixed()
    {
        // 先生成带注释的文档
        string baseDoc = Path.Combine(_root, "annot-base.ofd");
        using (var doc = new OFDDoc(baseDoc))
            doc.Add(new Paragraph("annot", 4));

        string withAnnot = Path.Combine(_root, "annot-with.ofd");
        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, withAnnot))
        {
            var annotation = new Ofd.Layout.Edit.Annotation(
                new Ofd.Core.BasicType.ST_Box(10, 10, 20, 20),
                Ofd.Core.Annotation.PageAnnot.AnnotType.Path,
                new NullDrawer());
            doc.AddAnnotation(1, annotation);
        }

        string dst = Path.Combine(_root, "annot-out.ofd");
        new OFDArchiveConverter().Convert(withAnnot, dst);

        var entries = ReadEntries(dst);
        string annotXml = entries.Where(kv => kv.Key.Contains("Annot_")).Select(kv => Encoding.UTF8.GetString(kv.Value)).First();
        Assert.Contains("ReadOnly=\"true\"", annotXml);
        Assert.Contains("NoZoom=\"true\"", annotXml);
        Assert.Contains("NoRotate=\"true\"", annotXml);
    }

    [Fact]
    public void Binary_attachment_is_removed_by_conversion()
    {
        string pdf = Path.Combine(_root, "a.pdf");
        File.WriteAllText(pdf, "pdf-bytes");
        string txt = Path.Combine(_root, "b.txt");
        File.WriteAllText(txt, "text-bytes");

        string src = Path.Combine(_root, "att-src.ofd");
        using (var doc = new OFDDoc(src))
        {
            doc.Add(new Paragraph("att", 4));
            doc.AddAttachment(new Ofd.Layout.Edit.Attachment("规范", pdf).SetFormat("pdf"));
            doc.AddAttachment(new Ofd.Layout.Edit.Attachment("说明", txt).SetFormat("txt"));
        }

        string dst = Path.Combine(_root, "att-out.ofd");
        new OFDArchiveConverter().Convert(src, dst);

        var entries = ReadEntries(dst);
        string xml = Encoding.UTF8.GetString(entries["Doc_0/Attachments.xml"]);

        Assert.DoesNotContain("规范", xml);
        Assert.Contains("说明", xml);
        Assert.DoesNotContain(entries.Keys, k => k.EndsWith("a.pdf"));
        Assert.Contains(entries.Keys, k => k.EndsWith("b.txt"));
    }

    /// <summary>空绘制器（注释外观绘制用）。</summary>
    private sealed class NullDrawer : Ofd.Layout.Element.Canvas.IDrawer
    {
        public void Draw(Ofd.Layout.Element.Canvas.DrawContext ctx) { }
    }

    [Fact]
    public void Encrypted_doc_without_decryptor_aborts_conversion()
    {
        string src = BuildDoc("enc-src.ofd");

        // 解压后塞入 Encryptions.xml，模拟已加密文档
        string dir = Path.Combine(_root, "encdir");
        Ofd.Reader.ZipUtil.UnZipFiles(src, dir + Path.DirectorySeparatorChar);
        File.WriteAllText(Path.Combine(dir, "Encryptions.xml"), "<ofd:Encryptions/>");

        using var reader = new OFDReader(dir, false);
        var handler = new EncryptionHandler();

        var ex = Assert.Throws<IOException>(() => handler.Handle(reader, reader.GetOFDDir()));
        Assert.Contains("未配置解密器", ex.Message);
        Assert.False(handler.HasDecryptor);
    }

    [Fact]
    public void Unencrypted_doc_passes_encryption_handler()
    {
        string src = BuildDoc("enc-plain.ofd");
        string dir = Path.Combine(_root, "encplain");
        Ofd.Reader.ZipUtil.UnZipFiles(src, dir + Path.DirectorySeparatorChar);

        using var reader = new OFDReader(dir, false);
        new EncryptionHandler().Handle(reader, reader.GetOFDDir()); // 不抛异常即为通过
        Assert.True(File.Exists(Path.Combine(dir, "OFD.xml")));
    }

    [Fact]
    public void Add_decryptor_requires_encryption_handler_in_pipeline()
    {
        var converter = new OFDArchiveConverter(new List<IArchiveHandler> { new DocTypeHandler() });
        Assert.Throws<InvalidOperationException>(() => converter.AddDecryptor(new FakeDecryptor()));
    }

    private sealed class FakeDecryptor : Ofd.Crypto.Decryptor.IUserFekDecryptor
    {
        public Ofd.Crypto.Decryptor.DecryptResult Decrypt(Ofd.Core.Crypto.Encryt.UserInfo userInfo) =>
            throw new NotSupportedException("测试用解密器不执行解密");

        public string EncryptCaseId() => "test";

        public string? GetUsername() => null;
    }

    [Fact]
    public void Conversion_keeps_shared_image_resources_resolvable()
    {
        // 同一张图在两页使用 → 布局把图像登记到 DocumentRes
        string png = Path.Combine(_root, "shared.png");
        using (var bmp = new SkiaSharp.SKBitmap(8, 8))
        using (var img = SkiaSharp.SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        string src = Path.Combine(_root, "shared-src.ofd");
        using (var doc = new OFDDoc(src))
        {
            doc.Add(new Ofd.Layout.Element.Canvas.Canvas(50, 200, new ImageOnlyDrawer(png)));
            doc.Add(new Ofd.Layout.Element.Canvas.Canvas(50, 200, new ImageOnlyDrawer(png)));
        }

        using (var reader = new OFDReader(src))
            Assert.Equal(2, reader.GetNumberOfPages());

        string dst = Path.Combine(_root, "shared-out.ofd");
        new OFDArchiveConverter().Convert(src, dst);

        // 转换后图像资源仍可被解析（资源迁移未破坏引用）
        using (var reader = new OFDReader(dst))
        {
            var medias = reader.GetResMgt()!.GetMultiMedias();
            Assert.NotEmpty(medias);
            Assert.All(medias, m => Assert.NotNull(m.GetMediaFile()));
        }

        var entries = ReadEntries(dst);
        Assert.Contains("Doc_0/DocumentRes.xml", entries.Keys);
    }

    [Fact]
    public void Doc_type_handler_alone_is_idempotent()
    {
        string src = BuildDoc("idem.ofd");
        string first = Path.Combine(_root, "idem1.ofd");
        string second = Path.Combine(_root, "idem2.ofd");

        var pipeline = new List<IArchiveHandler> { new DocTypeHandler() };
        new OFDArchiveConverter(pipeline).Convert(src, first);
        new OFDArchiveConverter(pipeline).Convert(first, second);

        using var reader = new OFDReader(second);
        Assert.Equal("OFD-A", reader.GetOFDDir().GetOfd().GetDocType());
    }
}
