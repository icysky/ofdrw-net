using Ofd.Core.Dom;

namespace Ofd.Core;

/// <summary>
/// 简单 OFD 类型元素：仅含 name 与文本值（Java OFDSimpleTypeElement）。
/// </summary>
public class OFDSimpleTypeElement : OFDElement
{
    public OFDSimpleTypeElement(string name, object? value) : base(name)
    {
        Proxy.Kids.Add(new DomText(value == null ? "" : value.ToString()!));
    }

    public OFDSimpleTypeElement(DomElement proxy) : base(proxy)
    {
    }

    /// <summary>获取元素值文本（无子文本返回 ""，dom4j getText 语义）。</summary>
    public string? GetValue()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var k in Proxy.Kids)
            if (k is DomText t)
                sb.Append(t.Text);
        return sb.ToString();
    }

    public OFDSimpleTypeElement SetValue(object? value)
    {
        SetText(value == null ? "" : value.ToString()!);
        return this;
    }
}
