using Ofd.Core.Dom;

namespace Ofd.Reader.Tools;

/// <summary>
/// 命名空间清理类（Java NameSpaceCleaner）：清除元素树上的所有命名空间。
/// </summary>
/// <remarks>
/// Java 采用 dom4j Visitor 遍历；本实现以静态方法遍历元素树完成等价操作
/// （设置 Prefix/Uri 为空，等价于 Namespace.NO_NAMESPACE 并清空额外命名空间声明）。
/// </remarks>
public static class NameSpaceCleaner
{
    /// <summary>清理根元素及其全部子孙元素的命名空间。</summary>
    public static void Clean(DomElement root)
    {
        if (root == null) return;

        var stack = new Stack<DomElement>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var e = stack.Pop();
            e.Prefix = "";
            e.Uri = "";
            foreach (var k in e.Kids)
                if (k is DomElement c)
                    stack.Push(c);
        }
    }
}
