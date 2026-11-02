namespace Ofd.Core;

/// <summary>OFD 文档常量（Java org.ofdrw.core.Const）。</summary>
public static class Const
{
    /// <summary>命名空间 URI，《GB/T_33190-2016》 7.1。</summary>
    public const string OFD_NAMESPACE = "http://www.ofdspec.org/2016";

    /// <summary>元素节点应使用的命名空间标识符。</summary>
    public const string OFD_VALUE = "ofd";

    /// <summary>OFD 命名空间前缀（含冒号）。</summary>
    public const string OFD_Q = "ofd:";

    /// <summary>默认命名空间前缀。</summary>
    public const string OFD_PREFIX = "ofd";

    /// <summary>xs:date 类型日期格式化。</summary>
    public const string DATE_FORMAT = "yyyy-MM-dd";

    /// <summary>xs:dateTime 类型时间日期格式化。</summary>
    public const string DATETIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ss";

    /// <summary>xs:dateTime 类型本地时间日期格式化。</summary>
    public const string LOCAL_DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

    /// <summary>OFD 索引文件。</summary>
    public const string INDEX_FILE = "OFD.xml";
}
