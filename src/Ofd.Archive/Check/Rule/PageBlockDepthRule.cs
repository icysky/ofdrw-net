using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 14：PageBlock 嵌套深度不超过 3（GB/T 42133-2022 6.3）。
/// </summary>
public class PageBlockDepthRule : IArchiveRule
{
    public const string RULE_NAME = "PAGEBLOCK_DEPTH";

    /// <summary>最大允许嵌套深度。</summary>
    private const int MAX_DEPTH = 3;

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

                foreach (var pageBlock in DomWalker.Descendants(page.GetProxy(), "PageBlock"))
                    CheckDepth(pageBlock, 1, pageLoc, violations);
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查 PageBlock 深度时异常: " + e.Message));
        }

        return violations;
    }

    /// <summary>递归检查嵌套深度。</summary>
    private static void CheckDepth(DomElement element, int depth, ST_Loc? pageLoc, List<ArchiveViolation> violations)
    {
        if (depth > MAX_DEPTH)
        {
            violations.Add(new ArchiveViolation(
                RULE_NAME, ArchiveViolation.Severity.Error,
                $"PageBlock 嵌套深度超过最大允许值: {depth} > {MAX_DEPTH}",
                pageLoc, depth.ToString(), "≤" + MAX_DEPTH));
            return; // 不再深入
        }

        foreach (var child in element.Kids)
        {
            if (child is DomElement el && el.Name == "PageBlock")
                CheckDepth(el, depth + 1, pageLoc, violations);
        }
    }
}
