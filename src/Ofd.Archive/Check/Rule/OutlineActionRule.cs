using Ofd.Core.Action;
using Ofd.Core.Action.ActionType.ActionGoto;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 7：大纲节点动作只允许 Goto（GB/T 42133-2022 6.2.2）。
/// </summary>
public class OutlineActionRule : IArchiveRule
{
    public const string RULE_NAME = "OUTLINE_ACTION";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            var document = reader.GetDoc(0);
            var outlines = document?.GetOutlines();
            if (outlines != null)
            {
                foreach (var elem in outlines.GetOutlineElems())
                {
                    var actions = elem.GetActions();
                    if (actions == null) continue;

                    foreach (var action in actions.GetActions())
                    {
                        var act = action.GetAction();
                        if (act != null && act is not Goto)
                        {
                            violations.Add(new ArchiveViolation(
                                RULE_NAME, ArchiveViolation.Severity.Error,
                                "大纲节点包含非 Goto 动作: " + act.GetType().Name,
                                null, act.GetType().Name, "Goto"));
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "无法检查大纲: " + e.Message));
        }

        return violations;
    }
}
