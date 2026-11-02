using Ofd.Archive.Check;
using Ofd.Archive.Check.Rule;
using Ofd.Archive.Convert;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Tests.Sign;
using Ofd.Reader;
using Ofd.Sign;
using Ofd.Sign.SignContainer;
using Ofd.Sign.Verify;
using Ofd.Sign.Verify.Container;
using Ofd.Tool.Merge;
using Xunit;

namespace Ofd.Tests.Integration;

/// <summary>
/// 全链路集成：排版出包 → 签名/验签 → OFD-A 检查与转换 → 合并 → 页面删除。
/// </summary>
public class EndToEndTests : IDisposable
{
    private readonly string _root;

    public EndToEndTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_e2e_" + Guid.NewGuid().ToString("N")[..8]);
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

    /// <summary>排版出包：每段固定 240mm（版心 246.2mm），一段一页。</summary>
    private string BuildPagedDoc(string name, params string[] texts)
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);

        foreach (var text in texts) doc.Add(new Paragraph(text, 5).SetHeight(240));

        return path;
    }

    /// <summary>递归收集页面内全部路径对象（Canvas 图元嵌套在 PageBlock 中）。</summary>
    private static List<Ofd.Core.BasicStructure.PageObj.Layer.Block.PathObject> AllPaths(
        Ofd.Core.BasicStructure.PageObj.PageObj page)
    {
        var result = new List<Ofd.Core.BasicStructure.PageObj.Layer.Block.PathObject>();
        foreach (var layer in page.GetContent()!.GetLayers())
            Collect(layer.GetPageBlocks(), result);
        return result;

        static void Collect(List<Ofd.Core.BasicStructure.PageObj.Layer.IPageBlockType> blocks,
            List<Ofd.Core.BasicStructure.PageObj.Layer.Block.PathObject> acc)
        {
            foreach (var b in blocks)
            {
                if (b is Ofd.Core.BasicStructure.PageObj.Layer.Block.PathObject p) acc.Add(p);
                else if (b is CT_PageBlock pb) Collect(pb.GetPageBlocks(), acc);
            }
        }
    }

    private static List<string> AllTexts(string ofd)
    {
        using var reader = new OFDReader(ofd);
        var all = new List<string>();

        for (int i = 1; i <= reader.GetNumberOfPages(); i++)
        {
            var page = reader.GetPage(i);
            all.AddRange(page.GetContent()!.GetLayers()
                .SelectMany(l => l.GetPageBlocks())
                .SelectMany(b => b switch
                {
                    TextObject t => t.GetTextCodes().Select(c => c.GetContent() ?? ""),
                    CT_PageBlock pb => pb.GetPageBlocks().OfType<TextObject>()
                        .SelectMany(t => t.GetTextCodes()).Select(c => c.GetContent() ?? ""),
                    _ => Enumerable.Empty<string>(),
                }));
        }

        return all;
    }

    [Fact]
    public void Layout_output_is_readable_end_to_end()
    {
        string doc = BuildPagedDoc("layout.ofd", "第一页正文", "第二页正文");

        using var reader = new OFDReader(doc);
        Assert.Equal(2, reader.GetNumberOfPages());

        var texts = AllTexts(doc);
        Assert.Contains("第一页正文", texts);
        Assert.Contains("第二页正文", texts);

        // 画布元素单独验证（避免影响上面的分页假设）
        string canvasDoc = Path.Combine(_root, "canvas.ofd");
        using (var canvasOfd = new OFDDoc(canvasDoc))
        {
            canvasOfd.Add(new Canvas(50, 20, new Line(50, 20).SetBeginPoint(0, 0).SetEndPoint(20, 10)));
        }

        using var canvasReader = new OFDReader(canvasDoc);
        var paths = AllPaths(canvasReader.GetPage(1));
        Assert.NotEmpty(paths);
        Assert.Contains("M", paths[0].GetAbbreviatedDataText());
    }

    [Fact]
    public void Signed_doc_validates_and_archive_check_reports_signature()
    {
        string plain = BuildPagedDoc("plain.ofd", "待签名内容");
        var (priv, cert) = SignTestKit.NewSm2Cert();

        string signed = Path.Combine(_root, "signed.ofd");
        using (var reader = new OFDReader(plain))
        using (var signer = new OFDSigner(reader, signed))
        {
            signer.SetSignContainer(new GBT35275DSContainer(cert, priv)).ExeSign();
        }

        // 验签通过
        using (var reader = new OFDReader(signed))
        using (var validator = new OFDValidator(reader))
        {
            validator.SetValidator(new GBT35275ValidateContainer());
            validator.ExeValidate();
        }

        // OFD-A 检查应报告「存在数字签名」
        var violations = new OFDArchiveChecker().Check(signed);
        Assert.Contains(violations, v => v.GetRuleName() == SignatureRule.RULE_NAME);
    }

    [Fact]
    public void Archive_conversion_removes_signature_and_sets_doc_type()
    {
        string plain = BuildPagedDoc("conv-src.ofd", "转换内容");
        var (priv, cert) = SignTestKit.NewSm2Cert();

        string signed = Path.Combine(_root, "conv-signed.ofd");
        using (var reader = new OFDReader(plain))
        using (var signer = new OFDSigner(reader, signed))
        {
            signer.SetSignContainer(new GBT35275DSContainer(cert, priv)).ExeSign();
        }

        string converted = Path.Combine(_root, "converted.ofd");
        new OFDArchiveConverter().Convert(signed, converted);

        // DocType 变为 OFD-A，签名被移除
        using (var reader = new OFDReader(converted))
        {
            Assert.Equal("OFD-A", reader.GetOFDDir().GetOfd().GetDocType());

            var signatures = reader.GetDefaultSignatures();
            Assert.True(signatures == null || signatures.GetSignatures().Count == 0);
        }

        var violations = new OFDArchiveChecker().Check(converted);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == DocTypeRule.RULE_NAME);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == SignatureRule.RULE_NAME);

        // 内容仍在
        Assert.Contains("转换内容", AllTexts(converted));
    }

    [Fact]
    public void Converted_docs_can_be_merged_and_pages_deleted()
    {
        string a = BuildPagedDoc("m-a.ofd", "A 内容");
        string b = BuildPagedDoc("m-b.ofd", "B 内容");

        string merged = Path.Combine(_root, "merged.ofd");
        using (var merger = new OFDMerger(merged))
        {
            merger.Add(a);
            merger.Add(b);
        }

        using (var reader = new OFDReader(merged))
            Assert.Equal(2, reader.GetNumberOfPages());

        var mergedTexts = AllTexts(merged);
        Assert.Contains("A 内容", mergedTexts);
        Assert.Contains("B 内容", mergedTexts);

        // 删除第一页
        string trimmed = Path.Combine(_root, "trimmed.ofd");
        using (var deleter = new OFDPageDeleter(merged, trimmed))
            deleter.Delete(0);

        using (var reader = new OFDReader(trimmed))
            Assert.Equal(1, reader.GetNumberOfPages());

        Assert.DoesNotContain("A 内容", AllTexts(trimmed));
        Assert.Contains("B 内容", AllTexts(trimmed));
    }

    [Fact]
    public void Encrypted_doc_converts_only_with_decryptor()
    {
        const string user = "张三";
        const string password = "12345678";

        // 1) 排版出包并加密
        string plain = BuildPagedDoc("enc-plain.ofd", "加密内容");
        string encrypted = Path.Combine(_root, "enc.ofd");

        using (var encryptor = new Ofd.Crypto.OFDEncryptor(plain, encrypted))
        {
            encryptor.AddUser(new Ofd.Crypto.Encryptor.UserPasswordEncryptor(user, password));
            encryptor.Encrypt();
        }

        // 2) 无解密器 → 转换必须硬中断
        var noKey = new OFDArchiveConverter();
        var ex = Assert.Throws<IOException>(() => noKey.Convert(encrypted, Path.Combine(_root, "enc-no-key.ofd")));
        Assert.Contains("未配置解密器", ex.Message);

        // 3) 配置解密器 → 正常转换
        string converted = Path.Combine(_root, "enc-converted.ofd");
        new OFDArchiveConverter()
            .AddDecryptor(new Ofd.Crypto.Decryptor.UserPasswordDecryptor(user, password))
            .Convert(encrypted, converted);

        using (var reader = new OFDReader(converted))
        {
            Assert.Equal("OFD-A", reader.GetOFDDir().GetOfd().GetDocType());
            Assert.Equal(1, reader.GetNumberOfPages());
        }

        Assert.Contains("加密内容", AllTexts(converted));

        // 4) 转换产物可再次检查（无 DOC_TYPE 违规）
        var violations = new OFDArchiveChecker().Check(converted);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == DocTypeRule.RULE_NAME);
    }

    [Fact]
    public void Archive_output_of_text_image_doc_is_readable()
    {
        // 文本 + 图像：同时产生 PublicRes 与 DocumentRes（Java 侧此场景转换产物仅剩 OFD.xml）
        string png = Path.Combine(_root, "full.png");
        using (var bmp = new SkiaSharp.SKBitmap(8, 8))
        using (var img = SkiaSharp.SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        string src = Path.Combine(_root, "full-src.ofd");
        using (var doc = new OFDDoc(src))
        {
            doc.Add(new Paragraph("全文", 5));
            doc.Add(new Ofd.Layout.Element.Img(20, 20, png));
        }

        string dst = Path.Combine(_root, "full-archived.ofd");
        new OFDArchiveConverter().Convert(src, dst);

        var entries = System.IO.Compression.ZipFile.OpenRead(dst).Entries
            .Select(e => e.FullName).ToList();

        Assert.Contains("Doc_0/Document.xml", entries);
        Assert.Contains("Doc_0/Pages/Page_0/Content.xml", entries);
        Assert.Contains("Doc_0/PublicRes.xml", entries);
        Assert.Contains("Doc_0/DocumentRes.xml", entries);

        // 产物可被正常读取（Java 侧同类场景产物仅剩 OFD.xml，Reader 直接报错）
        using var reader = new OFDReader(dst);
        Assert.Equal(1, reader.GetNumberOfPages());
        Assert.Contains("全文", AllTexts(dst));
        Assert.NotEmpty(reader.GetResMgt()!.GetMultiMedias());
    }

    [Fact]
    public void Full_chain_layout_sign_convert_merge()
    {
        // 1) 排版出包
        string layout = BuildPagedDoc("chain.ofd", "全链路");

        // 2) 签名 + 验签
        var (priv, cert) = SignTestKit.NewSm2Cert(77);
        string signed = Path.Combine(_root, "chain-signed.ofd");
        using (var reader = new OFDReader(layout))
        using (var signer = new OFDSigner(reader, signed))
        {
            signer.SetSignContainer(new GBT35275DSContainer(cert, priv)).ExeSign();
        }

        using (var reader = new OFDReader(signed))
        using (var validator = new OFDValidator(reader))
        {
            validator.SetValidator(new GBT35275ValidateContainer());
            validator.ExeValidate();
        }

        // 3) 转 OFD-A（去签名、改 DocType）
        string archived = Path.Combine(_root, "chain-archived.ofd");
        new OFDArchiveConverter().Convert(signed, archived);

        using (var reader = new OFDReader(archived))
            Assert.Equal("OFD-A", reader.GetOFDDir().GetOfd().GetDocType());

        // 4) 与另一文档合并
        string other = BuildPagedDoc("chain-other.ofd", "附加页");
        string final = Path.Combine(_root, "chain-final.ofd");
        using (var merger = new OFDMerger(final))
        {
            merger.Add(archived);
            merger.Add(other);
        }

        var texts = AllTexts(final);
        Assert.Contains("全链路", texts);
        Assert.Contains("附加页", texts);

        // 5) 合并产物是新建文档（DocType 回落 OFD）→ 再转一次 OFD-A 后检查
        string finalArchived = Path.Combine(_root, "chain-final-archived.ofd");
        new OFDArchiveConverter().Convert(final, finalArchived);

        var violations = new OFDArchiveChecker().Check(finalArchived);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == DocTypeRule.RULE_NAME);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == SignatureRule.RULE_NAME);
    }
}
