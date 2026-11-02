namespace Ofd.Core.BasicStructure.Doc.VPreferences;

/// <summary>窗口模式（7.5 表 9 视图首选项属性，默认 None）。</summary>
public enum PageMode
{
    /// <summary>常规模式</summary>
    None,

    /// <summary>打开后全文显示</summary>
    FullScreen,

    /// <summary>同时呈现文档大纲</summary>
    UseOutlines,

    /// <summary>同时呈现缩略图</summary>
    UseThumbs,

    /// <summary>同时呈现语义结构</summary>
    UseCustomTags,

    /// <summary>同时呈现图层</summary>
    UseLayers,

    /// <summary>同时呈现附件（Java 拼写 UseAttatchs，保留）</summary>
    UseAttatchs,

    /// <summary>同时呈现书签</summary>
    UseBookmarks,
}

public static class PageModeExtensions
{
    /// <summary>获取窗口模式，空串默认 None，未知类型抛异常（与 Java 一致）。</summary>
    public static PageMode GetInstance(string? mode)
    {
        mode = (mode ?? "").Trim();
        if (mode.Length == 0 || mode.Equals("None", StringComparison.OrdinalIgnoreCase)) return PageMode.None;
        if (mode.Equals("FullScreen", StringComparison.OrdinalIgnoreCase)) return PageMode.FullScreen;
        if (mode.Equals("UseOutlines", StringComparison.OrdinalIgnoreCase)) return PageMode.UseOutlines;
        if (mode.Equals("UseThumbs", StringComparison.OrdinalIgnoreCase)) return PageMode.UseThumbs;
        if (mode.Equals("UseCustomTags", StringComparison.OrdinalIgnoreCase)) return PageMode.UseCustomTags;
        if (mode.Equals("UseLayers", StringComparison.OrdinalIgnoreCase)) return PageMode.UseLayers;
        if (mode.Equals("UseAttatchs", StringComparison.OrdinalIgnoreCase)) return PageMode.UseAttatchs;
        if (mode.Equals("UseBookmarks", StringComparison.OrdinalIgnoreCase)) return PageMode.UseBookmarks;
        throw new ArgumentException("未知的窗口模式：" + mode);
    }
}
