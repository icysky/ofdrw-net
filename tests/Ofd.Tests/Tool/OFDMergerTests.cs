using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Reader;
using Ofd.Tool.Merge;
using Xunit;

namespace Ofd.Tests.Tool;

/// <summary>文档合并：多文档追加、选页、页面混合与参数校验。</summary>
public class OFDMergerTests : IDisposable
{
    private readonly string _root;

    public OFDMergerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_merge_" + Guid.NewGuid().ToString("N")[..8]);
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

    /// <summary>构造含指定文本的文档（每段后跟一个 200mm 块，确保分页）。</summary>
    private string BuildDoc(string name, params string[] texts)
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);
        foreach (var t in texts)
        {
            // 字号 4mm、固定高度 240mm：下行行高 7mm 已放不下 → 强制换页，一段独占一页
            doc.Add(new Paragraph(t, 4).SetHeight(240));
        }
        return path;
    }

    private static List<string> PageTexts(OFDReader reader, int pageIndex)
    {
        var page = reader.GetPage(pageIndex);
        return page.GetContent()!.GetLayers()
            .SelectMany(l => l.GetPageBlocks())
            .SelectMany(b => b switch
            {
                TextObject t => t.GetTextCodes().Select(c => c.GetContent() ?? ""),
                CT_PageBlock pb => pb.GetPageBlocks().OfType<TextObject>()
                    .SelectMany(t => t.GetTextCodes()).Select(c => c.GetContent() ?? ""),
                _ => Enumerable.Empty<string>(),
            })
            .ToList();
    }

    /// <summary>汇总全文档所有文本。</summary>
    private static List<string> AllTexts(OFDReader reader)
    {
        var all = new List<string>();
        for (int i = 1; i <= reader.GetNumberOfPages(); i++) all.AddRange(PageTexts(reader, i));
        return all;
    }

    [Fact]
    public void Merge_two_documents_concatenates_pages()
    {
        string a = BuildDoc("a.ofd", "A1", "A2");
        string b = BuildDoc("b.ofd", "B1");
        string dest = Path.Combine(_root, "merged.ofd");

        using (var merger = new OFDMerger(dest))
        {
            merger.Add(a);
            merger.Add(b);
        }

        using var reader = new OFDReader(dest);
        Assert.True(reader.GetNumberOfPages() >= 2);

        var all = AllTexts(reader);
        Assert.Contains("A1", all);
        Assert.Contains("A2", all);
        Assert.Contains("B1", all);
    }

    [Fact]
    public void Merge_selected_page_only()
    {
        string a = BuildDoc("sel.ofd", "P1", "P2");
        string dest = Path.Combine(_root, "sel-out.ofd");

        using (var merger = new OFDMerger(dest))
            merger.Add(a, 2);

        using var reader = new OFDReader(dest);
        Assert.Equal(1, reader.GetNumberOfPages());

        var texts = PageTexts(reader, 1);
        Assert.Contains("P2", texts);
        Assert.DoesNotContain("P1", texts);
    }

    [Fact]
    public void Merge_mix_overlays_two_pages_into_one()
    {
        string baseDoc = BuildDoc("mix-a.ofd", "BASE");
        string overDoc = BuildDoc("mix-b.ofd", "OVER");
        string dest = Path.Combine(_root, "mix-out.ofd");

        using (var merger = new OFDMerger(dest))
            merger.AddMix(baseDoc, 1, overDoc, 1);

        using var reader = new OFDReader(dest);
        Assert.Equal(1, reader.GetNumberOfPages());

        var texts = PageTexts(reader, 1);
        Assert.Contains("BASE", texts);
        Assert.Contains("OVER", texts);
    }

    [Fact]
    public void Merger_validates_destination()
    {
        Assert.Throws<ArgumentException>(() => new OFDMerger(null));
        Assert.Throws<ArgumentException>(() => new OFDMerger(Path.Combine(_root, "no-such", "x.ofd")));
    }

    [Fact]
    public void Empty_merger_writes_nothing()
    {
        string dest = Path.Combine(_root, "empty.ofd");
        using (var merger = new OFDMerger(dest)) { }

        Assert.False(File.Exists(dest)); // 无页面时不生成文件
    }
}
