namespace Ofd.Core.BasicStructure.Ofd.DocInfo;

/// <summary>文档分类（表 4 文档元数据属性）。默认值 Normal。</summary>
public enum DocUsage
{
    /// <summary>普通文档</summary>
    Normal,

    /// <summary>电子书</summary>
    EBook,

    /// <summary>电子报纸</summary>
    ENewsPaper,

    /// <summary>电子期刊（Java 拼写 EMagzine，保留）</summary>
    EMagzine,
}

public static class DocUsageExtensions
{
    /// <summary>获取文档分类，无法识别时默认 Normal。</summary>
    public static DocUsage GetInstance(string? usage)
    {
        usage = usage == null ? "" : usage.Trim();
        if (usage.Equals("Normal", StringComparison.OrdinalIgnoreCase)) return DocUsage.Normal;
        if (usage.Equals("EBook", StringComparison.OrdinalIgnoreCase)) return DocUsage.EBook;
        if (usage.Equals("ENewsPaper", StringComparison.OrdinalIgnoreCase)) return DocUsage.ENewsPaper;
        if (usage.Equals("EMagzine", StringComparison.OrdinalIgnoreCase)) return DocUsage.EMagzine;
        return DocUsage.Normal;
    }
}
