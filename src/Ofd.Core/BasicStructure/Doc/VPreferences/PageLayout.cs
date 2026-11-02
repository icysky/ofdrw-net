namespace Ofd.Core.BasicStructure.Doc.VPreferences;

/// <summary>页面布局（7.5 表 9 视图首选项）。</summary>
public enum PageLayout
{
    /// <summary>单页模式</summary>
    OnePage,

    /// <summary>单列模式</summary>
    OneColumn,

    /// <summary>对开模式</summary>
    TwoPageL,

    /// <summary>对开连续模式</summary>
    TwoColumnL,

    /// <summary>对开靠右模式</summary>
    TwoPageR,

    /// <summary>对开连续靠右模式</summary>
    TwoColumnR,
}

public static class PageLayoutExtensions
{
    /// <summary>获取页面布局，空串默认 OnePage，未知类型抛异常（与 Java 一致）。</summary>
    public static PageLayout GetInstance(string? pageLayout)
    {
        pageLayout = (pageLayout ?? "").Trim();
        if (pageLayout.Equals("OnePage", StringComparison.OrdinalIgnoreCase) || pageLayout.Length == 0)
            return PageLayout.OnePage;
        if (pageLayout.Equals("OneColumn", StringComparison.OrdinalIgnoreCase)) return PageLayout.OneColumn;
        if (pageLayout.Equals("TwoPageL", StringComparison.OrdinalIgnoreCase)) return PageLayout.TwoPageL;
        if (pageLayout.Equals("TwoColumnL", StringComparison.OrdinalIgnoreCase)) return PageLayout.TwoColumnL;
        if (pageLayout.Equals("TwoPageR", StringComparison.OrdinalIgnoreCase)) return PageLayout.TwoPageR;
        if (pageLayout.Equals("TwoColumnR", StringComparison.OrdinalIgnoreCase)) return PageLayout.TwoColumnR;
        throw new ArgumentException("未知页面布局类型： " + pageLayout);
    }
}
