using System.Text;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Core.PageDescription.Color.Color;
using Ofd.Core.PageDescription.Color.ColorSpace;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Core.Text.Font;
using Xunit;

namespace Ofd.Tests.BasicStructure;

/// <summary>res 资源体系（Res/字体/颜色空间/绘制参数/多媒体）验证。</summary>
public class ResourceTests
{
    [Fact]
    public void Res_container_factory_dispatch_and_round_trip()
    {
        var res = new Res().SetBaseLoc(new ST_Loc("./Res"));

        var fonts = new Fonts().AddFont(new CT_Font("宋体").SetID(10).SetBold(true));
        var colorSpaces = new ColorSpaces().AddColorSpace(new CT_ColorSpace(OFDColorSpaceType.RGB, 11));
        var drawParams = new DrawParams().AddDrawParam(new CT_DrawParam().SetID(new ST_ID(12)).SetLineWidth(1.5));
        var multiMedias = new MultiMedias().AddMultiMedia(
            new CT_MultiMedia().SetID(new ST_ID(13)).SetType(MediaType.Image).SetFormat("PNG")
                .SetMediaFile(new ST_Loc("Res/1.png")));

        res.AddResource(fonts).AddResource(colorSpaces).AddResource(drawParams).AddResource(multiMedias);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(res.Proxy));
        var parsed = new Res(DomParser.Parse(xml));

        Assert.Equal("./Res", parsed.GetBaseLoc()!.ToString());
        var resources = parsed.GetResources();
        Assert.Equal(4, resources.Count);
        Assert.IsType<Fonts>(resources[0]);
        Assert.IsType<ColorSpaces>(resources[1]);
        Assert.IsType<DrawParams>(resources[2]);
        Assert.IsType<MultiMedias>(resources[3]);

        // 字体
        var font = Assert.Single(((Fonts)resources[0]).GetFonts());
        Assert.Equal(10, font.GetID()!.Id);
        Assert.Equal("宋体", font.GetFontName());
        Assert.True(font.GetBold());
        Assert.False(font.GetItalic());

        // 多媒体
        var mm = Assert.Single(((MultiMedias)resources[3]).GetMultiMedias());
        Assert.Equal(MediaType.Image, mm.GetType());
        Assert.Equal("PNG", mm.GetFormat());
        Assert.Equal("Res/1.png", mm.GetMediaFile()!.ToString());

        // GetFonts 便捷方法
        Assert.Single(parsed.GetFonts());
    }

    [Fact]
    public void Font_charset_handles_shift_jis_and_default()
    {
        var font = new CT_Font("SimSun");
        Assert.Equal(Charset.unicode, font.GetCharset()); // 缺失默认 unicode
        font.SetCharset(Charset.shift_jis);
        Assert.Equal("shift-jis", font.Proxy.Attr("Charset")); // toString 特例
        Assert.Equal(Charset.shift_jis, font.GetCharset());
        font.SetCharset(null); // null → 默认 unicode
        Assert.Equal("unicode", font.Proxy.Attr("Charset"));
        Assert.Throws<ArgumentException>(() => CharsetExtensions.GetInstance("bogus"));
    }

    [Fact]
    public void ColorSpace_type_bpc_profile_palette()
    {
        var cs = new CT_ColorSpace(OFDColorSpaceType.CMYK, 20)
            .SetBitsPerComponent(BitsPerComponent.BIT_8)
            .SetProfile(new ST_Loc("Res/ICC.icc"));

        var palette = new Palette()
            .AddCV(new CV(new ST_Array(0, 0, 0)))
            .AddCV(new CV(new ST_Array("#FF", 255)));
        cs.SetPalette(palette);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(cs.Proxy));
        var parsed = new CT_ColorSpace(DomParser.Parse(xml));
        Assert.Equal(OFDColorSpaceType.CMYK, parsed.GetType());
        Assert.Equal(BitsPerComponent.BIT_8, parsed.GetBitsPerComponent());
        Assert.Equal("8", parsed.Proxy.Attr("BitsPerComponent"));
        Assert.Equal("Res/ICC.icc", parsed.GetProfile()!.ToString());

        var p = parsed.GetPalette()!;
        Assert.Equal(2, p.GetCVs().Count);
        Assert.Equal("0 0 0", p.GetCVByIndex(0).GetColor()!.ToString());
        Assert.Equal("#FF 255", p.GetColorByIndex(1)!.ToString());
        Assert.Throws<ArgumentOutOfRangeException>(() => p.GetCVByIndex(2));
        Assert.Throws<ArgumentException>(() => p.GetCVByIndex(-1));

        // 单独 BPC 语义：空值默认 BIT_1（Java 上游行为），非法值抛异常
        Assert.Equal(BitsPerComponent.BIT_1, BitsPerComponentExtensions.GetInstance((string?)null));
        Assert.Equal(BitsPerComponent.BIT_16, BitsPerComponentExtensions.GetInstance("16"));
        Assert.Throws<ArgumentException>(() => BitsPerComponentExtensions.GetInstance("3"));
    }

    [Fact]
    public void Color_value_index_alpha_and_rgb_helper()
    {
        var c = CT_Color.Rgb(17, 34, 51);
        Assert.Equal("17 34 51", c.GetValue()!.ToString());
        Assert.Equal(255, c.GetAlpha());
        c.SetAlpha(300);
        Assert.Equal(255, c.GetAlpha()); // 钳制上限
        c.SetAlpha(-5);
        Assert.Equal(0, c.GetAlpha());   // 钳制下限
        c.SetIndex(3);
        Assert.Equal(3, c.GetIndex());
        c.SetIndex(null);
        Assert.Null(c.GetIndex());
        Assert.Throws<ArgumentException>(() => c.SetIndex(-1));
        c.SetColorSpace(ST_RefID.GetInstance(11));
        Assert.Equal(11, c.GetColorSpace()!.GetRefId()!.Id);
        c.SetValue(null);
        Assert.Null(c.GetValue());
    }

    [Fact]
    public void DrawParam_defaults_and_fill_stroke_clone_rename()
    {
        var dp = new CT_DrawParam();
        Assert.Equal(0.353, dp.GetLineWidth());       // 缺省线宽 0.353mm
        Assert.Equal(3.528, dp.GetMiterLimit());      // 缺省截断值
        Assert.Equal(LineJoinType.Miter, dp.GetJoin());
        Assert.Equal(LineCapType.Butt, dp.GetCap());
        Assert.Equal(0, dp.GetDashOffset());
        Assert.Null(dp.GetFillColor());
        Assert.Throws<ArgumentException>(() => dp.SetLineWidth(-1));

        // 填充色：克隆源颜色并改名为 FillColor（源对象不受影响）
        var red = CT_Color.Rgb(255, 0, 0);
        dp.SetFillColor(red);
        var fill = dp.GetFillColor()!;
        Assert.Equal("FillColor", fill.Proxy.Name);
        Assert.Equal("255 0 0", fill.GetValue()!.ToString());
        Assert.Equal("Color", red.Proxy.Name); // 源元素名未被改动

        dp.SetStrokeColor(CT_Color.Rgb(0, 0, 255));
        Assert.Equal("StrokeColor", dp.GetStrokeColor()!.Proxy.Name);
        Assert.Equal("0 0 255", dp.GetStrokeColor()!.GetValue()!.ToString());

        // 再次设置覆盖（不重复追加）
        dp.SetFillColor(CT_Color.Rgb(0, 255, 0));
        Assert.Single(dp.GetOFDElements("FillColor", e => new Ofd.Core.Graph.PathObj.FillColor(e)));

        // 序列化往返
        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(dp.Proxy));
        var parsed = new CT_DrawParam(DomParser.Parse(xml));
        Assert.Equal("0 255 0", parsed.GetFillColor()!.GetValue()!.ToString());

        // Clone 深拷贝
        var copy = dp.Clone();
        copy.SetLineWidth(9);
        Assert.Equal(0.353, dp.GetLineWidth()); // 原对象不变
        Assert.Equal(9, copy.GetLineWidth());
    }

    [Fact]
    public void AddFont_requires_id()
    {
        Assert.Throws<ArgumentException>(() => new Fonts().AddFont(new CT_Font("A")));
        Assert.Throws<ArgumentException>(() => new ColorSpaces().AddColorSpace(new CT_ColorSpace()));
        Assert.Throws<ArgumentException>(() => new DrawParams().AddDrawParam(new CT_DrawParam()));
        Assert.Throws<ArgumentException>(() => new MultiMedias().AddMultiMedia(new CT_MultiMedia()));
        // null 忽略
        Assert.Empty(new Fonts().AddFont(null).GetFonts());
    }
}
