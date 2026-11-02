using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 23：文字对象必须设置 Size（GB/T 42133-2022 6.6）。
/// </summary>
public class TextSizeRule : IArchiveRule
{
    public const string RULE_NAME = "TEXT_SIZE";

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

                foreach (var textObj in DomWalker.Descendants(page.GetProxy(), "TextObject"))
                {
                    string? size = textObj.Attr("Size");
                    if (!string.IsNullOrEmpty(size)) continue;

                    string id = textObj.Attr("ID") ?? "?";
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Warn,
                        $"文字对象缺少 Size 属性 (ID={id})",
                        pageLoc, "缺失", "应设置 Size 属性"));
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查文字 Size 时异常: " + e.Message));
        }

        return violations;
    }
}
