using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.Text;
using Ofd.Core.Text.Font;
using Ofd.Layout;
using Ofd.Font;
using Ofd.Layout.Element;
using Ofd.Reader;
using Xunit;
using FontType = Ofd.Font.Font;

namespace Ofd.Tests.Layout;

/// <summary>文档内容替换：单页/整篇替换、DeltaX 重算、扩展处理器、模式约束。</summary>
public class DocContentReplaceTests : IDisposable
{
    private readonly string _root;

    public DocContentReplaceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_replace_" + Guid.NewGuid().ToString("N")[..8]);
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
        foreach (var t in texts)
            doc.Add(new Paragraph(t, 5));
        return path;
    }

    private static List<TextObject> Texts(Ofd.Core.BasicStructure.PageObj.PageObj page) =>
        page.GetContent()!.GetLayers()
            .SelectMany(l => l.GetPageBlocks().OfType<TextObject>())
            .ToList();

    [Fact]
    public void Replace_text_on_single_page_updates_content_and_delta()
    {
        string baseDoc = BuildDoc("a.ofd", "Hello");
        string outPath = Path.Combine(_root, "a-out.ofd");

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            var replace = new DocContentReplace(doc);
            replace.ReplaceText(1, new Dictionary<string, string> { ["Hello"] = "World" });
        }

        using var reader2 = new OFDReader(outPath);
        var texts = Texts(reader2.GetPage(1));
        var code = Assert.Single(texts).GetTextCodes()[0];

        Assert.Equal("World", code.GetContent());
        // DeltaX 长度 = 字符数 - 1
        Assert.Equal(4, code.GetDeltaX()!.Size());
    }

    [Fact]
    public void Replace_all_pages_when_no_page_number()
    {
        string baseDoc = BuildDoc("b.ofd", "AAA", "AAA");
        string outPath = Path.Combine(_root, "b-out.ofd");

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            new DocContentReplace(doc).ReplaceText(new Dictionary<string, string> { ["AAA"] = "BB" });
        }

        using var reader2 = new OFDReader(outPath);
        Assert.True(reader2.GetNumberOfPages() >= 1);

        foreach (var page in reader2.GetPageList())
        {
            foreach (var t in Texts(page.GetObj()!))
                Assert.Equal("BB", t.GetTextCodes()[0].GetContent());
        }
    }

    [Fact]
    public void Unknown_key_leaves_text_unchanged()
    {
        string baseDoc = BuildDoc("c.ofd", "Keep");
        string outPath = Path.Combine(_root, "c-out.ofd");

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            new DocContentReplace(doc).ReplaceText(1, new Dictionary<string, string> { ["缺失"] = "X" });
        }

        using var reader2 = new OFDReader(outPath);
        Assert.Equal("Keep", Assert.Single(Texts(reader2.GetPage(1))).GetTextCodes()[0].GetContent());
    }

    [Fact]
    public void Custom_handler_provides_font_for_delta_calculation()
    {
        string baseDoc = BuildDoc("d.ofd", "abcd");
        string outPath = Path.Combine(_root, "d-out.ofd");

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            var handler = new TimesHandler();
            var replace = new DocContentReplace(doc, handler);
            replace.ReplaceText(1, new Dictionary<string, string> { ["abcd"] = "abcdef" });
        }

        using var reader2 = new OFDReader(outPath);
        var code = Assert.Single(Texts(reader2.GetPage(1))).GetTextCodes()[0];
        Assert.Equal("abcdef", code.GetContent());
        Assert.Equal(5, code.GetDeltaX()!.Size());
    }

    [Fact]
    public void Replace_requires_modify_mode()
    {
        string outPath = Path.Combine(_root, "new.ofd");
        using var doc = new OFDDoc(outPath);
        doc.Add(new Paragraph("x", 4));

        var replace = new DocContentReplace(doc);
        Assert.Throws<InvalidOperationException>(() => replace.GetReader());
    }

    [Fact]
    public void Null_doc_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DocContentReplace(null!));
    }

    private sealed class TimesHandler : IReplaceTextHandler
    {
        public CT_CGTransform? HandleCgTransform(TextObject textObject, string newText, CT_Font? beforeCtFont) => null;

        public FontType? HandleNewFont(TextObject textObject, string newText, CT_Font? beforeCtFont) =>
            Ofd.Font.FontName.TimesNewRoman.Font();
    }
}
