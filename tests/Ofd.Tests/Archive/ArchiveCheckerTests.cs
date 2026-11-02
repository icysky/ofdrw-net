using Ofd.Archive.Check;
using Ofd.Archive.Check.Rule;
using Ofd.Font;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Archive;

/// <summary>OFD-A 检查框架：规则执行、排序、异常隔离与各规则判定。</summary>
public class ArchiveCheckerTests : IDisposable
{
    private readonly string _root;

    public ArchiveCheckerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_arch_" + Guid.NewGuid().ToString("N")[..8]);
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

    private string BuildDoc(string name = "doc.ofd")
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);
        doc.Add(new Paragraph("archive", 4));
        return path;
    }

    [Fact]
    public void Violation_formats_message()
    {
        var v = new ArchiveViolation("DOC_TYPE", ArchiveViolation.Severity.Error, "类型错误",
            Ofd.Core.BasicType.ST_Loc.GetInstance("OFD.xml"), "OFD", "OFD-A");

        string text = v.ToString();
        Assert.Contains("[ERROR]", text);
        Assert.Contains("DOC_TYPE", text);
        Assert.Contains("类型错误", text);
        Assert.Contains("OFD.xml", text);
        Assert.Contains("实际: OFD", text);
        Assert.Contains("期望: OFD-A", text);

        // 必填项校验
        Assert.Throws<ArgumentException>(() => new ArchiveViolation(null!, ArchiveViolation.Severity.Info, "d"));
        Assert.Throws<ArgumentException>(() => new ArchiveViolation("R", ArchiveViolation.Severity.Info, null!));
    }

    [Fact]
    public void Doc_type_rule_reports_missing_type()
    {
        string path = BuildDoc();
        using var reader = new OFDReader(path);
        var violations = new DocTypeRule().Check(reader, reader.GetOFDDir());

        var v = Assert.Single(violations);
        Assert.Equal(DocTypeRule.RULE_NAME, v.GetRuleName());
        Assert.Equal(ArchiveViolation.Severity.Error, v.GetSeverity());
        Assert.Equal("OFD", v.GetActualValue()); // 新建文档默认 DocType=OFD
        Assert.Equal("OFD-A", v.GetExpectedValue());
        Assert.NotNull(v.GetLocation());
    }

    [Fact]
    public void Structural_rules_pass_on_plain_document()
    {
        string path = BuildDoc("plain.ofd");
        using var reader = new OFDReader(path);
        var ofdDir = reader.GetOFDDir();

        Assert.Empty(new SingleDocRule().Check(reader, ofdDir));
        Assert.Empty(new ExternalResourceRule().Check(reader, ofdDir));
        Assert.Empty(new PermissionRule().Check(reader, ofdDir));
        Assert.Empty(new NonGotoActionRule().Check(reader, ofdDir));
        Assert.Empty(new OutlineActionRule().Check(reader, ofdDir));
        Assert.Empty(new ExtensionRule().Check(reader, ofdDir));
        Assert.Empty(new AudioVideoRule().Check(reader, ofdDir));
        Assert.Empty(new ColorSpaceRule().Check(reader, ofdDir));
        Assert.Empty(new ColorProfileRule().Check(reader, ofdDir));
        Assert.Empty(new ImageExtensionRule().Check(reader, ofdDir));
        Assert.Empty(new ImageInterpolateRule().Check(reader, ofdDir));
        Assert.Empty(new TextSizeRule().Check(reader, ofdDir));
    }

    [Fact]
    public void Annotation_signature_attachment_rules_pass_on_plain_doc()
    {
        string path = BuildDoc("plain2.ofd");
        using var reader = new OFDReader(path);
        var ofdDir = reader.GetOFDDir();

        Assert.Empty(new AnnotationRule().Check(reader, ofdDir));
        Assert.Empty(new SignatureRule().Check(reader, ofdDir));
        Assert.Empty(new AttachmentRule().Check(reader, ofdDir));
        Assert.Empty(new ImageResourceRegRule().Check(reader, ofdDir));
    }

    [Fact]
    public void Attachment_rule_accepts_text_and_rejects_binary()
    {
        string txt = Path.Combine(_root, "a.txt");
        File.WriteAllText(txt, "text");
        string pdf = Path.Combine(_root, "b.pdf");
        File.WriteAllText(pdf, "pdf");

        string ok = Path.Combine(_root, "att-ok.ofd");
        using (var doc = new OFDDoc(ok))
        {
            doc.Add(new Paragraph("p", 4));
            doc.AddAttachment(new Ofd.Layout.Edit.Attachment("说明", txt).SetFormat("txt"));
        }

        using (var reader = new OFDReader(ok))
            Assert.Empty(new AttachmentRule().Check(reader, reader.GetOFDDir()));

        string bad = Path.Combine(_root, "att-bad.ofd");
        using (var doc = new OFDDoc(bad))
        {
            doc.Add(new Paragraph("p", 4));
            doc.AddAttachment(new Ofd.Layout.Edit.Attachment("规范", pdf).SetFormat("pdf"));
        }

        using (var reader = new OFDReader(bad))
        {
            var v = Assert.Single(new AttachmentRule().Check(reader, reader.GetOFDDir()));
            Assert.Equal(ArchiveViolation.Severity.Error, v.GetSeverity());
            Assert.Equal("TXT/XML", v.GetExpectedValue());
        }
    }

    /// <summary>构造显式使用系统字体（未嵌入）的文档，用于触发 FONT_SUBSET 规则。</summary>
    private string BuildDocWithSystemFont(string name = "sysfont.ofd")
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);

        // 显式把默认字体替换为系统宋体（无字体文件 → 未嵌入）
        doc.Add(new Paragraph("系统字体", 4).SetDefaultFont(FontName.SimSun.Font(), true));

        return path;
    }

    [Fact]
    public void Checker_runs_default_rules_and_sorts()
    {
        // 默认字体已改为随包 Noto 子集（内嵌）；这里显式用系统字体才能触发 FONT_SUBSET
        string path = BuildDocWithSystemFont("check.ofd");

        var checker = new OFDArchiveChecker();
        var violations = checker.Check(path);

        // DocType 非 OFD-A 且系统字体未嵌入 → 至少两类违规
        Assert.Contains(violations, v => v.GetRuleName() == DocTypeRule.RULE_NAME);
        Assert.Contains(violations, v => v.GetRuleName() == FontSubsetRule.RULE_NAME);

        // 排序稳定
        Assert.Equal(violations.Select(v => v.GetRuleName()).OrderBy(n => n, StringComparer.Ordinal),
            violations.Select(v => v.GetRuleName()));
    }

    [Fact]
    public void Font_subset_rule_reports_unembedded_font()
    {
        string path = BuildDocWithSystemFont("font.ofd");
        using var reader = new OFDReader(path);
        var violations = new FontSubsetRule().Check(reader, reader.GetOFDDir());

        // 显式使用系统字体、未嵌入 → 应报告
        Assert.NotEmpty(violations);
        Assert.All(violations, v => Assert.Equal(ArchiveViolation.Severity.Error, v.GetSeverity()));
        Assert.Contains(violations, v => v.GetDescription().Contains("未嵌入字型文件"));
    }

    [Fact]
    public void Checker_isolates_rule_exceptions()
    {
        var checker = new OFDArchiveChecker(new List<IArchiveRule>
        {
            new ThrowingRule(),
            new DocTypeRule(),
        });

        string path = BuildDoc("boom.ofd");
        var violations = checker.Check(path);

        Assert.Equal(2, violations.Count);
        Assert.Contains(violations, v => v.GetDescription().Contains("规则执行异常"));
        Assert.Contains(violations, v => v.GetRuleName() == DocTypeRule.RULE_NAME);
    }

    [Fact]
    public void Custom_rule_set_is_used()
    {
        var checker = new OFDArchiveChecker(new List<IArchiveRule> { new AlwaysOkRule() });
        Assert.Empty(checker.Check(BuildDoc("custom.ofd")));
    }

    private sealed class ThrowingRule : IArchiveRule
    {
        public List<ArchiveViolation> Check(OFDReader reader, Ofd.Pkg.Container.OFDDir ofdDir) =>
            throw new InvalidOperationException("boom");
    }

    private sealed class AlwaysOkRule : IArchiveRule
    {
        public List<ArchiveViolation> Check(OFDReader reader, Ofd.Pkg.Container.OFDDir ofdDir) =>
            new List<ArchiveViolation>();
    }
}
