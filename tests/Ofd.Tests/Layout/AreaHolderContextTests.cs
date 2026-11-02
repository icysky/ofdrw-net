using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Layout;
using Ofd.Layout.AreaHolder;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>区域占位上下文：按名称取回占位画布/单元格并向其中写入内容。</summary>
public class AreaHolderContextTests : IDisposable
{
    private readonly string _root;

    public AreaHolderContextTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_ahctx_" + Guid.NewGuid().ToString("N")[..8]);
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

    /// <summary>解析 AreaHolderBlocks.xml 中第一个 Boundary 的 x/y。</summary>
    private static double[] ParseBoundary(string ofdPath)
    {
        string xml = ReadEntries(ofdPath)["Doc_0/AreaHolderBlocks.xml"];
        var m = Regex.Match(xml, @"Boundary=""([-0-9.]+) ([-0-9.]+) ([-0-9.]+) ([-0-9.]+)""");
        Assert.True(m.Success, "未找到 Boundary: " + xml);
        return new[] { double.Parse(m.Groups[1].Value), double.Parse(m.Groups[2].Value) };
    }

    private string BuildDocWithArea(string name, string areaName)
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);
        doc.Add(new Paragraph("first page", 4));
        // 使用流式构造：绝对定位元素会被分段引擎跳过
        doc.Add(new AreaHolderBlock(areaName, 60, 40));
        return path;
    }

    [Fact]
    public void Get_cell_writes_text_into_placeholder_block()
    {
        string baseDoc = BuildDocWithArea("base.ofd", "signArea");
        string outPath = Path.Combine(_root, "filled.ofd");

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            var ctx = new AreaHolderContext(doc);

            var cell = ctx.GetCell("signArea");
            Assert.NotNull(cell);
            cell!.SetValue("内容").SetFontSize(3);
        }

        using var reader2 = new OFDReader(outPath);
        bool found = false;

        foreach (var page in reader2.GetPageList())
        {
            var blocks = page.GetObj()!.GetContent()!.GetLayers()
                .SelectMany(l => l.GetPageBlocks())
                .ToList();

            foreach (var block in blocks.OfType<CT_PageBlock>())
            {
                var texts = block.GetPageBlocks().OfType<TextObject>().ToList();
                if (texts.Count == 0) continue;

                Assert.Equal("内容", texts[0].GetTextCodes()[0].GetContent());
                found = true;
            }
        }

        Assert.True(found, "占位页块中应写入文字");
    }

    [Fact]
    public void Get_returns_canvas_matching_recorded_boundary()
    {
        string baseDoc = BuildDocWithArea("base2.ofd", "area2");
        string outPath = Path.Combine(_root, "canvas.ofd");

        var recorded = ParseBoundary(baseDoc);

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            var ctx = new AreaHolderContext(doc);
            var canvas = ctx.Get("area2");

            Assert.NotNull(canvas);
            Assert.NotNull(canvas!.GetPreferBlock());

            // 画布位置与大小来自区域占位区块记录
            Assert.Equal(recorded[0], canvas.GetX()!.Value, 3);
            Assert.Equal(recorded[1], canvas.GetY()!.Value, 3);
            Assert.Equal(60d, canvas.GetWidth()!.Value, 6);
            Assert.Equal(40d, canvas.GetHeight()!.Value, 6);

            // 改绘制内容同样生效
            canvas.SetDrawer(new Line(60, 40).SetBeginPoint(0, 0).SetEndPoint(20, 20));
        }

        Assert.True(File.Exists(outPath));
    }

    [Fact]
    public void Unknown_area_returns_null()
    {
        string baseDoc = BuildDocWithArea("base3.ofd", "known");
        string outPath = Path.Combine(_root, "unknown.ofd");

        using (var reader = new OFDReader(baseDoc))
        using (var doc = new OFDDoc(reader, outPath))
        {
            var ctx = new AreaHolderContext(doc);
            Assert.Null(ctx.Get("missing"));
            Assert.Null(ctx.GetCell("missing"));
            Assert.Null(ctx.GetCell(null));
        }
    }

    [Fact]
    public void Doc_without_area_holders_returns_null()
    {
        string plain = Path.Combine(_root, "plain.ofd");
        using (var doc = new OFDDoc(plain))
        {
            doc.Add(new Paragraph("no area", 4));
        }

        string outPath = Path.Combine(_root, "noarea.ofd");
        using (var reader = new OFDReader(plain))
        using (var doc = new OFDDoc(reader, outPath))
        {
            var ctx = new AreaHolderContext(doc);
            Assert.Null(ctx.Get("any"));
        }
    }

    [Fact]
    public void Area_holder_file_is_recorded_in_document()
    {
        string baseDoc = BuildDocWithArea("base4.ofd", "recorded");

        var entries = ReadEntries(baseDoc);
        Assert.Contains("Doc_0/AreaHolderBlocks.xml", entries.Keys);
        Assert.Contains("recorded", entries["Doc_0/AreaHolderBlocks.xml"]);
        Assert.Contains("PageBlockID", entries["Doc_0/AreaHolderBlocks.xml"]);
    }
}
