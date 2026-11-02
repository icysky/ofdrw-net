namespace Ofd.Core.Dom;

/// <summary>DOM 节点基类（dom4j Node 的最小对应）。</summary>
public abstract class DomNode
{
}

/// <summary>元素内文本（dom4j Text）。</summary>
public sealed class DomText(string text) : DomNode
{
    public string Text { get; set; } = text;
}

/// <summary>CDATA 节点（dom4j CDATA，输出为 &lt;![CDATA[...]]&gt;）。</summary>
public sealed class DomCData(string text) : DomNode
{
    public string Text { get; set; } = text;
}

/// <summary>注释节点（dom4j Comment，输出为 &lt;!--...--&gt;）。</summary>
public sealed class DomComment(string text) : DomNode
{
    public string Text { get; set; } = text;
}
