using Ofd.Core.BasicStructure.Res;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 19：多页共用的栅格图像应注册到 DocumentRes（GB/T 42133-2022 6.5a）。
/// </summary>
public class ImageResourceRegRule : IArchiveRule
{
    public const string RULE_NAME = "IMAGE_RESOURCE_REG";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            int pageCount = reader.GetNumberOfPages();

            // 统计每个图像 ResourceID 被哪些页面引用
            var imageRefs = new Dictionary<string, HashSet<int>>();

            for (int i = 1; i <= pageCount; i++)
            {
                var page = reader.GetPage(i);
                CollectImageRefs(page.GetProxy(), i, imageRefs);
            }

            var resMgt = reader.GetResMgt();
            if (resMgt == null) return violations;

            foreach (var (resId, pages) in imageRefs)
            {
                if (pages.Count < 2) continue;

                var media = resMgt.GetMultiMedia(resId);
                if (media == null || media.GetType() != MediaType.Image) continue;

                violations.Add(new ArchiveViolation(
                    RULE_NAME, ArchiveViolation.Severity.Warn,
                    $"图像资源 (ID={resId}) 被 {pages.Count} 个页面引用，建议注册到 DocumentRes",
                    media.GetMediaFile(), "PageRes", "DocumentRes"));
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查图像注册时异常: " + e.Message));
        }

        return violations;
    }

    /// <summary>递归收集容器内 ImageObject 的 ResourceID 引用。</summary>
    private static void CollectImageRefs(DomElement container, int pageNum,
        Dictionary<string, HashSet<int>> imageRefs)
    {
        foreach (var kid in container.Kids)
        {
            if (kid is not DomElement child) continue;

            if (child.Name == "ImageObject")
            {
                string? resId = child.Attr("ResourceID");
                if (resId != null)
                {
                    if (!imageRefs.TryGetValue(resId, out var set))
                    {
                        set = new HashSet<int>();
                        imageRefs[resId] = set;
                    }
                    set.Add(pageNum);
                }
            }

            CollectImageRefs(child, pageNum, imageRefs);
        }
    }
}
