using Ofd.Font;
using SkiaSharp;
using Xunit;

namespace Ofd.Tests.Font;

/// <summary>字体模块：内置字体、宽度映射、系统字体枚举、文本测量与目录加载。</summary>
public class FontTests
{
    [Fact]
    public void Builtin_fonts_have_expected_names()
    {
        var simsun = FontName.SimSun.Font();
        Assert.Equal("宋体", simsun.GetName());
        Assert.Equal("宋体", simsun.GetFamilyName());
        Assert.Equal("宋体-宋体", simsun.GetCompleteFontName());
        Assert.False(simsun.HasWidthMath());

        var times = FontName.TimesNewRoman.Font();
        Assert.Equal("Times New Roman", times.GetName());
        Assert.True(times.HasWidthMath());
        Assert.Equal(95, FontNameExtensions.TimesNewRomanPrintableAsciiWidthMap.Length);
        Assert.Equal(95, FontNameExtensions.NotoPrintableAsciiWidthMap.Length);

        Assert.Equal("黑体", FontName.SimHei.Font().GetName());
        Assert.Equal("楷体", FontName.KaiTi.Font().GetName());
        Assert.Equal("微软雅黑", FontName.MSYahei.Font().GetName());
        Assert.Equal("仿宋", FontName.FangSong.Font().GetName());
    }

    [Fact]
    public void Char_width_scale_follows_map_then_half_width_default()
    {
        var times = FontName.TimesNewRoman.Font();
        // 空格（32）与 '0'（48）
        Assert.Equal(0.25, times.GetCharWidthScale(' '), 6);
        Assert.Equal(FontNameExtensions.TimesNewRomanPrintableAsciiWidthMap[48 - 32], times.GetCharWidthScale('0'), 6);
        // 非 ASCII → 1
        Assert.Equal(1, times.GetCharWidthScale('中'));

        // 无映射：ASCII 0.5，非 ASCII 1
        var simsun = FontName.SimSun.Font();
        Assert.Equal(0.5, simsun.GetCharWidthScale('A'));
        Assert.Equal(1, simsun.GetCharWidthScale('中'));
    }

    [Fact]
    public void Width_map_can_be_replaced_and_queried()
    {
        var font = new Ofd.Font.Font("自定义", "自定义族");
        Assert.False(font.HasWidthMath());

        font.SetPrintableAsciiWidthMap(new double[95]);
        Assert.True(font.HasWidthMath());
        Assert.Equal(0, font.GetCharWidthScale('A'));
        Assert.Equal(1, font.GetCharWidthScale('中')); // 非 ASCII 始终 1
    }

    [Fact]
    public void Font_file_loading_and_error_handling()
    {
        string? ttf = FindSystemFont("arial.ttf") ?? FindAnySystemFont();

        if (ttf != null)
        {
            var font = new Ofd.Font.Font("Arial", "Arial", ttf);
            Assert.NotNull(font.GetFontObj());
            Assert.Equal(Path.GetFileName(ttf), font.GetFontFileName());
            Assert.True(font.IsEmbeddable());

            font.SetEmbeddable(false);
            Assert.False(font.IsEmbeddable());
        }

        // 不存在的字体文件 → 异常
        Assert.Throws<ArgumentException>(() =>
            new Ofd.Font.Font("X", "X", Path.Combine(Path.GetTempPath(), "not-exist-" + Guid.NewGuid() + ".ttf")));

        // 非字体内容 → 格式错误
        string bad = Path.Combine(Path.GetTempPath(), "bad-" + Guid.NewGuid().ToString("N")[..6] + ".ttf");
        File.WriteAllBytes(bad, new byte[] { 1, 2, 3, 4 });
        try
        {
            Assert.Throws<ArgumentException>(() => new Ofd.Font.Font("X", "X", bad));
        }
        finally
        {
            File.Delete(bad);
        }
    }

    [Fact]
    public void EnvFont_resolves_system_font_and_falls_back()
    {
        Assert.Null(EnvFont.GetFont("这个字体肯定不存在-XYZ"));

        var def = EnvFont.GetDefaultFont();
        Assert.NotNull(def);

        // 未命中名称时使用 family 兜底
        var byFamily = EnvFont.GetFont("这个字体肯定不存在-XYZ", def!.GetFamilyName());
        Assert.NotNull(byFamily);
    }

    [Fact]
    public void StrBounds_measures_text_at_given_size()
    {
        var b = EnvFont.StrBounds("宋体", null, "Hello 中文", 10);
        Assert.True(b.Width > 0);
        Assert.True(b.Height > 0);

        // 字号翻倍 → 宽度近似翻倍
        var b2 = EnvFont.StrBounds("宋体", null, "Hello 中文", 20);
        Assert.True(b2.Width > b.Width * 1.8);
        Assert.True(b2.Width < b.Width * 2.2);

        // 空串宽度为 0
        Assert.Equal(0, EnvFont.StrBounds("宋体", null, "", 10).Width);
    }

    [Fact]
    public void EnvFont_load_registers_fonts_from_directory()
    {
        string? ttf = FindSystemFont("arial.ttf") ?? FindAnySystemFont();
        if (ttf == null) return; // 无系统字体文件时跳过

        string dir = Path.Combine(Path.GetTempPath(), "ofdnet_font_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string target = Path.Combine(dir, "MyTestFont.ttf");
            File.Copy(ttf, target);

            EnvFont.Load(dir);

            // 以文件名注册 → 可按 "MyTestFont" 查询
            var loaded = EnvFont.GetFont("MyTestFont");
            Assert.NotNull(loaded);
            Assert.NotNull(loaded!.GetFontObj());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SetMapping_overrides_lookup()
    {
        var custom = new Ofd.Font.Font("自定义映射字体", "自定义族").SetTypeface(SKTypeface.Default);
        EnvFont.SetMapping("自定义映射字体", custom);

        var found = EnvFont.GetFont("自定义映射字体");
        Assert.NotNull(found);
        Assert.Equal("自定义映射字体", found!.GetName());
    }

    private static string? FindSystemFont(string fileName)
    {
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string path = Path.Combine(dir, fileName);
        return File.Exists(path) ? path : null;
    }

    private static string? FindAnySystemFont()
    {
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (!Directory.Exists(dir)) return null;

        return Directory.EnumerateFiles(dir, "*.ttf").FirstOrDefault()
               ?? Directory.EnumerateFiles(dir, "*.otf").FirstOrDefault();
    }
}
