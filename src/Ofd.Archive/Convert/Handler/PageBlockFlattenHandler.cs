using Ofd.Archive.Check;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器：PageBlock 展平 —— 嵌套深度超过 3 的 PageBlock 子元素提升到父级（GB/T 42133-2022 6.3）。
/// </summary>
public class PageBlockFlattenHandler : IArchiveHandler
{
    /// <summary>最大允许嵌套深度。</summary>
    private const int MAX_DEPTH = 3;

    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            int pageCount = reader.GetNumberOfPages();
            for (int i = 1; i <= pageCount; i++)
            {
                var page = reader.GetPage(i);

                // 顶层 PageBlock（深度 1）
                foreach (var pageBlock in DomWalker.Descendants(page.GetProxy(), "PageBlock").ToList())
                {
                    // 仅处理顶层（父不是 PageBlock）
                    if (pageBlock.Parent?.Name == "PageBlock") continue;
                    FlattenRecursive(pageBlock, 1);
                }
            }
        }
        catch (Exception e)
        {
            throw new IOException("展平 PageBlock 时异常: " + e.Message, e);
        }
    }

    /// <summary>递归展平：深度超限时把子元素提升到父级并移除自身。</summary>
    private static void FlattenRecursive(DomElement block, int depth)
    {
        // 先递归处理子块
        var childBlocks = block.Kids.OfType<DomElement>().Where(e => e.Name == "PageBlock").ToList();
        foreach (var child in childBlocks) FlattenRecursive(child, depth + 1);

        if (depth <= MAX_DEPTH) return;

        var parent = block.Parent;
        if (parent == null) return;

        // 把 block 的子节点原地替换到 parent 的对应位置（保持 Z 序）
        var snapshot = parent.Kids.ToList();
        var children = block.Kids.ToList();

        parent.Kids.Clear();
        foreach (var node in snapshot)
        {
            if (ReferenceEquals(node, block))
            {
                foreach (var child in children)
                {
                    if (child is DomElement el) parent.Add(el);
                    else parent.Kids.Add(child);
                }
            }
            else if (node is DomElement el)
            {
                parent.Add(el);
            }
            else
            {
                parent.Kids.Add(node);
            }
        }

        block.Kids.Clear();
    }
}
