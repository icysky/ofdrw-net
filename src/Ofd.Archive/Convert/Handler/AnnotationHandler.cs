using Ofd.Archive.Check;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器：注释合规转换（GB/T 42133-2022 6.10.1）。
/// <para>置 ReadOnly/NoZoom/NoRotate=true；展平 Appearance 内的 PageBlock 嵌套（不固化外观）。</para>
/// </summary>
public class AnnotationHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            var entities = reader.GetAnnotationEntities();
            if (entities == null || entities.Count == 0) return;

            foreach (var entity in entities)
            {
                foreach (var annot in entity.GetAnnots())
                {
                    annot.SetReadOnly(true);
                    annot.SetNoZoom(true);
                    annot.SetNoRotate(true);

                    var appearance = annot.GetAppearance();
                    if (appearance != null) FlattenPageBlocks(appearance.GetProxy());
                }
            }
        }
        catch (Exception e)
        {
            throw new IOException("处理注释时异常: " + e.Message, e);
        }
    }

    /// <summary>
    /// 把 Appearance 下所有 PageBlock 的子元素提升到 Appearance 级并删除被掏空的块。
    /// <para>
    /// 注意：此处必须有界。若父指针不一致导致 <see cref="DomElement.Detach"/> 失效，
    /// 原先的 while(true) 会每轮重新分配列表并永久循环（表现为内存单调暴涨）。
    /// 这里直接操作 Kids 列表摘除节点，并设置最大迭代次数兜底。
    /// </para>
    /// </summary>
    private static void FlattenPageBlocks(DomElement appearance)
    {
        const int maxRounds = 32;

        for (int round = 0; round < maxRounds; round++)
        {
            var blocks = DomWalker.Descendants(appearance, "PageBlock").ToList();
            if (blocks.Count == 0) return;

            foreach (var block in blocks)
            {
                // 提升子元素（直接摘除，避免依赖 Parent 指针）
                foreach (var kid in block.Kids.ToList())
                {
                    block.Kids.Remove(kid);
                    if (kid is DomElement el)
                    {
                        el.Detach();
                        appearance.Add(el);
                    }
                    else
                    {
                        appearance.Kids.Add(kid);
                    }
                }

                block.Kids.Clear();
                block.Detach();
                appearance.Kids.Remove(block);
            }
        }
    }
}
