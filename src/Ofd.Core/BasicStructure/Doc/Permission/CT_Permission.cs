namespace Ofd.Core.BasicStructure.Doc.Permission;

/// <summary>
/// 文档权限声明（7.5 图 9），用于文档防扩散等应用目的。
/// 各布尔子元素默认 true（允许）。
/// </summary>
public class CT_Permission : OFDElement
{
    public CT_Permission(Dom.DomElement proxy) : base(proxy) { }

    public CT_Permission() : base("Permissions") { }

    /// <summary>【可选】设置 是否允许编辑（默认 true）。</summary>
    public CT_Permission SetEdit(bool edit) => (CT_Permission)SetOFDEntity("Edit", BooleanStr(edit));

    /// <summary>【可选】获取 是否允许编辑，缺省 true。</summary>
    public bool GetEdit() => BoolAttr("Edit", true);

    /// <summary>【可选】设置 是否允许添加或修改标注（默认 true）。</summary>
    public CT_Permission SetAnnot(bool annot) => (CT_Permission)SetOFDEntity("Annot", BooleanStr(annot));

    /// <summary>【可选】获取 是否允许添加或修改标注，缺省 true。</summary>
    public bool GetAnnot() => BoolAttr("Annot", true);

    /// <summary>【可选】设置 是否允许导出（默认 true）。</summary>
    public CT_Permission SetExport(bool export) => (CT_Permission)SetOFDEntity("Export", BooleanStr(export));

    /// <summary>【可选】获取 是否允许导出，缺省 true。</summary>
    public bool GetExport() => BoolAttr("Export", true);

    /// <summary>【可选】设置 是否允许进行数字签名（默认 true）。</summary>
    public CT_Permission SetSignature(bool signature) => (CT_Permission)SetOFDEntity("Signature", BooleanStr(signature));

    /// <summary>【可选】获取 是否允许进行数字签名，缺省 true。</summary>
    public bool GetSignature() => BoolAttr("Signature", true);

    /// <summary>【可选】设置 是否允许添加水印（默认 true）。</summary>
    public CT_Permission SetWatermark(bool watermark) => (CT_Permission)SetOFDEntity("Watermark", BooleanStr(watermark));

    /// <summary>【可选】获取 是否允许添加水印，缺省 true。</summary>
    public bool GetWatermark() => BoolAttr("Watermark", true);

    /// <summary>【可选】设置 是否允许截屏（默认 true）。</summary>
    public CT_Permission SetPrintScreen(bool printScreen) => (CT_Permission)SetOFDEntity("PrintScreen", BooleanStr(printScreen));

    /// <summary>【可选】获取 是否允许截屏，缺省 true。</summary>
    public bool GetPrintScreen() => BoolAttr("PrintScreen", true);

    /// <summary>【可选】设置 打印权限（不设置则默认可打印且份数不受限）。</summary>
    public CT_Permission SetPrint(Print print)
    {
        Set(print);
        return this;
    }

    /// <summary>【可选】获取 打印权限，null 表示不存在。</summary>
    public Print? GetPrint()
    {
        var e = GetOFDElement("Print");
        return e == null ? null : new Print(e);
    }

    /// <summary>【可选】设置 有效期（不设置则开始结束均不受限）。</summary>
    public CT_Permission SetValidPeriod(ValidPeriod validPeriod)
    {
        Set(validPeriod);
        return this;
    }

    /// <summary>【可选】获取 有效期，null 表示不存在。</summary>
    public ValidPeriod? GetValidPeriod()
    {
        var e = GetOFDElement("ValidPeriod");
        return e == null ? null : new ValidPeriod(e);
    }

    private static string BooleanStr(bool v) => v ? "true" : "false";

    private bool BoolAttr(string name, bool defaultValue)
    {
        var str = GetOFDElementText(name);
        return str == null || str.Trim().Length == 0 ? defaultValue : bool.Parse(str.Trim());
    }
}
