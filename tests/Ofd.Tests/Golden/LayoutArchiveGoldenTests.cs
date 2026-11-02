using System.Globalization;
using System.IO.Compression;
using System.Text;
using Ofd.Archive.Convert;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Font;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Golden;

/// <summary>
/// 排版 / 转换产物结构签名 golden：
/// 把「页面 → 图层 → 图元（类型/尺寸/边界/文字）」与「转换后包结构」压成稳定文本，
/// 与 <c>TestData/layout-archive-golden.txt</c> 比对，防止后续重构悄悄改变产物结构。
/// <para>更新基线：设置环境变量 <c>OFDNET_UPDATE_GOLDEN=1</c> 运行本类。</para>
/// </summary>
public class LayoutArchiveGoldenTests : IDisposable
{
    private readonly string _root;
    private readonly string _goldenFile;

    public LayoutArchiveGoldenTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_golden_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);

        // 测试运行目录为 bin/Release/net10.0
        _goldenFile = Path.Combine(AppContext.BaseDirectory, "TestData", "layout-archive-golden.txt");
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

    private string BuildDoc(string name)
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);

        // 使用带宽度映射表的字体，保证换行与 DeltaX 可预期
        var p1 = new Paragraph(80, null);
        p1.Add(new Span("Golden测试").SetFontSize(5).SetFont(Ofd.Font.FontName.TimesNewRoman.Font()));
        doc.Add(p1);

        var p2 = new Paragraph(60, null);
        p2.Add(new Span("second line").SetFontSize(4).SetFont(Ofd.Font.FontName.TimesNewRoman.Font()));
        doc.Add(p2);

        doc.Add(new Canvas(40, 20, new Line(40, 20).SetBeginPoint(0, 0).SetEndPoint(30, 15)));

        return path;
    }

    /// <summary>生成结构签名（不含随机 ID / 时间戳）。</summary>
    internal static string Signature(string ofd)
    {
        var sb = new StringBuilder();
        using var reader = new OFDReader(ofd);

        sb.AppendLine($"pages={reader.GetNumberOfPages()}");
        sb.AppendLine($"docType={reader.GetOFDDir().GetOfd().GetDocType()}");

        for (int i = 1; i <= reader.GetNumberOfPages(); i++)
        {
            var page = reader.GetPage(i);
            var layers = page.GetContent()?.GetLayers() ?? new List<Ofd.Core.BasicStructure.PageObj.Layer.CT_Layer>();
            sb.AppendLine($"page{i}.layers={layers.Count}");

            foreach (var layer in layers)
            {
                sb.AppendLine($"page{i}.layer[{layer.GetType()}].blocks={layer.GetPageBlocks().Count}");
                Walk(layer.GetPageBlocks(), sb, $"page{i}");
            }
        }

        return sb.ToString();

        static void Walk(List<Ofd.Core.BasicStructure.PageObj.Layer.IPageBlockType> blocks,
            StringBuilder sb, string prefix)
        {
            foreach (var block in blocks)
            {
                switch (block)
                {
                    case TextObject text:
                    {
                        string content = string.Concat(text.GetTextCodes().Select(c => c.GetContent() ?? ""));
                        var boundary = text.GetBoundary();
                        sb.AppendLine($"{prefix}.text \"{content}\" size={F(text.GetSize())} " +
                                      $"bnd={Box(boundary)} font={(text.GetFont() != null ? "true" : "false")} " +
                                      $"codes={text.GetTextCodes().Count}");
                        break;
                    }
                    case PathObject path:
                    {
                        sb.AppendLine($"{prefix}.path data=\"{path.GetAbbreviatedDataText()}\" " +
                                      $"bnd={Box(path.GetBoundary())} stroke={(path.GetStroke() ? "true" : "false")} fill={(path.GetFill() ? "true" : "false")}");
                        break;
                    }
                    case CT_PageBlock pageBlock:
                        sb.AppendLine($"{prefix}.pageblock children={pageBlock.GetPageBlocks().Count}");
                        Walk(pageBlock.GetPageBlocks(), sb, prefix + "/pb");
                        break;
                    default:
                        sb.AppendLine($"{prefix}.other {block.GetType().Name}");
                        break;
                }
            }
        }
    }

    /// <summary>统一换行符，避免 Windows/Linux 与 Java 侧差异造成假失败。</summary>
    /// <summary>按行拆分（统一换行风格、去空行、去行尾空白），用于跨平台/跨语言比较。</summary>
    private static string[] Lines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => l.Length > 0)
            .ToArray();

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Box(Ofd.Core.BasicType.ST_Box? box) =>
        box == null
            ? "none"
            : $"{F(box.TopLeftX)},{F(box.TopLeftY)},{F(box.Width)},{F(box.Height)}";

    /// <summary>转换后包结构签名：条目名 + DocType（排除 ID/时间敏感内容）。</summary>
    private static string ArchiveSignature(string ofd)
    {
        var sb = new StringBuilder();
        using var reader = new OFDReader(ofd);
        sb.AppendLine($"docType={reader.GetOFDDir().GetOfd().GetDocType()}");
        sb.AppendLine($"hasSignatures={reader.GetDefaultSignatures()?.GetSignatures().Count > 0}");

        using var zip = ZipFile.OpenRead(ofd);
        var names = zip.Entries.Select(e => e.FullName).OrderBy(n => n, StringComparer.Ordinal);
        foreach (var name in names) sb.AppendLine("entry=" + name);

        return sb.ToString();
    }

    /// <summary>把基准文件解析为「段落名 → 内容」。</summary>
    private static Dictionary<string, string> ReadSections(string file)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(file)) return result;

        string? current = null;
        var body = new StringBuilder();

        foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
        {
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                if (current != null) result[current] = body.ToString().TrimEnd('\n');
                current = line[4..].Trim();
                body.Clear();
            }
            else if (current != null)
            {
                body.AppendLine(line);
            }
        }

        if (current != null) result[current] = body.ToString().TrimEnd('\n');
        return result;
    }

    private void AssertGolden(string actual, string section)
    {
        string header = $"### {section}\n";
        bool update = Environment.GetEnvironmentVariable("OFDNET_UPDATE_GOLDEN") == "1";

        if (update)
        {
            // 更新模式：解析已有段落 → 覆盖当前段 → 按固定顺序重写（保证与测试执行顺序无关）
            var sections = ReadSections(_goldenFile);
            sections[section] = actual.TrimEnd('\n');

            string[] order = { "layout", "archive" };
            var sb = new StringBuilder();
            foreach (var name in order)
            {
                if (!sections.TryGetValue(name, out var body)) continue;
                sb.Append("### ").Append(name).Append('\n').Append(body).Append("\n\n");
            }

            File.WriteAllText(_goldenFile, sb.ToString().TrimEnd('\n') + "\n", Encoding.UTF8);
            return;
        }

        Assert.True(File.Exists(_goldenFile), $"缺少 golden 基准文件: {_goldenFile}");
        string golden = File.ReadAllText(_goldenFile, Encoding.UTF8);

        int start = golden.IndexOf(header, StringComparison.Ordinal);
        Assert.True(start >= 0, $"基准中缺少段落 [{section}]");

        int bodyStart = start + header.Length;
        int end = golden.IndexOf("### ", bodyStart, StringComparison.Ordinal);
        string expected = (end < 0 ? golden[bodyStart..] : golden[bodyStart..end]).TrimEnd('\n');

        Assert.Equal(Lines(expected), Lines(actual));
    }

    [Fact]
    public void Layout_matches_java_baseline()
    {
        // 与 tools/javaprobe/Probe.java 完全相同的场景（仅文本，无画布）
        string path = Path.Combine(_root, "java-parity.ofd");

        using (var doc = new OFDDoc(path))
        {
            var p1 = new Paragraph(80, null);
            p1.Add(new Span("Golden测试").SetFontSize(5).SetFont(Ofd.Font.FontName.TimesNewRoman.Font()));
            doc.Add(p1);

            var p2 = new Paragraph(60, null);
            p2.Add(new Span("second line").SetFontSize(4).SetFont(Ofd.Font.FontName.TimesNewRoman.Font()));
            doc.Add(p2);
        }

        string baselinePath = Path.Combine(AppContext.BaseDirectory, "TestData", "java-layout-golden.txt");
        Assert.True(File.Exists(baselinePath), "缺少 Java 基线文件: " + baselinePath);

        string expected = string.Join("\n",
            File.ReadAllLines(baselinePath, Encoding.UTF8)
                .Where(l => !l.StartsWith('#'))
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0));

        Assert.Equal(Lines(expected), Lines(Signature(path)));
    }

    /// <summary>注释产物签名：注释相关条目 + 逐注释的类型/可见性/只读/边界。</summary>
    private static string AnnotationSignature(string ofd)
    {
        var sb = new StringBuilder();

        using (var zip = ZipFile.OpenRead(ofd))
        {
            foreach (var name in zip.Entries.Select(e => e.FullName)
                         .Where(n => n.Contains("Annot"))
                         .OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.Append("entry=").Append(name).Append('\n');
            }
        }

        using var reader = new OFDReader(ofd);
        foreach (var entity in reader.GetAnnotationEntities())
        {
            var annots = entity.GetAnnots();
            sb.Append("page=").Append(entity.GetPageId())
                .Append(" annots=").Append(annots.Count).Append('\n');

            foreach (var annot in annots)
            {
                sb.Append("annot type=").Append(annot.GetType())
                    .Append(" visible=").Append(annot.GetVisible() ? "true" : "false")
                    .Append(" readonly=").Append(annot.GetReadOnly() == true ? "true" : "false")
                    .Append(" bnd=").Append(Box(annot.GetAppearance()?.GetBoundary()))
                    .Append('\n');
            }
        }

        return sb.ToString();
    }

    /// <summary>注释条目 + 每个注释的类型/可见性/外观块数/边界（用于水印对拍）。</summary>
    private static string AnnotationSummary(string ofd)
    {
        var sb = new StringBuilder();

        using (var zip = ZipFile.OpenRead(ofd))
        {
            foreach (var name in zip.Entries.Select(e => e.FullName)
                         .Where(n => n.Contains("Annot"))
                         .OrderBy(n => n, StringComparer.Ordinal))
            {
                sb.Append("entry=").Append(name).Append('\n');
            }
        }

        using var reader = new OFDReader(ofd);
        foreach (var entity in reader.GetAnnotationEntities())
        {
            var annots = entity.GetAnnots();
            sb.Append("page=").Append(entity.GetPageId())
                .Append(" annots=").Append(annots.Count).Append('\n');

            foreach (var annot in annots)
            {
                var appearance = annot.GetAppearance();
                sb.Append("annot type=").Append(annot.GetType())
                    .Append(" visible=").Append(annot.GetVisible() ? "true" : "false")
                    .Append(" blocks=").Append(appearance?.GetPageBlocks().Count ?? 0)
                    .Append(" bnd=").Append(Box(appearance?.GetBoundary()))
                    .Append('\n');
            }
        }

        return sb.ToString();
    }

    /// <summary>签名包结构摘要（不含证书/时间/ID，便于跨实现比对）。</summary>
    private static string SignatureSummary(string ofd)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        using (var zip = ZipFile.OpenRead(ofd))
        {
            foreach (var e in zip.Entries)
            {
                if (string.IsNullOrEmpty(e.Name)) continue;

                using var stream = e.Open();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                entries[e.FullName] = reader.ReadToEnd();
            }
        }

        var sb = new StringBuilder();
        foreach (var name in entries.Keys.Where(n => n.Contains("Sign")).OrderBy(n => n, StringComparer.Ordinal))
            sb.Append("entry=").Append(name).Append('\n');

        entries.TryGetValue("Doc_0/Signs/Signatures.xml", out var sigList);
        entries.TryGetValue("Doc_0/Signs/Sign_0/Signature.xml", out var sig);
        sigList ??= "";
        sig ??= "";

        sb.Append("signatures=").Append(Count(sigList, "<ofd:Signature "))
            .Append(" type=").Append(First(sigList, "Type=\"([^\"]+)\""))
            .Append(" provider=").Append(First(sig, "ProviderName=\"([^\"]+)\""))
            .Append(" method=").Append(First(sig, "SignatureMethod>([^<]+)<"))
            .Append(" checkMethod=").Append(First(sig, "CheckMethod=\"([^\"]+)\""))
            .Append(" references=").Append(Count(sig, "<ofd:Reference "))
            .Append(" signedValue=").Append(entries.ContainsKey("Doc_0/Signs/Sign_0/SignedValue.dat") ? 1 : 0)
            .Append(" seal=").Append(entries.ContainsKey("Doc_0/Signs/Sign_0/Seal.esl") ? 1 : 0)
            .Append(" maxSignId=").Append(First(sigList, "MaxSignId>([^<]+)<"))
            .Append('\n');

        return sb.ToString();
    }

    /// <summary>加密包结构摘要（条目名 + Encryptions.xml 关键字段）。</summary>
    private static string EncryptionSummary(string ofd)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        using (var zip = ZipFile.OpenRead(ofd))
        {
            foreach (var e in zip.Entries)
            {
                if (string.IsNullOrEmpty(e.Name)) continue;

                using var stream = e.Open();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                entries[e.FullName] = reader.ReadToEnd();
            }
        }

        var sb = new StringBuilder();
        foreach (var name in entries.Keys.OrderBy(n => n, StringComparer.Ordinal))
            sb.Append("entry=").Append(name).Append('\n');

        string enc = entries.TryGetValue("Encryptions.xml", out var xml) ? xml : "";

        sb.Append("encryptInfos=").Append(Count(enc, "<ofd:EncryptInfo "))
            .Append(" scope=").Append(First(enc, "EncryptScope>([^<]+)<"))
            .Append(" provider=").Append(First(enc, "Provider [^>]*Name=\"([^\"]+)\""))
            .Append(" company=").Append(First(enc, "Provider [^>]*Company=\"([^\"]+)\""))
            .Append(" seed=").Append(First(enc, "DecryptSeedLoc>([^<]+)<"))
            .Append(" map=").Append(First(enc, "EntriesMapLoc>([^<]+)<"))
            .Append('\n');

        return sb.ToString();
    }

    [Fact]
    public void Encryption_matches_java_baseline()
    {
        string plain = Path.Combine(_root, "enc-src.ofd");
        using (var doc = new OFDDoc(plain))
            doc.Add(new Paragraph("enc", 5).SetDefaultFont(Ofd.Font.FontName.TimesNewRoman.Font(), true));

        string encrypted = Path.Combine(_root, "enc-out.ofd");
        using (var encryptor = new Ofd.Crypto.OFDEncryptor(plain, encrypted))
        {
            encryptor.AddUser(new Ofd.Crypto.Encryptor.UserPasswordEncryptor("张三", "12345678"));
            encryptor.Encrypt();
        }

        string baselinePath = Path.Combine(AppContext.BaseDirectory, "TestData", "java-encryption-golden.txt");
        Assert.True(File.Exists(baselinePath), "缺少 Java 加密基线: " + baselinePath);

        string expected = string.Join("\n",
            File.ReadAllLines(baselinePath, Encoding.UTF8)
                .Where(l => !l.StartsWith('#'))
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0));

        Assert.Equal(Lines(expected), Lines(EncryptionSummary(encrypted)));
    }

    /// <summary>签章包结构摘要（SES V4：Signs 条目 + Seal 元素/文件 + 引用数）。</summary>
    private static string SealSummary(string ofd)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        using (var zip = ZipFile.OpenRead(ofd))
        {
            foreach (var e in zip.Entries)
            {
                if (string.IsNullOrEmpty(e.Name)) continue;

                using var stream = e.Open();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                entries[e.FullName] = reader.ReadToEnd();
            }
        }

        var sb = new StringBuilder();
        foreach (var name in entries.Keys.Where(n => n.Contains("Sign")).OrderBy(n => n, StringComparer.Ordinal))
            sb.Append("entry=").Append(name).Append('\n');

        entries.TryGetValue("Doc_0/Signs/Signatures.xml", out var sigList);
        entries.TryGetValue("Doc_0/Signs/Sign_0/Signature.xml", out var sig);
        sigList ??= "";
        sig ??= "";

        sb.Append("signatures=").Append(Count(sigList, "<ofd:Signature "))
            .Append(" type=").Append(First(sigList, "Type=\"([^\"]+)\""))
            .Append(" hasSealElement=").Append(sig.Contains("<ofd:Seal") ? 1 : 0)
            .Append(" sealFile=").Append(entries.ContainsKey("Doc_0/Signs/Sign_0/Seal.esl") ? 1 : 0)
            .Append(" references=").Append(Count(sig, "<ofd:Reference "))
            .Append(" provider=").Append(First(sig, "ProviderName=\"([^\"]+)\""))
            .Append('\n');

        return sb.ToString();
    }

    [Fact]
    public void Seal_matches_java_baseline()
    {
        string plain = Path.Combine(_root, "seal-src.ofd");
        using (var doc = new OFDDoc(plain))
            doc.Add(new Paragraph("seal", 5).SetDefaultFont(Ofd.Font.FontName.TimesNewRoman.Font(), true));

        var (priv, cert) = Ofd.Tests.Sign.SignTestKit.NewSm2Cert();
        var seal = Ofd.Tests.Sign.SignTests.BuildSealV4(cert, priv);

        string signed = Path.Combine(_root, "seal-out.ofd");
        using (var reader = new OFDReader(plain))
        using (var signer = new Ofd.Sign.OFDSigner(reader, signed))
        {
            signer.SetSignContainer(new Ofd.Sign.SignContainer.SESV4Container(priv, seal, cert)).ExeSign();
        }

        string baselinePath = Path.Combine(AppContext.BaseDirectory, "TestData", "java-seal-golden.txt");
        Assert.True(File.Exists(baselinePath), "缺少 Java 签章基线: " + baselinePath);

        string expected = string.Join("\n",
            File.ReadAllLines(baselinePath, Encoding.UTF8)
                .Where(l => !l.StartsWith('#'))
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0));

        Assert.Equal(Lines(expected), Lines(SealSummary(signed)));
    }

    [Fact]
    public void Signature_matches_java_baseline()
    {
        string plain = Path.Combine(_root, "sig-src.ofd");
        using (var doc = new OFDDoc(plain))
            doc.Add(new Paragraph("sig", 5).SetDefaultFont(Ofd.Font.FontName.TimesNewRoman.Font(), true));

        var (priv, cert) = Ofd.Tests.Sign.SignTestKit.NewSm2Cert();

        string signed = Path.Combine(_root, "sig-out.ofd");
        using (var reader = new OFDReader(plain))
        using (var signer = new Ofd.Sign.OFDSigner(reader, signed))
        {
            signer.SetSignContainer(new Ofd.Sign.SignContainer.GBT35275DSContainer(cert, priv)).ExeSign();
        }

        string baselinePath = Path.Combine(AppContext.BaseDirectory, "TestData", "java-signature-golden.txt");
        Assert.True(File.Exists(baselinePath), "缺少 Java 签名基线: " + baselinePath);

        string expected = string.Join("\n",
            File.ReadAllLines(baselinePath, Encoding.UTF8)
                .Where(l => !l.StartsWith('#'))
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0));

        string actualSummary = SignatureSummary(signed);
        Assert.Equal(Lines(expected), Lines(actualSummary));
    }

    private static int Count(string text, string needle)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }

    private static string First(string text, string pattern)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, pattern);
        return match.Success ? match.Groups[1].Value : "none";
    }

    [Fact]
    public void Watermark_matches_java_baseline()
    {
        string baseDoc = Path.Combine(_root, "wm-base.ofd");
        using (var doc = new OFDDoc(baseDoc)) doc.Add(new Paragraph("wm", 5));

        string withWatermark = Path.Combine(_root, "wm-out.ofd");
        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, withWatermark))
        {
            var wm = new Ofd.Layout.Edit.Watermark(PageLayout.A4())
                .SetValue("CONF")
                .SetGlobalAlpha(0.2)
                .SetAngle(30)
                .SetIntervalX(40)
                .SetIntervalY(40);

            doc.AddWatermark(wm);
        }

        string baselinePath = Path.Combine(AppContext.BaseDirectory, "TestData", "java-watermark-golden.txt");
        Assert.True(File.Exists(baselinePath), "缺少 Java 水印基线: " + baselinePath);

        string expected = string.Join("\n",
            File.ReadAllLines(baselinePath, Encoding.UTF8)
                .Where(l => !l.StartsWith('#'))
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0));

        Assert.Equal(Lines(expected), Lines(AnnotationSummary(withWatermark)));
    }

    [Fact]
    public void Annotation_matches_java_baseline()
    {
        // 1) 基础文本文档
        string baseDoc = Path.Combine(_root, "annot-base.ofd");
        using (var doc = new OFDDoc(baseDoc)) doc.Add(new Paragraph("annot", 5));

        // 2) 修改模式写入注释（与 Java 探针同参数）
        string withAnnot = Path.Combine(_root, "annot-out.ofd");
        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, withAnnot))
        {
            var annot = new Ofd.Layout.Edit.Annotation(
                    new Ofd.Core.BasicType.ST_Box(10, 10, 20, 20),
                    Ofd.Core.Annotation.PageAnnot.AnnotType.Path,
                    new Ofd.Layout.Element.Canvas.Line(20, 20).SetBeginPoint(0, 0).SetEndPoint(10, 10))
                .SetCreator("probe");

            doc.AddAnnotation(1, annot);
        }

        string baselinePath = Path.Combine(AppContext.BaseDirectory, "TestData", "java-annotation-golden.txt");
        Assert.True(File.Exists(baselinePath), "缺少 Java 注释基线: " + baselinePath);

        string expected = string.Join("\n",
            File.ReadAllLines(baselinePath, Encoding.UTF8)
                .Where(l => !l.StartsWith('#'))
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0));

        Assert.Equal(Lines(expected), Lines(AnnotationSignature(withAnnot)));
    }

    [Fact]
    public void Layout_output_matches_structural_golden()
    {
        string ofd = BuildDoc("golden-layout.ofd");
        AssertGolden(Signature(ofd), "layout");
    }

    [Fact]
    public void Archive_conversion_matches_package_golden()
    {
        string src = BuildDoc("golden-archive-src.ofd");
        string dst = Path.Combine(_root, "golden-archive.ofd");
        new OFDArchiveConverter().Convert(src, dst);

        AssertGolden(ArchiveSignature(dst), "archive");
    }
}
