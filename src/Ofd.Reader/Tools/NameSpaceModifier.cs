using Ofd.Core;
using Ofd.Core.Dom;

namespace Ofd.Reader.Tools;

/// <summary>
/// 命名空间变更类（Java NameSpaceModifier，已废弃，等价于 Ofd.Pkg 的 OFDNameSpaceModifier）。
/// </summary>
/// <remarks>
/// Java 采用 dom4j Visitor 遍历；本实现以静态方法遍历元素树完成等价操作。
/// </remarks>
public static class NameSpaceModifier
{
    /// <summary>将元素树整体变更到指定命名空间（默认 OFD 标准命名空间）。</summary>
    public static void Modify(DomElement root, string? prefix = null, string? uri = null)
    {
        if (root == null) return;

        string expectPrefix = prefix ?? Const.OFD_PREFIX;
        string expectUri = uri ?? Const.OFD_NAMESPACE;

        var stack = new Stack<DomElement>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var e = stack.Pop();
            e.Prefix = expectPrefix;
            e.Uri = expectUri;
            foreach (var k in e.Kids)
                if (k is DomElement c)
                    stack.Push(c);
        }
    }
}
