using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 注释合规检查（GB/T 42133-2022 6.10.1）：
/// ReadOnly/NoZoom/NoRotate 应为 true；Appearance 中不应嵌套 PageBlock。
/// </summary>
public class AnnotationRule : IArchiveRule
{
    public const string RULE_NAME = "ANNOTATION";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            var entities = reader.GetAnnotationEntities();
            if (entities == null || entities.Count == 0) return violations;

            foreach (var entity in entities)
            {
                var annotLoc = ST_Loc.GetInstance("Annots/Annot_" + entity.GetPageId() + ".xml");

                foreach (var annot in entity.GetAnnots())
                {
                    string annotId = annot.GetId()?.ToString() ?? "?";

                    if (annot.GetReadOnly() != true)
                    {
                        violations.Add(new ArchiveViolation(
                            RULE_NAME, ArchiveViolation.Severity.Warn,
                            $"注释 ReadOnly 应为 true (ID={annotId})",
                            annotLoc, annot.GetReadOnly()?.ToString().ToLowerInvariant(), "true"));
                    }

                    if (annot.GetNoZoom() != true)
                    {
                        violations.Add(new ArchiveViolation(
                            RULE_NAME, ArchiveViolation.Severity.Warn,
                            $"注释 NoZoom 应为 true (ID={annotId})",
                            annotLoc, annot.GetNoZoom()?.ToString().ToLowerInvariant(), "true"));
                    }

                    if (annot.GetNoRotate() != true)
                    {
                        violations.Add(new ArchiveViolation(
                            RULE_NAME, ArchiveViolation.Severity.Warn,
                            $"注释 NoRotate 应为 true (ID={annotId})",
                            annotLoc, annot.GetNoRotate()?.ToString().ToLowerInvariant(), "true"));
                    }

                    // Appearance 中不应嵌套 PageBlock
                    var appearance = annot.GetAppearance();
                    if (appearance == null) continue;

                    var nested = DomWalker.Descendants(appearance.GetProxy(), "PageBlock");
                    if (nested.Count > 0)
                    {
                        violations.Add(new ArchiveViolation(
                            RULE_NAME, ArchiveViolation.Severity.Warn,
                            $"注释 Appearance 中存在 PageBlock 嵌套 (ID={annotId})，共 {nested.Count} 层",
                            annotLoc, "存在嵌套", "展平"));
                    }
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查注释时异常: " + e.Message));
        }

        return violations;
    }
}
