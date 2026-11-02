using Ofd.Archive.Check;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器 6：删除非 Goto 动作（GB/T 42133-2022 6.2.2c/6.2.3c/6.3.3b）。
/// <para>Document 级直接删除全部 Actions；页面/图元级保留 Goto/GotoA，其余删除。</para>
/// </summary>
public class NonGotoActionHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            // 1) Document 级：删除全部 Actions
            reader.GetDoc(0)?.RemoveOFDElemByNames("Actions");

            // 2) Page 级：逐页清理图元动作
            int pageCount = reader.GetNumberOfPages();
            for (int i = 1; i <= pageCount; i++)
            {
                var page = reader.GetPage(i);
                foreach (var actions in DomWalker.Descendants(page.GetProxy(), "Actions"))
                    CleanActionsElement(actions);
            }
        }
        catch (Exception e)
        {
            throw new IOException("无法处理动作: " + e.Message, e);
        }
    }

    /// <summary>清理单个 Actions 元素：保留 Goto/GotoA，删除其余；全删则移除该节点。</summary>
    private static void CleanActionsElement(Ofd.Core.Dom.DomElement actions)
    {
        bool hasValidAction = false;

        // 先快照，避免边遍历边删除
        var actionList = actions.Kids.OfType<Ofd.Core.Dom.DomElement>().ToList();

        foreach (var actionEl in actionList)
        {
            var typeEl = actionEl.Kids.OfType<Ofd.Core.Dom.DomElement>().FirstOrDefault();
            if (typeEl == null)
            {
                actionEl.Detach();
                continue;
            }

            if (typeEl.Name.Equals("Goto", StringComparison.OrdinalIgnoreCase) ||
                typeEl.Name.Equals("GotoA", StringComparison.OrdinalIgnoreCase))
            {
                hasValidAction = true; // 文档内跳转允许保留
            }
            else
            {
                actionEl.Detach(); // URI/Sound/Movie 等
            }
        }

        if (!hasValidAction) actions.Detach();
    }
}
