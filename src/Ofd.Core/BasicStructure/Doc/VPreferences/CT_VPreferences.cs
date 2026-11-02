namespace Ofd.Core.BasicStructure.Doc.VPreferences;

/// <summary>视图首选项（7.5 图 10），用于限定文档初始化视图。</summary>
public class CT_VPreferences : OFDElement
{
    public CT_VPreferences(Dom.DomElement proxy) : base(proxy) { }

    public CT_VPreferences() : base("VPreferences") { }

    /// <summary>【可选】设置 窗口模式（null 按默认 None）。</summary>
    public CT_VPreferences SetPageMode(PageMode pageMode)
    {
        SetOFDEntity("PageMode", pageMode.ToString());
        return this;
    }

    /// <summary>【可选】获取 窗口模式，缺省 None。</summary>
    public PageMode GetPageMode()
    {
        var mode = GetOFDElementText("PageMode");
        if (mode == null || mode.Trim().Length == 0) return PageMode.None;
        return PageModeExtensions.GetInstance(mode);
    }

    /// <summary>【可选】设置 页面布局模式（null 按默认 OneColumn）。</summary>
    public CT_VPreferences SetPageLayout(PageLayout pageLayout)
    {
        SetOFDEntity("PageLayout", pageLayout.ToString());
        return this;
    }

    /// <summary>【可选】获取 页面布局模式，缺省 OneColumn。</summary>
    public PageLayout GetPageLayout()
    {
        var str = GetOFDElementText("PageLayout");
        if (str == null || str.Trim().Length == 0) return PageLayout.OneColumn;
        return PageLayoutExtensions.GetInstance(str);
    }

    /// <summary>【可选】设置 标题栏显示模式。</summary>
    public CT_VPreferences SetTabDisplay(TabDisplay tabDisplay)
    {
        SetOFDEntity("TabDisplay", tabDisplay.ToString());
        return this;
    }

    /// <summary>【可选】获取 标题栏显示模式，缺省 FileName。</summary>
    public TabDisplay GetTabDisplay()
    {
        var str = GetOFDElementText("TabDisplay");
        if (str == null || str.Trim().Length == 0) return TabDisplay.FileName;
        return TabDisplayExtensions.GetInstance(str);
    }

    /// <summary>【可选】设置 是否隐藏工具栏（默认 false）。</summary>
    public CT_VPreferences SetHideToolbar(bool hideToolbar)
    {
        SetOFDEntity("HideToolbar", hideToolbar ? "true" : "false");
        return this;
    }

    /// <summary>【可选】获取 是否隐藏工具栏，缺省 false。</summary>
    public bool GetHideToolbar()
    {
        var str = GetOFDElementText("HideToolbar");
        return str != null && str.Trim().Length != 0 && bool.Parse(str.Trim());
    }

    /// <summary>【可选】设置 是否隐藏菜单栏（默认 false）。</summary>
    public CT_VPreferences SetHideMenubar(bool hideMenubar)
    {
        SetOFDEntity("HideMenubar", hideMenubar ? "true" : "false");
        return this;
    }

    /// <summary>【可选】获取 是否隐藏菜单栏，缺省 false。</summary>
    public bool GetHideMenubar()
    {
        var str = GetOFDElementText("HideMenubar");
        return str != null && str.Trim().Length != 0 && bool.Parse(str.Trim());
    }

    /// <summary>【可选】设置 是否隐藏主窗口之外的其他窗口组件（默认 false）。</summary>
    public CT_VPreferences SetHideWindowUI(bool hideWindowUI)
    {
        SetOFDEntity("HideWindowUI", hideWindowUI ? "true" : "false");
        return this;
    }

    /// <summary>【可选】获取 是否隐藏主窗口之外的其他窗口组件，缺省 false。</summary>
    public bool GetHideWindowUI()
    {
        var str = GetOFDElementText("HideWindowUI");
        return str != null && str.Trim().Length != 0 && bool.Parse(str.Trim());
    }

    /// <summary>【可选】设置 文档自动缩放模式（与 Zoom 互斥）。</summary>
    public CT_VPreferences SetZoomMode(ZoomMode zoomMode)
    {
        RemoveOFDElemByNames("Zoom", "ZoomMode");
        Add(zoomMode);
        return this;
    }

    /// <summary>【可选】设置 文档的缩放率（与 ZoomMode 互斥）。</summary>
    public CT_VPreferences SetZoom(double zoom)
    {
        RemoveOFDElemByNames("Zoom", "ZoomMode");
        Add(new Zoom(zoom));
        return this;
    }

    /// <summary>【可选】获取 具体的缩放处理方式和值（Zoom / ZoomMode），不存在 null。</summary>
    public ZoomScale? GetZoomScale()
    {
        var zoomMode = GetOFDElement("ZoomMode");
        if (zoomMode != null) return new ZoomMode(zoomMode);
        var zoom = GetOFDElement("Zoom");
        return zoom == null ? null : new Zoom(zoom);
    }
}
