using System.Text;
using Ofd.Core.Action.ActionType.ActionGoto;
using Ofd.Core.BasicStructure.Doc.Bookmark;
using Ofd.Core.BasicStructure.Doc.Permission;
using Ofd.Core.BasicStructure.Doc.VPreferences;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Xunit;

namespace Ofd.Tests.BasicStructure;

/// <summary>vpreferences / permission / bookmark / CT_Dest 批次行为验证。</summary>
public class DocPreferencesTests
{
    [Fact]
    public void VPreferences_defaults_match_spec()
    {
        var vp = new CT_VPreferences();
        Assert.Equal(PageMode.None, vp.GetPageMode());
        Assert.Equal(PageLayout.OneColumn, vp.GetPageLayout()); // 默认 OneColumn（非 OnePage）
        Assert.Equal(TabDisplay.FileName, vp.GetTabDisplay());
        Assert.False(vp.GetHideToolbar());
        Assert.False(vp.GetHideMenubar());
        Assert.False(vp.GetHideWindowUI());
        Assert.Null(vp.GetZoomScale());
    }

    [Fact]
    public void VPreferences_zoom_and_zoom_mode_are_mutually_exclusive()
    {
        var vp = new CT_VPreferences();
        vp.SetZoom(1.5);
        Assert.IsType<Zoom>(vp.GetZoomScale());
        Assert.Equal(1.5, ((Zoom)vp.GetZoomScale()!).GetValue());

        // 设置 ZoomMode 应删除已有 Zoom
        vp.SetZoomMode(ZoomMode.GetInstance(ZoomMode.Type.FitWidth));
        var zs = vp.GetZoomScale();
        Assert.IsType<ZoomMode>(zs);
        Assert.Equal(ZoomMode.Type.FitWidth, ((ZoomMode)zs!).GetType());
        Assert.Null(vp.GetOFDElement("Zoom"));
    }

    [Fact]
    public void VPreferences_round_trip()
    {
        var vp = new CT_VPreferences()
            .SetPageMode(PageMode.UseBookmarks)
            .SetPageLayout(PageLayout.TwoColumnR)
            .SetTabDisplay(TabDisplay.DocTitle)
            .SetHideToolbar(true)
            .SetZoom(2.0);
        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(vp.Proxy));
        var parsed = new CT_VPreferences(DomParser.Parse(xml));
        Assert.Equal(PageMode.UseBookmarks, parsed.GetPageMode());
        Assert.Equal(PageLayout.TwoColumnR, parsed.GetPageLayout());
        Assert.Equal(TabDisplay.DocTitle, parsed.GetTabDisplay());
        Assert.True(parsed.GetHideToolbar());
        Assert.Equal(2.0, ((Zoom)parsed.GetZoomScale()!).GetValue());
        // Zoom 值走 fmt
        Assert.Contains("<ofd:Zoom>2</ofd:Zoom>", xml);
    }

    [Fact]
    public void Permission_defaults_are_all_true()
    {
        var p = new CT_Permission();
        Assert.True(p.GetEdit());
        Assert.True(p.GetAnnot());
        Assert.True(p.GetExport());
        Assert.True(p.GetSignature());
        Assert.True(p.GetWatermark());
        Assert.True(p.GetPrintScreen());
        Assert.Null(p.GetPrint());
        Assert.Null(p.GetValidPeriod());
    }

    [Fact]
    public void Permission_print_and_valid_period()
    {
        var print = new Print(false, 3);
        Assert.False(print.GetPrintable());
        Assert.Equal(3, print.GetCopies());
        Assert.Equal(-1, new Print().GetCopies()); // 缺省 -1

        var vp = new ValidPeriod(new DateTime(2026, 10, 21, 8, 30, 0), new DateTime(2027, 10, 21, 18, 0, 0));
        var perm = new CT_Permission().SetPrint(print).SetValidPeriod(vp).SetEdit(false);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(perm.Proxy));
        var parsed = new CT_Permission(DomParser.Parse(xml));
        Assert.False(parsed.GetEdit());
        Assert.False(parsed.GetPrint()!.GetPrintable());
        Assert.Equal(3, parsed.GetPrint()!.GetCopies());
        Assert.Equal(new DateTime(2026, 10, 21, 8, 30, 0), parsed.GetValidPeriod()!.GetStartDate());
        Assert.Equal(new DateTime(2027, 10, 21, 18, 0, 0), parsed.GetValidPeriod()!.GetEndDate());
        Assert.Contains("StartDate=\"2026-10-21 08:30:00\"", xml); // Java 本地时间格式（空格分隔）
    }

    [Fact]
    public void ValidPeriod_null_removes_attribute()
    {
        var vp = new ValidPeriod(new DateTime(2026, 1, 1, 0, 0, 0), new DateTime(2026, 2, 1, 0, 0, 0));
        vp.SetStartDate(null);
        Assert.Null(vp.GetStartDate());
        Assert.NotNull(vp.GetEndDate());
    }

    [Fact]
    public void Bookmark_with_dest_round_trip()
    {
        var dest = new CT_Dest()
            .SetType(DestType.XYZ)
            .SetPageID(ST_RefID.GetInstance(12))
            .SetLeft(10)
            .SetTop(20.5)
            .SetZoom(1.25);
        var bookmarks = new Bookmarks()
            .AddBookmark(new Bookmark("第一章", dest))
            .AddBookmark(new Bookmark("第二章", new CT_Dest().SetType(DestType.Fit).SetPageID(ST_RefID.GetInstance(13))));

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(bookmarks.Proxy));
        var parsed = new Bookmarks(DomParser.Parse(xml));
        var list = parsed.GetBookmarks();
        Assert.Equal(2, list.Count);
        Assert.Equal("第一章", list[0].GetBookmarkName());
        var d = list[0].GetDest()!;
        Assert.Equal(DestType.XYZ, d.GetType());
        Assert.Equal(12, d.GetPageID()!.GetRefId()!.Id);
        Assert.Equal(10, d.GetLeft());
        Assert.Equal("20.500", ((double)d.GetTop()).ToString("F3")); // 20.5 经 fmt 为 "20.500"
        Assert.Equal(1.25, d.GetZoom());
        Assert.Equal(DestType.Fit, list[1].GetDest()!.GetType());
        Assert.Equal(0, list[1].GetDest()!.GetLeft()); // 缺省 0
    }

    [Fact]
    public void Enum_parsing_is_case_insensitive_and_rejects_unknown()
    {
        Assert.Equal(PageLayout.TwoPageR, PageLayoutExtensions.GetInstance("twopager"));
        Assert.Equal(PageMode.UseThumbs, PageModeExtensions.GetInstance(" usethumbs "));
        Assert.Equal(TabDisplay.DocTitle, TabDisplayExtensions.GetInstance("doctitle"));
        Assert.Equal(DestType.FitR, DestTypeExtensions.GetInstance("fitr"));
        Assert.Throws<ArgumentException>(() => PageLayoutExtensions.GetInstance("Bogus"));
        Assert.Throws<ArgumentException>(() => PageModeExtensions.GetInstance("Bogus"));
        Assert.Throws<ArgumentException>(() => DestTypeExtensions.GetInstance("Bogus"));
        Assert.Throws<ArgumentException>(() => TabDisplayExtensions.GetInstance("Bogus"));
    }

    [Fact]
    public void ZoomValue_uses_fmt_and_repeated_set_appends_like_java()
    {
        var z = new Zoom(1.5);
        Assert.Equal("1.500", ElementTextOf(z.Proxy));
        // Java 用 addText（追加）：重复设置产生 "1.500" + "2"（fmt(2.0)="2"），保持上游行为
        z.SetValue(2.0);
        Assert.Equal("1.5002", ElementTextOf(z.Proxy));
    }

    private static string ElementTextOf(DomElement e)
    {
        var sb = new StringBuilder();
        foreach (var k in e.Kids)
            if (k is DomText t) sb.Append(t.Text);
        return sb.ToString();
    }
}
