using Ofd.Core.BasicStructure.Doc;
using Ofd.Font;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Layout.Element.Canvas;
using Ofd.Layout.Engine;
using Ofd.Pkg.Container;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>字体设置、文本测量与绘制参数缓存。</summary>
public class CanvasFontAndMeasureTests : IDisposable
{
    private readonly string _root;

    public CanvasFontAndMeasureTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_canvasfont_" + Guid.NewGuid().ToString("N")[..8]);
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

    [Fact]
    public void Font_setting_defaults_and_validation()
    {
        var fs = FontSetting.GetInstance();
        Assert.Equal(5d, fs.GetFontSize());
        Assert.Equal("宋体", fs.GetFont().GetName());
        Assert.Equal(400, fs.GetFontWeight());
        Assert.Equal(0, fs.GetCharDirection());
        Assert.Equal(0, fs.GetReadDirection());
        Assert.Equal(CanvasTextAlign.Start, fs.GetTextAlign());
        Assert.False(fs.IsItalic());
        Assert.Equal(0d, fs.GetLetterSpacing());

        // 加粗 = 800
        fs.SetBold();
        Assert.Equal(800, fs.GetFontWeight());

        // 非法粗细
        Assert.Throws<FormatException>(() => fs.SetFontWeight(850));
        Assert.Throws<FormatException>(() => fs.SetFontWeight(1000));

        // 方向仅允许 0/90/180/270
        Assert.Throws<ArgumentException>(() => fs.SetCharDirection(45));
        Assert.Throws<ArgumentException>(() => fs.SetReadDirection(45));
        fs.SetReadDirection(90);
        Assert.Equal(90, fs.GetReadDirection());

        // 负字间距修正为 0
        fs.SetLetterSpacing(-2);
        Assert.Equal(0d, fs.GetLetterSpacing());
        fs.SetLetterSpacing(1.5);
        Assert.Equal(1.5, fs.GetLetterSpacing());

        // 空字体
        Assert.Throws<ArgumentException>(() => new FontSetting(5, null!));
        Assert.Throws<ArgumentException>(() => fs.SetFont(null!));

        Assert.Equal(8d, FontSetting.GetInstance(8).GetFontSize());
    }

    [Fact]
    public void Font_setting_char_box_uses_width_map_then_metrics()
    {
        // Times New Roman 有宽度映射表 → 查表
        var times = new FontSetting(10, Ofd.Font.FontName.TimesNewRoman.Font());
        var box = times.Box('A');
        Assert.Equal(10 * times.GetFont().GetCharWidthScale('A'), box.Width, 6);
        Assert.Equal(10d, box.Height, 6);
        Assert.Equal(box.Width, times.CharWidth('A'), 6);

        // 宋体无映射 → 使用字体度量
        var simsun = new FontSetting(12, Ofd.Font.FontName.SimSun.Font());
        Assert.True(simsun.CharWidth('中') > 0);
        Assert.True(simsun.Box('中').Height > 0);
    }

    [Fact]
    public void Font_setting_clone_copies_letter_spacing()
    {
        var fs = FontSetting.GetInstance(9)
            .SetBold()
            .SetItalic(true)
            .SetLetterSpacing(0.8)
            .SetReadDirection(180)
            .SetCharDirection(90)
            .SetTextAlign(CanvasTextAlign.Center);

        var clone = fs.Clone();

        Assert.Equal(9d, clone.GetFontSize());
        Assert.Equal(800, clone.GetFontWeight());
        Assert.True(clone.IsItalic());
        Assert.Equal(0.8, clone.GetLetterSpacing()); // 上游遗漏，已修正
        Assert.Equal(180, clone.GetReadDirection());
        Assert.Equal(90, clone.GetCharDirection());
        Assert.Equal(CanvasTextAlign.Center, clone.GetTextAlign());

        // 独立
        clone.SetFontSize(1);
        Assert.Equal(9d, fs.GetFontSize());
    }

    [Fact]
    public void Measure_width_follows_direction_matrix()
    {
        var fs = new FontSetting(10, Ofd.Font.FontName.TimesNewRoman.Font());

        // 阅读 0 / 字符 0
        var body = TextMeasureTool.MeasureWidth("abc", fs);
        Assert.Equal(2, body.Offset.Length);
        Assert.Equal(fs.CharWidth('a'), body.Offset[0], 6);
        Assert.Equal(fs.CharWidth('b'), body.Offset[1], 6);
        Assert.Equal(fs.CharWidth('a') + fs.CharWidth('b') + fs.CharWidth('c'), body.Width, 6);
        Assert.Equal(0, body.FirstCharOffsetX);
        Assert.Equal(0, body.FirstCharOffsetY);

        // 阅读 180 / 字符 0：偏移为负
        var rev = TextMeasureTool.MeasureWidth("abc", fs.Clone().SetReadDirection(180));
        Assert.True(rev.Offset[0] < 0);
        Assert.Equal(-fs.CharWidth('a'), rev.FirstCharOffsetX, 6);

        // 阅读 90 / 字符 0：偏移 = 字号；宽度 = Σ|offset| + 字号 = 2*10 + 10
        var vertical = TextMeasureTool.MeasureWidth("abc", fs.Clone().SetReadDirection(90));
        Assert.Equal(10d, vertical.Offset[0], 6);
        Assert.Equal(30d, vertical.Width, 6);
        Assert.Equal(10d, vertical.FirstCharOffsetY, 6);

        // 单字符与空文本
        var single = TextMeasureTool.MeasureWidth("a", fs);
        Assert.Empty(single.Offset);
        Assert.Equal(fs.CharWidth('a'), single.Width, 6);

        var empty = TextMeasureTool.MeasureWidth("", fs);
        Assert.Empty(empty.Offset);
        Assert.Equal(0, empty.Width);

        Assert.Equal(body.Offset, TextMeasureTool.Measure("abc", fs));
    }

    [Fact]
    public void Measure_area_accumulates_letter_spacing()
    {
        var fs = new FontSetting(10, Ofd.Font.FontName.TimesNewRoman.Font()).SetLetterSpacing(2);

        var area = TextMeasureTool.MeasureArea("abc", fs);
        Assert.Equal(3, area.CharAreas!.Count);
        Assert.Equal(fs.CharWidth('a') + fs.CharWidth('b') + fs.CharWidth('c') + 4, area.Width, 6);
        Assert.Equal(10d, area.Height, 6);
        Assert.Equal(2d, area.LetterSpacing, 6);

        // 竖排（阅读 90）：高度累加（3*10 + 2*2），宽度取字符最大宽（Times 中 b 最宽 = 5）
        var vertical = TextMeasureTool.MeasureArea("abc", fs.Clone().SetReadDirection(90));
        Assert.Equal(3 * 10 + 4, vertical.Height, 6);
        Assert.Equal(5d, vertical.Width, 6);
    }

    [Fact]
    public void Draw_param_cache_invalidates_and_deduplicates()
    {
        var ofdDir = new OFDDir(Path.Combine(_root, "res"));
        var docDir = ofdDir.NewDoc();
        docDir.SetDocument(new Document().SetCommonData(new CT_CommonData().SetMaxUnitID(100)));
        var res = new ResManager(ofdDir, docDir, new AtomicCounter(100));

#pragma warning disable CS0618 // 与上游一致标记为弃用
        var cache = new DrawParamCache();
#pragma warning restore CS0618

        Assert.Equal(new[] { 0, 0, 0 }, cache.GetFillColor());
        Assert.Equal(new[] { 0, 0, 0 }, cache.GetStrokeColor());
        Assert.Null(cache.GetID());

#pragma warning disable CS0618
        var id1 = cache.AddToResource(res);
#pragma warning restore CS0618
        Assert.NotNull(id1);
        Assert.Equal(id1!.Id, cache.GetID()!.Id);

        // 未修改 → 复用缓存 ID（不新增资源）
        int countBefore = res.GetNewResIds().Count;
        Assert.Equal(id1.Id, cache.AddToResource(res)!.Id);
        Assert.Equal(countBefore, res.GetNewResIds().Count);

        // 修改属性 → 缓存失效 → 新资源
#pragma warning disable CS0618
        cache.SetLineWidth(2.5);
        var id2 = cache.AddToResource(res);
#pragma warning restore CS0618
        Assert.NotEqual(id1.Id, id2!.Id);
        Assert.Equal(id2.Id, cache.GetID()!.Id);

        var param = res.DocRes().GetResources().OfType<Ofd.Core.BasicStructure.Res.Resources.DrawParams>()
            .First().GetDrawParams().Last();
        Assert.Equal(2.5, param.GetLineWidth(), 6);

        // 克隆深拷贝颜色数组
#pragma warning disable CS0618
        cache.SetFillColor(new[] { 1, 2, 3 });
        var clone = cache.Clone();
        clone.SetFillColor(new[] { 9, 9, 9 });
#pragma warning restore CS0618
        Assert.Equal(new[] { 1, 2, 3 }, cache.GetFillColor());
        Assert.Equal(new[] { 9, 9, 9 }, clone.GetFillColor());
    }
}
