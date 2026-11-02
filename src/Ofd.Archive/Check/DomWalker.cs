using Ofd.Core.Dom;

namespace Ofd.Archive.Check;

/// <summary>DOM 遍历工具：按元素名深度优先收集后代（对应 Java 的 page.elements(name)）。</summary>
internal static class DomWalker
{
    /// <summary>收集指定名称的后代元素。</summary>
    public static List<DomElement> Descendants(DomElement? root, string name)
    {
        var result = new List<DomElement>();
        if (root == null) return result;

        Visit(root, name, result);
        return result;
    }

    private static void Visit(DomElement node, string name, List<DomElement> acc)
    {
        foreach (var kid in node.Kids)
        {
            if (kid is not DomElement el) continue;

            if (el.Name == name) acc.Add(el);
            Visit(el, name, acc);
        }
    }
}
