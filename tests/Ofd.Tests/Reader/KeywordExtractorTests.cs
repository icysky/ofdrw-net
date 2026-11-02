using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Reader;
using Ofd.Reader.Keyword;
using Xunit;

namespace Ofd.Tests.Reader;

/// <summary>关键字检索：坐标定位、多关键字、页码过滤与参数校验。</summary>
public class KeywordExtractorTests : IDisposable
{
    private readonly string _root;

    public KeywordExtractorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_kw_" + Guid.NewGuid().ToString("N")[..8]);
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
        foreach (var t in texts) doc.Add(new Paragraph(t, 5));
        return path;
    }

    [Fact]
    public void Find_keyword_returns_position_in_mm()
    {
        string path = BuildDoc("kw.ofd", "关键字测试");
        using var reader = new OFDReader(path);

        var list = KeywordExtractor.GetKeyWordPositionList(reader, "测试");

        var pos = Assert.Single(list);
        Assert.Equal(1, pos.GetPage());
        Assert.Equal("测试", pos.GetKeyword());

        var box = pos.GetBox();
        Assert.True(box.Width > 0, $"width={box.Width}");
        Assert.True(box.Height > 0, $"height={box.Height}");
        // 坐标应落在 A4 版心范围内
        Assert.InRange(box.TopLeftX, 0, 210);
        Assert.InRange(box.TopLeftY, 0, 297);
    }

    [Fact]
    public void Multiple_occurrences_and_multiple_keywords()
    {
        string path = BuildDoc("kw2.ofd", "测试A测试B");
        using var reader = new OFDReader(path);

        var list = KeywordExtractor.GetKeyWordPositionList(reader, new[] { "测试", "A" });

        // "测试" 出现两次 + "A" 一次
        Assert.Equal(3, list.Count);
        Assert.Equal(2, list.Count(p => p.GetKeyword() == "测试"));
        Assert.Equal(1, list.Count(p => p.GetKeyword() == "A"));
    }

    [Fact]
    public void Missing_keyword_returns_empty()
    {
        string path = BuildDoc("kw3.ofd", "一段普通文字");
        using var reader = new OFDReader(path);

        Assert.Empty(KeywordExtractor.GetKeyWordPositionList(reader, "不存在"));
    }

    [Fact]
    public void Page_filter_limits_search_scope()
    {
        // 固定高度 240mm：每段独占一页（字号 5mm 行高 7mm 已放不下剩余空间）
        string path = Path.Combine(_root, "kw4.ofd");
        using (var doc = new OFDDoc(path))
        {
            doc.Add(new Paragraph("第一页", 5).SetHeight(240));
            doc.Add(new Paragraph("第二页", 5).SetHeight(240));
        }
        using var reader = new OFDReader(path);

        var all = KeywordExtractor.GetKeyWordPositionList(reader, "页");
        Assert.Equal(2, all.Count);

        var onlyFirst = KeywordExtractor.GetKeyWordPositionList(reader, "页", new[] { 1 });
        Assert.Single(onlyFirst);
        Assert.Equal(1, onlyFirst[0].GetPage());
    }

    [Fact]
    public void Invalid_page_throws()
    {
        string path = BuildDoc("kw5.ofd", "内容");
        using var reader = new OFDReader(path);

        Assert.Throws<ArgumentException>(() =>
            KeywordExtractor.GetKeyWordPositionList(reader, "内容", new[] { 0 }));
        Assert.Throws<ArgumentException>(() =>
            KeywordExtractor.GetKeyWordPositionList(reader, "内容", new[] { 99 }));
    }
}
