using System.IO.Compression;
using System.Text;
using Ofd.Core.Annotation.PageAnnot;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Layout;
using Ofd.Layout.Edit;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Layout.Engine;
using Ofd.Pkg.Container;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>水印：默认参数、参数校验、平铺绘制数量、整文档水印落位。</summary>
public class WatermarkTests : IDisposable
{
    private readonly string _root;
    private readonly ResManager _res;

    public WatermarkTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_wm_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);

        var ofdDir = new OFDDir(Path.Combine(_root, "ofd"));
        var docDir = ofdDir.NewDoc();
        docDir.SetDocument(new Document().SetCommonData(new CT_CommonData().SetMaxUnitID(100)));
        _res = new ResManager(ofdDir, docDir, new AtomicCounter(100));
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

    private static Dictionary<string, string> ReadEntries(string ofdPath)
    {
        var res = new Dictionary<string, string>(StringComparer.Ordinal);
        using var zip = ZipFile.OpenRead(ofdPath);
        foreach (var e in zip.Entries)
        {
            using var ms = new MemoryStream();
            using var s = e.Open();
            s.CopyTo(ms);
            res[e.FullName] = Encoding.UTF8.GetString(ms.ToArray());
        }
        return res;
    }

    [Fact]
    public void Watermark_defaults_match_java()
    {
        var wm = new Watermark(PageLayout.A4());

        Assert.Equal(AnnotType.Watermark, wm.GetType());
        Assert.Equal(4.5d, wm.GetFontSize(), 6);
        Assert.Equal("#000000", wm.GetColor());
        Assert.Equal("宋体", wm.GetFontName());
        Assert.Equal(0.5d, wm.GetGlobalAlpha());
        Assert.Equal(330d, wm.GetAngle());
        Assert.Equal(30d, wm.GetIntervalX());
        Assert.Equal(30d, wm.GetIntervalY());
        Assert.False(wm.GetBold());
        Assert.False(wm.GetItalic());
        Assert.Equal(0d, wm.GetLetterSpacing());
        Assert.Null(wm.GetValue());

        // 画布比页面大 50mm
        var boundary = wm.GetBoundary();
        Assert.Equal(260d, boundary.Width, 6);
        Assert.Equal(347d, boundary.Height, 6);

        // 绘制器自绑定
        Assert.NotNull(wm.GetDrawer());
        Assert.Same(wm, wm.GetDrawer().GetAnnotation());
    }

    [Fact]
    public void Watermark_validates_alpha_and_angle()
    {
        var wm = new Watermark(0, 0, 100, 100);

        Assert.Throws<ArgumentException>(() => wm.SetGlobalAlpha(1.5));
        Assert.Throws<ArgumentException>(() => wm.SetGlobalAlpha(-0.1));
        Assert.Throws<ArgumentException>(() => wm.SetAngle(400));

        // 负角度转为 360+angle
        wm.SetAngle(-30);
        Assert.Equal(330d, wm.GetAngle());

        Assert.Throws<ArgumentException>(() => wm.SetFont("", null));
        Assert.Throws<ArgumentException>(() => wm.SetFont("X", Path.Combine(_root, "none.ttf")));
    }

    [Fact]
    public void Watermark_tiles_text_by_interval()
    {
        // 边界 60x60，间距 30 → x=0,30 与 y=0,30 → 4 个水印
        var wm = new Watermark(0, 0, 60, 60)
            .SetValue("机密")
            .SetAngle(0)
            .SetGlobalAlpha(0.4);

        var container = new CT_PageBlock();
        using var ctx = new DrawContext(container, new ST_Box(0, 0, 60, 60), new AtomicCounter(100), _res);

        wm.GetDrawer().Draw(ctx);

        var texts = container.GetPageBlocks().OfType<TextObject>().ToList();
        Assert.Equal(4, texts.Count);
        Assert.All(texts, t => Assert.Equal("机密", t.GetTextCodes()[0].GetContent()));
        Assert.All(texts, t => Assert.Equal((int)(0.4 * 255), t.GetAlpha()));
    }

    [Fact]
    public void Watermark_without_value_draws_nothing()
    {
        var wm = new Watermark(0, 0, 60, 60);
        var container = new CT_PageBlock();
        using var ctx = new DrawContext(container, new ST_Box(0, 0, 60, 60), new AtomicCounter(100), _res);

        wm.GetDrawer().Draw(ctx);
        Assert.Empty(container.GetPageBlocks());
    }

    [Fact]
    public void Add_watermark_applies_to_all_pages()
    {
        string baseDoc = Path.Combine(_root, "base.ofd");
        using (var doc = new OFDDoc(baseDoc))
        {
            doc.Add(new Div(100, 200).SetClear(Clear.Both).SetIntegrity(true));
            doc.Add(new Div(100, 200).SetClear(Clear.Both).SetIntegrity(true));
        }

        string outPath = Path.Combine(_root, "wm.ofd");
        var watermark = new Watermark(PageLayout.A4()).SetValue("内部资料").SetFontSize(6).SetAngle(0);

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            doc.AddWatermark(watermark);
        }

        var entries = ReadEntries(outPath);
        var annotFiles = entries.Keys.Where(k => k.StartsWith("Doc_0/Annots/") && k.Contains("Annot_")).ToList();
        Assert.Equal(2, annotFiles.Count); // 两页各一个分页注释文件

        foreach (var f in annotFiles)
        {
            Assert.Contains("Watermark", entries[f]);
            Assert.Contains("Appearance", entries[f]);
        }

        Assert.Contains("Doc_0/Annots/Annotations.xml", entries.Keys);
    }

    [Fact]
    public void Add_watermark_requires_modify_mode()
    {
        string outPath = Path.Combine(_root, "direct.ofd");
        using var doc = new OFDDoc(outPath);
        doc.Add(new Paragraph("x", 4));

        Assert.Throws<InvalidOperationException>(() => doc.AddWatermark(new Watermark(0, 0, 10, 10)));
    }
}
