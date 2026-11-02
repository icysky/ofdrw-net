using Ofd.Core.BasicStructure.Res;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 13：图像资源禁止使用扩展机制加入自定义数据（GB/T 42133-2022 6.2.6f）。
/// <para>
/// 判定方式：图像 MultiMedia 的子元素前缀不是空或 <c>ofd</c> 时视为自定义扩展。
/// （Java 通过 dom4j 命名空间 URI 判断；C# DOM 不保留 URI，改用前缀判定。）
/// </para>
/// </summary>
public class ImageExtensionRule : IArchiveRule
{
    public const string RULE_NAME = "IMAGE_EXTENSION";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        var resMgt = reader.GetResMgt();
        if (resMgt == null) return violations;

        try
        {
            foreach (var media in resMgt.GetMultiMedias())
            {
                if (media.GetType() != MediaType.Image) continue;

                foreach (var child in media.GetProxy().Kids)
                {
                    if (child is not Ofd.Core.Dom.DomElement el) continue;

                    string prefix = el.Prefix ?? "";
                    if (prefix.Length == 0 || prefix == "ofd") continue;

                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Warn,
                        "图像资源包含自定义扩展数据: " + el.Name,
                        media.GetMediaFile(), el.Name, "删除"));
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查图像扩展时异常: " + e.Message));
        }

        return violations;
    }
}
