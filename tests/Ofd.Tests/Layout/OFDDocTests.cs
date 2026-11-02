using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>虚拟文档门面：新建文档 → 流式内容 → 打包 → 阅读器读回。</summary>
public class OFDDocTests : IDisposable
{
    private readonly string _root;

    public OFDDocTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_doc_" + Guid.NewGuid().ToString("N")[..8]);
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

    private static List<TextObject> Texts(Ofd.Core.BasicStructure.PageObj.PageObj page) =>
        page.GetContent()!.GetLayers()
            .SelectMany(l => l.GetPageBlocks().OfType<TextObject>())
            .ToList();

    [Fact]
    public void New_doc_with_paragraph_is_packaged_and_read_back()
    {
        string outPath = Path.Combine(_root, "a.ofd");

        using (var doc = new OFDDoc(outPath))
        {
            doc.SetDefaultPageLayout(PageLayout.A4());
            doc.Add(new Paragraph("Hello", 5));
        }

        Assert.True(File.Exists(outPath));

        using var reader = new OFDReader(outPath);
        Assert.Equal(1, reader.GetNumberOfPages());

        var page = reader.GetPage(1);
        var texts = Texts(page);
        Assert.Single(texts);
        Assert.Equal("Hello", texts[0].GetTextCodes()[0].GetContent());
        Assert.Equal(5d, texts[0].GetSize(), 6);
    }

    [Fact]
    public void Multiple_block_elements_produce_multiple_pages_or_segments()
    {
        string outPath = Path.Combine(_root, "b.ofd");

        using (var doc = new OFDDoc(outPath))
        {
            // 版心高 246.2mm：每段 200mm（不可拆分）→ 两段各占一页
            doc.Add(new Div(100, 200).SetClear(Clear.Both).SetIntegrity(true).SetBackgroundColor(1, 2, 3));
            doc.Add(new Div(100, 200).SetClear(Clear.Both).SetIntegrity(true).SetBackgroundColor(4, 5, 6));
        }

        using var reader = new OFDReader(outPath);
        Assert.Equal(2, reader.GetNumberOfPages());
    }

    [Fact]
    public void Canvas_content_is_rendered_into_page()
    {
        string outPath = Path.Combine(_root, "c.ofd");

        using (var doc = new OFDDoc(outPath))
        {
            var canvas = new Canvas(50, 20, new Line(50, 20).SetBeginPoint(0, 0).SetEndPoint(20, 20));
            doc.Add(canvas);
        }

        using var reader = new OFDReader(outPath);
        var page = reader.GetPage(1);
        // Canvas 渲染为页块，页块内含线段路径
        var blocks = page.GetContent()!.GetLayers().SelectMany(l => l.GetPageBlocks()).ToList();
        Assert.Contains(blocks, b => b is CT_PageBlock);
    }

    [Fact]
    public void Empty_doc_throws_on_close()
    {
        string outPath = Path.Combine(_root, "empty.ofd");
        var doc = new OFDDoc(outPath);
        Assert.Throws<InvalidOperationException>(() => doc.Close());
    }

    [Fact]
    public void Ctor_validates_output_path()
    {
        Assert.Throws<ArgumentException>(() => new OFDDoc((string)null!));
        Assert.Throws<ArgumentException>(() => new OFDDoc(_root)); // 目录
        Assert.Throws<ArgumentException>(() => new OFDDoc(Path.Combine(_root, "no-such-dir", "x.ofd")));
        Assert.Throws<ArgumentException>(() => new OFDDoc((Stream)null!));
    }

    [Fact]
    public void Output_can_be_written_to_stream()
    {
        using var ms = new MemoryStream();
        using (var doc = new OFDDoc(ms))
        {
            doc.Add(new Paragraph("Stream", 4));
        }

        Assert.True(ms.Length > 0);

        // 流内容应可被阅读器解析
        using var ms2 = new MemoryStream(ms.ToArray());
        using var reader = new OFDReader(ms2);
        Assert.Equal(1, reader.GetNumberOfPages());
    }

    [Fact]
    public void Add_duplicate_element_is_rejected()
    {
        string outPath = Path.Combine(_root, "dup.ofd");
        using var doc = new OFDDoc(outPath);

        var paragraph = new Paragraph("x", 4);
        doc.Add(paragraph);
        Assert.Throws<ArgumentException>(() => doc.Add(paragraph));
    }

    [Fact]
    public void Modify_mode_exposes_addition_vpage()
    {
        string first = Path.Combine(_root, "m1.ofd");
        using (var doc = new OFDDoc(first))
        {
            doc.Add(new Paragraph("base", 4));
        }

        string second = Path.Combine(_root, "m2.ofd");
        using var reader = new OFDReader(first);
        using var doc2 = new OFDDoc(reader, second);

        var avp = doc2.GetAVPage(1);
        Assert.NotNull(avp);
        Assert.Equal(1, avp.GetPageNum());
        Assert.NotNull(avp.GetPageObj());
        Assert.NotNull(avp.GetPageLoc());

        doc2.Close();
        Assert.True(File.Exists(second));
    }

    [Fact]
    public void Render_finish_handler_and_page_handler_are_invoked()
    {
        string outPath = Path.Combine(_root, "h.ofd");
        int renderFinish = 0;
        int pageHandled = 0;

        using (var doc = new OFDDoc(outPath))
        {
            doc.OnRenderFinish(new LambdaFinishHandler((_, _, _) => renderFinish++));
            doc.OnPage(new LambdaPageHandler(_ => pageHandled++));
            doc.Add(new Paragraph("hook", 4));
        }

        Assert.Equal(1, renderFinish);
        Assert.Equal(1, pageHandled);
    }

    private sealed class LambdaFinishHandler : Ofd.Layout.Handler.IRenderFinishHandler
    {
        private readonly Action<Ofd.Layout.Engine.AtomicCounter, Ofd.Pkg.Container.OFDDir, int> _action;

        public LambdaFinishHandler(Action<Ofd.Layout.Engine.AtomicCounter, Ofd.Pkg.Container.OFDDir, int> action) =>
            _action = action;

        public void Handle(Ofd.Layout.Engine.AtomicCounter maxUnitId, Ofd.Pkg.Container.OFDDir ofdDir, int index) =>
            _action(maxUnitId, ofdDir, index);
    }

    private sealed class LambdaPageHandler : Ofd.Layout.Handler.IVPageHandler
    {
        private readonly Action<VirtualPage> _action;

        public LambdaPageHandler(Action<VirtualPage> action) => _action = action;

        public void Handle(VirtualPage page) => _action(page);
    }
}
