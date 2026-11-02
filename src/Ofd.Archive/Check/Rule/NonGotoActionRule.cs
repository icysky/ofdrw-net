using Ofd.Core.Action;
using Ofd.Core.Action.ActionType.ActionGoto;
using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 6：文档级动作只允许 Goto（GB/T 42133-2022 6.2.2）。
/// </summary>
public class NonGotoActionRule : IArchiveRule
{
    public const string RULE_NAME = "NON_GOTO_ACTION";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            var document = reader.GetDoc(0);
            var actions = document?.GetActions();
            if (actions != null)
            {
                foreach (var action in actions.GetActions())
                {
                    var act = action.GetAction();
                    if (act != null && act is not Goto)
                    {
                        violations.Add(new ArchiveViolation(
                            RULE_NAME, ArchiveViolation.Severity.Error,
                            "Document 包含非 Goto 动作: " + act.GetType().Name,
                            ST_Loc.GetInstance("Document.xml"),
                            act.GetType().Name, "Goto"));
                    }
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "无法检查动作: " + e.Message));
        }

        return violations;
    }
}
