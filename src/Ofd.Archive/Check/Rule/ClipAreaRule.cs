using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 15：裁剪区面积不应为 0（应删除并设置 Visible=false）（GB/T 42133-2022 6.3）。
/// </summary>
public class ClipAreaRule : IArchiveRule
{
    public const string RULE_NAME = "CLIP_AREA";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            int pageCount = reader.GetNumberOfPages();
            for (int i = 1; i <= pageCount; i++)
            {
                var page = reader.GetPage(i);
                var pageLoc = reader.GetPageAbsLoc(i);

                foreach (var clip in DomWalker.Descendants(page.GetProxy(), "ClipArea"))
                    CheckClipArea(clip, pageLoc, violations);
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查裁剪区时异常: " + e.Message));
        }

        return violations;
    }

    /// <summary>检查单个裁剪区。</summary>
    private static void CheckClipArea(DomElement clip, ST_Loc? pageLoc, List<ArchiveViolation> violations)
    {
        string? boundaryStr = clip.Attr("Boundary");
        if (boundaryStr == null) return;

        var boundary = ST_Box.GetInstance(boundaryStr);
        if (boundary == null) return;

        if (boundary.Width <= 0 || boundary.Height <= 0)
        {
            violations.Add(new ArchiveViolation(
                RULE_NAME, ArchiveViolation.Severity.Warn,
                "裁剪区面积为 0，应删除并设置 Visible=false",
                pageLoc, "面积=0", "Visible=false"));
        }
    }
}
