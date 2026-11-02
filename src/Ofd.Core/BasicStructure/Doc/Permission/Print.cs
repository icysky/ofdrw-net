namespace Ofd.Core.BasicStructure.Doc.Permission;

/// <summary>
/// 打印权限（7.5 图 9）。若不设置 Print 节点则默认可以打印且份数不受限。
/// </summary>
public class Print : OFDElement
{
    public Print(Dom.DomElement proxy) : base(proxy) { }

    public Print() : base("Print") { }

    public Print(bool printable, int copies) : this()
    {
        SetPrintable(printable).SetCopies(copies);
    }

    /// <summary>设置 是否允许被打印（默认 true）。</summary>
    public Print SetPrintable(bool printable) => (Print)AddAttribute("Printable", printable ? "true" : "false");

    /// <summary>获取 是否允许被打印，缺省 true。</summary>
    public bool GetPrintable()
    {
        var str = AttributeValue("Printable");
        return str == null || str.Trim().Length == 0 || bool.Parse(str.Trim());
    }

    /// <summary>
    /// 设置 打印份数（默认 -1：不限；负值不限；0 不允许打印；正值实际份数）。
    /// </summary>
    public Print SetCopies(int copies) =>
        (Print)AddAttribute("Copies", copies.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>获取 打印份数，缺省 -1。</summary>
    public int GetCopies()
    {
        var str = AttributeValue("Copies");
        if (str == null || str.Trim().Length == 0) return -1;
        return int.Parse(str.Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }
}
