using Ofd.Core.Action.ActionType.ActionGoto;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>处理器 7：删除大纲中的非 Goto 动作（GB/T 42133-2022 6.2.2）。</summary>
public class OutlineActionHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            var document = reader.GetDoc(0);
            var outlines = document?.GetOutlines();
            if (outlines == null) return;

            foreach (var elem in outlines.GetOutlineElems())
            {
                var actions = elem.GetActions();
                if (actions == null) continue;

                foreach (var action in actions.GetActions().ToList())
                {
                    var act = action.GetAction();
                    if (act != null && act is not Goto) actions.Remove(action);
                }
            }
        }
        catch (Exception e)
        {
            throw new IOException("无法处理大纲动作: " + e.Message, e);
        }
    }
}
