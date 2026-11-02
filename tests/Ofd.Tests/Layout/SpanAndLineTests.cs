using Ofd.Layout.Element;
using SkiaSharp;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>文本单元（Span / PlaceholderSpan / TxtLineBlock / Img）。</summary>
public class SpanAndLineTests : IDisposable
{
    private readonly string _root;

    public SpanAndLineTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_span_" + Guid.NewGuid().ToString("N")[..8]);
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
    public void Span_defaults_match_java()
    {
        var span = new Span("测试文本");

        Assert.Equal(3d, span.GetFontSize());
        Assert.Equal(0d, span.GetLetterSpacing());
        Assert.True(span.IsFill());
        Assert.False(span.IsBold());
        Assert.False(span.IsItalic());
        Assert.False(span.IsUnderline());
        Assert.Equal(1.2d, span.GetUnderlineOffset(), 6);
        Assert.Equal(0d, span.GetUnderlineWidth(), 6);
        Assert.Equal(4, span.Length());
        Assert.NotNull(span.GetFont());
        Assert.Null(span.GetColor());

        Assert.Throws<ArgumentException>(() => new Span((string)null!));
    }

    [Fact]
    public void Span_glyphs_delta_and_block_size()
    {
        var span = new Span("abc").SetFontSize(10).SetLetterSpacing(1);
        var glyphs = span.GlyphList();

        Assert.Equal(3, glyphs.Count);
        Assert.Same(glyphs, span.GlyphList()); // 缓存

        var delta = span.GetDeltaX();
        Assert.Equal(2, delta.Length);
        Assert.Equal(glyphs[0].GetW(), delta[0], 6);
        Assert.Equal(glyphs[1].GetW(), delta[1], 6);

        var block = span.BlockSize();
        Assert.Equal(glyphs.Sum(g => g.GetW()), block.GetWidth(), 6);
        Assert.Equal(glyphs.Max(g => g.GetH()), block.GetHeight(), 6);

        // 单字符无偏移
        Assert.Empty(new Span("a").GetDeltaX());
    }

    [Fact]
    public void Span_set_text_rebuilds_glyph_cache()
    {
        var span = new Span("abc");
        Assert.Equal(3, span.GlyphList().Count);

        span.SetText("ab");
        Assert.Equal(2, span.GlyphList().Count);
    }

    [Fact]
    public void Span_split_clones_and_validates_index()
    {
        var span = new Span("abcdef").SetFontSize(6).SetColor(1, 2, 3);
        var parts = span.Split(2);

        Assert.Equal("ab", parts[0].GetText());
        Assert.Equal("cdef", parts[1].GetText());
        Assert.Equal(new[] { 1, 2, 3 }, parts[0].GetColor());
        Assert.Equal(6d, parts[0].GetFontSize());

        // 克隆独立
        parts[0].SetColor(9, 9, 9);
        Assert.Equal(new[] { 1, 2, 3 }, span.GetColor());

        Assert.Throws<ArgumentException>(() => span.Split(-1));
        Assert.Throws<ArgumentException>(() => span.Split(6));
    }

    [Fact]
    public void Span_integrity_rule_for_short_text()
    {
        var longSpan = new Span("ab");
        Assert.False(longSpan.IsIntegrity());

        var single = new Span("a");
        Assert.True(single.IsIntegrity()); // 单字符强制不可拆分

        single.SetText("ab");
        Assert.True(single.IsIntegrity()); // 一旦置为 true 保持（Java 同）
    }

    [Fact]
    public void Span_split_line_break_marks_each_line()
    {
        var multi = new Span("a\nbc");
        var lines = multi.SplitLineBreak();

        Assert.Equal(2, lines.Count);
        Assert.Equal("a", lines.First!.Value.GetText());
        Assert.True(lines.First.Value.HasLinebreak());
        Assert.Equal("bc", lines.Last!.Value.GetText());
        Assert.False(lines.Last.Value.HasLinebreak());

        // 仅有单行时不拆分、不标记
        var single = new Span("abc").SplitLineBreak();
        Assert.Single(single);
        Assert.False(single.First!.Value.HasLinebreak());
    }

    [Fact]
    public void Placeholder_span_width_and_count_modes()
    {
        var byWidth = new PlaceholderSpan(10d, 5d);
        var size = byWidth.BlockSize();
        Assert.Equal(10d, size.GetWidth(), 6);
        Assert.Equal(5d, size.GetHeight(), 6);
        Assert.True(byWidth.IsIntegrity());
        Assert.Single(byWidth.SplitLineBreak());

        // 数量模式：宽度 = 数量 × (字号 + 字间距)
        var byCount = new PlaceholderSpan(3, 4d);
        Assert.Equal(3, byCount.GetHoldNum());
        Assert.Equal(12d, byCount.GetHoldWidth(), 6);
        Assert.Equal(12d, byCount.BlockSize().GetWidth(), 6);

        // 复制模式继承字号与字间距
        var src = new Span("x").SetFontSize(2).SetLetterSpacing(0.5);
        var bySpan = new PlaceholderSpan(4, src);
        Assert.Equal(4 * 2.5, bySpan.GetHoldWidth(), 6);

        // 设置宽度后 HoldNum 失效
        byCount.SetHoldWidth(7);
        Assert.Equal(7d, byCount.BlockSize().GetWidth(), 6);
    }

    [Fact]
    public void Txt_line_block_accepts_until_full()
    {
        var line = new TxtLineBlock(10, 1, TextAlign.Left);

        Assert.True(line.IsEmpty());
        Assert.True(line.TryAdd(new PlaceholderSpan(4d, 2d)));
        Assert.True(line.TryAdd(new PlaceholderSpan(4d, 3d)));

        // 剩余 2，无法容纳 4
        Assert.False(line.TryAdd(new PlaceholderSpan(4d, 2d)));

        Assert.False(line.IsEmpty());
        Assert.Equal(2, line.GetInlineSpans().Count);
        Assert.Equal(8d, line.GetWidth(), 6);
        Assert.Equal(3d, line.GetMaxSpanHeight(), 6);
        Assert.Equal(4d, line.GetHeight(), 6);  // 最高字高 3 + 行间距 1
        Assert.Equal(8d, line.Size().GetWidth(), 6);
        Assert.Equal(TextAlign.Left, line.GetTextAlign());
        Assert.Equal(10d, line.GetLineMaxAvailableWidth(), 6);

        // 默认对齐为左
        Assert.Equal(TextAlign.Left, new TxtLineBlock(10, 1).GetTextAlign());
    }

    [Fact]
    public void Txt_line_block_split_add_returns_remainder()
    {
        var line = new TxtLineBlock(10, 0);
        var span = new Span("abcdefghij").SetFontSize(2); // 每字符约 1mm (ASCII 半角)

        var remainder = line.TrySplitAdd(span);
        Assert.NotNull(remainder);
        Assert.Single(line.GetInlineSpans());
        Assert.EndsWith("j", remainder!.GetText()!);

        // 不可拆分的 Span 抛异常
        var fixedSpan = new Span("a");
        Assert.True(fixedSpan.IsIntegrity() == true);
        Assert.Throws<InvalidOperationException>(() => line.TrySplitAdd(fixedSpan));
    }

    [Fact]
    public void Img_sets_size_and_rejects_split()
    {
        string png = Path.Combine(_root, "img.png");
        using (var bmp = new SKBitmap(20, 10))
        using (var img = SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        var imgDiv = new Img(30, 20, png);
        Assert.Equal(30d, imgDiv.GetWidth());
        Assert.Equal(20d, imgDiv.GetHeight());
        Assert.Equal(png, imgDiv.GetSrc());
        Assert.True(imgDiv.IsIntegrity());
        Assert.Equal("Img", imgDiv.ElementType());
        Assert.Throws<InvalidOperationException>(() => imgDiv.Split(10));

#pragma warning disable CS0618 // 弃用构造：解析图片尺寸（5px/mm）
        var parsed = new Img(png);
#pragma warning restore CS0618
        Assert.Equal(4d, parsed.GetWidth());   // 20 / 5
        Assert.Equal(2d, parsed.GetHeight());  // 10 / 5

        Assert.Throws<ArgumentException>(() => new Img(10, 10, Path.Combine(_root, "nope.png")));

        // 读取图片
        var bitmap = Img.ReadImage(png);
        Assert.NotNull(bitmap);
        Assert.Equal(20, bitmap!.Width);
        bitmap.Dispose();
        Assert.Null(Img.ReadImage(Path.Combine(_root, "nope.png")));
    }
}
