using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Reader;
using Ofd.Tool.Merge;
using Xunit;

namespace Ofd.Tests.Tool;

/// <summary>Tool 基础：裸文档构造、页面删除器、文档上下文、合并条目。</summary>
public class ToolMergeBasicsTests : IDisposable
{
    private readonly string _root;

    public ToolMergeBasicsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_tool_" + Guid.NewGuid().ToString("N")[..8]);
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

    private string BuildDoc(string name, params string[] texts)
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);
        foreach (var t in texts) doc.Add(new Paragraph(t, 4));
        return path;
    }

    /// <summary>构造指定页数的文档（每页一个 200mm 高不可拆分块）。</summary>
    private string BuildMultiPageDoc(string name, int pages)
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);
        for (int i = 0; i < pages; i++)
            doc.Add(new Div(100, 200).SetClear(Clear.Both).SetIntegrity(true));
        return path;
    }

    [Fact]
    public void Bare_doc_creates_valid_container()
    {
        string outPath = Path.Combine(_root, "bare.ofd");

        using (var doc = new BareOFDDoc(outPath))
        {
            Assert.NotNull(doc.Document);
            Assert.NotNull(doc.DocDir);
            Assert.NotNull(doc.Prm);
            Assert.Equal(0, doc.MaxUnitID.Get());
        }

        Assert.True(File.Exists(outPath));

        // OFD.xml 应可被解析
        using var reader = new OFDReader(outPath);
        Assert.NotNull(reader.GetOFDDir().GetOfd().GetDocBody());
    }

    [Fact]
    public void Bare_doc_validates_output_path()
    {
        Assert.Throws<ArgumentException>(() => new BareOFDDoc(null!));
        Assert.Throws<ArgumentException>(() => new BareOFDDoc(_root));
        Assert.Throws<ArgumentException>(() => new BareOFDDoc(Path.Combine(_root, "no-such", "x.ofd")));
    }

    [Fact]
    public void Page_deleter_removes_page_node()
    {
        string src = BuildMultiPageDoc("del-src.ofd", 2);
        string outPath = Path.Combine(_root, "del-out.ofd");

        using (var reader = new OFDReader(src))
            Assert.Equal(2, reader.GetNumberOfPages());

        using (var deleter = new OFDPageDeleter(src, outPath))
            deleter.Delete(0);

        using var reader2 = new OFDReader(outPath);
        Assert.Equal(1, reader2.GetNumberOfPages());
    }

    [Fact]
    public void Page_deleter_ignores_empty_indexes()
    {
        string src = BuildDoc("del2-src.ofd", "p1");
        string outPath = Path.Combine(_root, "del2-out.ofd");

        using (var deleter = new OFDPageDeleter(src, outPath))
            deleter.Delete();

        using var reader = new OFDReader(outPath);
        Assert.Equal(1, reader.GetNumberOfPages());
    }

    [Fact]
    public void Page_deleter_validates_paths()
    {
        Assert.Throws<ArgumentException>(() => new OFDPageDeleter(Path.Combine(_root, "nope.ofd"), Path.Combine(_root, "o.ofd")));
        Assert.Throws<ArgumentException>(() => new OFDPageDeleter(BuildDoc("v.ofd", "x"), null));
    }

    [Fact]
    public void Doc_context_reads_pages_and_default_area()
    {
        string src = BuildMultiPageDoc("ctx.ofd", 2);

        using var ctx = new DocContext(src);
        Assert.NotNull(ctx.Reader);
        Assert.NotNull(ctx.ResMgt);
        Assert.Equal(2, ctx.Reader.GetNumberOfPages());

        var area = ctx.GetDefaultArea();
        Assert.NotNull(area);
        Assert.Equal(PageLayout.A4().GetPageArea().GetPhysicalBox()!.ToString(),
            area.GetPhysicalBox()!.ToString());
    }

    [Fact]
    public void Page_entry_and_doc_page_hold_values()
    {
        string src = BuildDoc("entry.ofd", "a");
        using var ctx = new DocContext(src);

        var entry = new PageEntry(1, ctx);
        Assert.Same(ctx, entry.DocCtx);
        Assert.Equal(1, entry.PageIndex);
        Assert.True(entry.CopyTemplate);
        Assert.True(entry.CopyAnnotations);
        Assert.Null(entry.TbMixPages);

        var mix = new PageEntry(2, ctx);
        var withMix = new PageEntry(1, ctx, mix);
        Assert.Single(withMix.TbMixPages!);

        var docPage = new DocPage(src, 3);
        Assert.Equal(3, docPage.Index);
        Assert.Equal(src, docPage.Path);
    }
}
