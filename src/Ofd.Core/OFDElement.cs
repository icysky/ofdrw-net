using Ofd.Core.BasicType;
using Ofd.Core.Dom;

namespace Ofd.Core;

/// <summary>
/// OFD 文档元素基类（GB/T 33190-2016 7.1：XML 命名空间为 http://www.ofdspec.org/2016，
/// 标识符 ofd；元素节点使用命名空间，元素属性不使用命名空间）。
/// <para>
/// Java 版 OFDElement 通过 DefaultElementProxy 代理 dom4j Element（719 行转发表）；
/// C# 版简化为组合：本类持有底层 <see cref="DomElement"/>（Proxy），序列化直接 Dump Proxy。
/// </para>
/// </summary>
public class OFDElement
{
    /// <summary>
    /// 命名空间严格模式。
    /// true - 元素须位于 OFD 空间（ofdspec.org 前缀）；
    /// false - 只要元素名称相同即认为是 OFD 元素（默认，与 Java 一致）。
    /// </summary>
    public static bool NSStrictMode = false;

    /// <summary>底层 XML 元素（dom4j Element 的 C# 对应）。</summary>
    protected internal DomElement Proxy { get; }

    protected OFDElement(string name)
    {
        // 设置 xmlns:ofd=http://www.ofdspec.org/2016
        Proxy = new DomElement(name, Const.OFD_PREFIX, Const.OFD_NAMESPACE);
    }

    /// <summary>代理包装构造（对应 Java OFDElement(Element proxy)，C# 直接持有解析树元素）。</summary>
    protected OFDElement(DomElement proxy)
    {
        Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
    }

    /// <summary>共享另一个 OFDElement 的底层元素（对应 Java OFDElement(OFDElement other)）。</summary>
    protected OFDElement(OFDElement other)
    {
        Proxy = other?.Proxy ?? throw new ArgumentNullException(nameof(other));
    }

    /// <summary>获取 OFD 类型元素实例（Java getInstance(String name)）。</summary>
    public static OFDElement GetInstance(string name) => new SimpleOFDElement(name);

    private sealed class SimpleOFDElement : OFDElement
    {
        public SimpleOFDElement(string name) : base(name) { }
    }

    /// <summary>获取底层 XML 元素（对应 Java DefaultElementProxy.getProxy()，供 Pkg/Reader 等模块使用）。</summary>
    public DomElement GetProxy() => Proxy;

    /// <summary>元素全名（含前缀，如 "ofd:OFD"）。</summary>
    public string GetQualifiedName() => Proxy.Prefix.Length > 0 ? Proxy.Prefix + ":" + Proxy.Name : Proxy.Name;

    // ==================== 实体（文本子元素）操作 ====================

    /// <summary>向元素中增加 OFD 元素（不查重，直接追加）。</summary>
    public OFDElement AddOFDEntity(string name, object? value)
    {
        Proxy.Add(new OFDSimpleTypeElement(name, value).Proxy);
        return this;
    }

    /// <summary>
    /// 设置 OFD 参数：已存在同名元素则修改其文本，否则追加。
    /// （Java setOFDEntity(qname, Serializable value)）
    /// </summary>
    public OFDElement SetOFDEntity(string name, object? value)
    {
        var e = GetOFDElement(name);
        if (e == null)
            return AddOFDEntity(name, value);
        SetElementText(e, value?.ToString() ?? "");
        return this;
    }

    /// <summary>设置元素名称（保留属性与子节点）。</summary>
    public OFDElement SetOFDName(string name)
    {
        Proxy.Name = name;
        Proxy.Uri = Const.OFD_NAMESPACE;
        Proxy.Prefix = Const.OFD_PREFIX;
        return this;
    }

    /// <summary>获取指定名称 OFD 元素（第一个）。非严格模式按元素名匹配（与 dom4j element(name) 一致，不限命名空间）。</summary>
    public DomElement? GetOFDElement(string name) =>
        MatchElement(Proxy, name);

    /// <summary>获取指定名称 OFD 元素并包装为指定类型（Java 同类方法无类型约束，mapper 可返回任意类型）。</summary>
    public R? GetOFDElement<R>(string name, Func<DomElement, R> mapper)
    {
        var e = GetOFDElement(name);
        return e == null ? default : mapper(e);
    }

    /// <summary>获取 OFD 元素中的文本（元素不存在时 null）。</summary>
    public string? GetOFDElementText(string name)
    {
        var e = GetOFDElement(name);
        return e == null ? null : ElementText(e);
    }

    /// <summary>获取指定名称 OFD 元素集合并保持原有次序。</summary>
    public List<R> GetOFDElements<R>(string name, Func<DomElement, R> mapper)
    {
        var c = MatchElements(Proxy, name);
        return c.Select(mapper).ToList();
    }

    /// <summary>
    /// 设置元素：若同类型元素已存在则删除原元素后添加（Java set(Element)）。
    /// </summary>
    public OFDElement Set(OFDElement element)
    {
        if (element == null) return this;
        // 删除同名原有元素
        foreach (var old in MatchElements(Proxy, element.Proxy.Name).ToList())
            Proxy.Kids.Remove(old);
        Proxy.Add(element.Proxy);
        return this;
    }

    /// <summary>根据名称删除节点内所有匹配的 OFD 元素，返回被删除元素。</summary>
    public List<DomElement> RemoveOFDElemByNames(params string?[] names)
    {
        var deleted = new List<DomElement>();
        if (names == null) return null!;
        foreach (var name in names)
        {
            if (name == null || name.Trim().Length == 0) continue;
            foreach (var toBeDelete in MatchElements(Proxy, name).ToList())
            {
                Proxy.Kids.Remove(toBeDelete);
                deleted.Add(toBeDelete);
            }
        }
        return deleted;
    }

    // ==================== 属性操作 ====================

    /// <summary>获取属性值（不存在 null）。</summary>
    public string? AttributeValue(string name) => Proxy.Attr(name);

    /// <summary>设置属性（存在则覆盖）。</summary>
    public OFDElement AddAttribute(string name, string? value)
    {
        Proxy.SetAttr(name, value);
        return this;
    }

    /// <summary>属性存在则删除，返回是否删除成功。</summary>
    public bool RemoveAttr(string name)
    {
        int before = Proxy.Attrs.Count;
        Proxy.Attrs.RemoveAll(a => a.K == name);
        return Proxy.Attrs.Count < before;
    }

    /// <summary>设置 OFD 对象标识（无符号整数，文档内唯一；0 为无效标识）。</summary>
    public OFDElement SetObjID(ST_ID objId) => AddAttribute("ID", objId.ToString());

    public OFDElement SetObjID(long objId) => SetObjID(new ST_ID(objId));

    public OFDElement SetObjID(string id) => AddAttribute("ID", id);

    /// <summary>获取 OFD 对象标识，null 表示不存在。</summary>
    public ST_ID? GetObjID() => ST_ID.GetInstance(AttributeValue("ID"));

    // ==================== 内容（子节点）操作 ====================

    /// <summary>添加子元素（dom4j add 语义：若子元素已有父则先脱离原父）。</summary>
    public OFDElement Add(OFDElement element)
    {
        Proxy.Add(element.Proxy);
        return this;
    }

    /// <summary>删除子元素，返回是否成功。</summary>
    public bool Remove(OFDElement element) => Proxy.Kids.Remove(element.Proxy);

    /// <summary>移除元素中所有子内容。</summary>
    public OFDElement RemoveAll()
    {
        Proxy.Kids.Clear();
        return this;
    }

    /// <summary>获取所有子元素（保持次序）。</summary>
    public List<DomElement> Elements() =>
        Proxy.Kids.OfType<DomElement>().ToList();

    /// <summary>获取元素全部直接文本（无文本返回 null，dom4j getText 语义）。</summary>
    public string? GetText() => Proxy.GetText();

    /// <summary>设置元素文本（覆盖全部子内容）。</summary>
    public OFDElement SetText(string text)
    {
        Proxy.Kids.Clear();
        Proxy.Kids.Add(new DomText(text));
        return this;
    }

    // ==================== 命名空间匹配 ====================

    /// <summary>
    /// dom4j element(name) / elements(name) 语义：仅按元素名匹配、不限命名空间；
    /// 严格模式（<see cref="NSStrictMode"/>）要求命名空间以 http://www.ofdspec.org 开头
    /// （Java OFDCommonQName 语义）。
    /// </summary>
    internal static bool MatchName(DomElement e, string name)
    {
        if (e.Name != name) return false;
        return !NSStrictMode || e.Uri.StartsWith("http://www.ofdspec.org", StringComparison.Ordinal);
    }

    internal static DomElement? MatchElement(DomElement parent, string name)
    {
        foreach (var k in parent.Kids)
            if (k is DomElement e && MatchName(e, name))
                return e;
        return null;
    }

    internal static List<DomElement> MatchElements(DomElement parent, string name)
    {
        var res = new List<DomElement>();
        foreach (var k in parent.Kids)
            if (k is DomElement e && MatchName(e, name))
                res.Add(e);
        return res;
    }

    /// <summary>元素全部直接文本（dom4j Element.getText()：无文本节点返回 ""，含空白）。</summary>
    internal static string ElementText(DomElement e)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var k in e.Kids)
            if (k is DomText t)
                sb.Append(t.Text);
        return sb.ToString();
    }

    /// <summary>设置元素的文本内容（清除原有子内容后写单个文本节点）。</summary>
    internal static void SetElementText(DomElement e, string text)
    {
        e.Kids.Clear();
        e.Kids.Add(new DomText(text));
    }

    public override string ToString() => GetQualifiedName();
}
