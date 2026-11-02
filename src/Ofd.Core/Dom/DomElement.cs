namespace Ofd.Core.Dom;

/// <summary>
/// 轻量元素节点（dom4j Element 的最小对应）。
/// 属性与子节点均为插入序——这是与 dom4j 序列化字节一致的关键。
/// Prefix+Name 构成输出 QName；Uri 仅在根元素上声明 xmlns。
/// </summary>
public sealed class DomElement : DomNode
{
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string Uri { get; set; } = "";

    /// <summary>属性（有序，K 为完整 QName 字符串，如 "ofd:DocType"）。</summary>
    public List<(string K, string V)> Attrs { get; } = new();

    /// <summary>父元素（根为 null；dom4j 树双向语义）。</summary>
    public DomElement? Parent { get; private set; }

    /// <summary>子节点（有序，可为 DomElement/DomText/DomCData/DomComment）。</summary>
    public List<DomNode> Kids { get; } = new();

    public DomElement() { }

    public DomElement(string name, string prefix = "", string uri = "")
    {
        Name = name;
        Prefix = prefix;
        Uri = uri;
    }

    /// <summary>
    /// 追加子元素并返回该子元素（dom4j add 语义：若子元素已有父则先脱离原父）。
    /// </summary>
    public DomElement Add(DomElement child)
    {
        child.Detach();
        Kids.Add(child);
        child.Parent = this;
        return child;
    }

    /// <summary>从父元素脱离（dom4j detach 语义；无父时为空操作）。</summary>
    public void Detach()
    {
        if (Parent == null) return;
        Parent.Kids.Remove(this);
        Parent = null;
    }

    /// <summary>
    /// 深拷贝（dom4j Element.clone 语义：复制名称/前缀/命名空间/属性/全部子节点，新树无父）。
    /// </summary>
    public DomElement Clone()
    {
        var e = new DomElement(Name, Prefix, Uri);
        foreach (var (k, v) in Attrs)
            e.Attrs.Add((k, v));
        foreach (var k in Kids)
        {
            switch (k)
            {
                case DomElement c:
                    e.Add(c.Clone());
                    break;
                case DomText t:
                    e.Kids.Add(new DomText(t.Text));
                    break;
                case DomCData cd:
                    e.Kids.Add(new DomCData(cd.Text));
                    break;
                case DomComment cm:
                    e.Kids.Add(new DomComment(cm.Text));
                    break;
            }
        }
        return e;
    }

    public DomElement AddText(string text)
    {
        Kids.Add(new DomText(text));
        return this;
    }

    /// <summary>取第一个同名直接子元素（无则 null，dom4j element(name) 语义）。</summary>
    public DomElement? GetElement(string name)
    {
        foreach (var k in Kids)
            if (k is DomElement e && e.Name == name)
                return e;
        return null;
    }

    /// <summary>取所有同名直接子元素（保持次序，dom4j elements(name) 语义）。</summary>
    public List<DomElement> GetElements(string name)
    {
        var res = new List<DomElement>();
        foreach (var k in Kids)
            if (k is DomElement e && e.Name == name)
                res.Add(e);
        return res;
    }

    /// <summary>取第一个同名子元素的文本（无则 null）。</summary>
    public string? GetElementText(string name)
    {
        foreach (var k in Kids)
            if (k is DomElement e && e.Name == name)
                return e.GetText();
        return null;
    }

    /// <summary>元素的直接文本内容（所有 DomText 拼接，无则返回 null）。</summary>
    public string? GetText()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var k in Kids)
            if (k is DomText t)
                sb.Append(t.Text);
        return Kids.Any(static k => k is DomText) ? sb.ToString() : null;
    }

    public string? Attr(string name)
    {
        foreach (var (k, v) in Attrs)
            if (k == name)
                return v;
        return null;
    }

    public DomElement SetAttr(string name, string? value)
    {
        if (value == null)
        {
            Attrs.RemoveAll(a => a.K == name);
            return this;
        }
        for (int i = 0; i < Attrs.Count; i++)
            if (Attrs[i].K == name)
            {
                Attrs[i] = (name, value);
                return this;
            }
        Attrs.Add((name, value));
        return this;
    }
}
