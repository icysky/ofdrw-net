using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 17：图像对象 Interpolate 必须为 false（GB/T 42133-2022 6.5）。
/// </summary>
public class ImageInterpolateRule : IArchiveRule
{
    public const string RULE_NAME = "IMAGE_INTERPOLATE";

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

                foreach (var imgObj in DomWalker.Descendants(page.GetProxy(), "ImageObject"))
                {
                    if (!"true".Equals(imgObj.Attr("Interpolate"), StringComparison.OrdinalIgnoreCase)) continue;

                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Warn,
                        "图像对象 Interpolate=true，OFD-A 要求设置为 false",
                        pageLoc, "true", "false"));
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查图像插值时异常: " + e.Message));
        }

        return violations;
    }
}
