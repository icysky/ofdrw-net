using System.IO.Compression;
using System.Text;
using Ofd.Layout;
using Ofd.Layout.Edit;
using Ofd.Layout.Element;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>多行水印（按 \n 分行）：每个平铺点应输出多行文本。</summary>
public class WatermarkMultiLineTests : IDisposable
{
    private readonly string _root;

    public WatermarkMultiLineTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_wmml_" + Guid.NewGuid().ToString("N")[..8]);
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

    private static Dictionary<string, string> ReadEntries(string ofd)
    {
        using var zip = ZipFile.OpenRead(ofd);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var e in zip.Entries)
        {
            if (string.IsNullOrEmpty(e.Name)) continue;

            using var stream = e.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            result[e.FullName] = reader.ReadToEnd();
        }

        return result;
    }

    [Fact]
    public void Two_line_watermark_emits_both_lines_per_tile()
    {
        string plain = Path.Combine(_root, "p.ofd");
        using (var doc = new OFDDoc(plain)) doc.Add(new Paragraph("内容", 5));

        string marked = Path.Combine(_root, "wm.ofd");
        using (var reader = new OFDReader(plain))
        using (var doc = new OFDDoc(reader, marked))
        {
            var wm = new Watermark(PageLayout.A4())
                .SetValue("第一行水印\n第二行水印")
                .SetGlobalAlpha(0.2)
                .SetAngle(45)
                .SetIntervalX(120)
                .SetIntervalY(120);

            doc.AddWatermark(wm);
        }

        var entries = ReadEntries(marked);
        string annot = entries.Where(kv => kv.Key.Contains("Annot_"))
            .Select(kv => kv.Value).First();

        var codes = System.Text.RegularExpressions.Regex
            .Matches(annot, "<ofd:TextCode[^>]*>([^<]*)</ofd:TextCode>")
            .Select(m => m.Groups[1].Value)
            .ToList();

        int first = codes.Count(c => c == "第一行水印");
        int second = codes.Count(c => c == "第二行水印");

        Assert.True(first > 0, "未输出第一行水印");
        Assert.Equal(first, second); // 每个平铺点两行成对出现
    }
}
