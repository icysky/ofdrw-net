namespace Ofd.Core.BasicStructure.Doc.VPreferences;

/// <summary>
/// 标题栏显示模式（默认 FileName；设置 DocTitle 但不存在 Title 属性时按 FileName 处理）。
/// 7.5 表 9 视图首选项。
/// </summary>
public enum TabDisplay
{
    /// <summary>文件名称</summary>
    FileName,

    /// <summary>呈现元数据中的 Title 属性</summary>
    DocTitle,
}

public static class TabDisplayExtensions
{
    /// <summary>获取标题栏显示模式，空值默认 FileName，未知类型抛异常。</summary>
    public static TabDisplay GetInstance(string? tabDisplay)
    {
        if (tabDisplay == null || tabDisplay.Trim().Length == 0) return TabDisplay.FileName;
        if (tabDisplay.Equals("FileName", StringComparison.OrdinalIgnoreCase)) return TabDisplay.FileName;
        if (tabDisplay.Equals("DocTitle", StringComparison.OrdinalIgnoreCase)) return TabDisplay.DocTitle;
        throw new ArgumentException("未知的标题栏显示模式： " + tabDisplay);
    }
}
