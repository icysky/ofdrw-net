using Ofd.Archive.Check.Rule;
using Ofd.Reader;

namespace Ofd.Archive.Check;

/// <summary>
/// OFD-A 合规检查器（Java OFDArchiveChecker）：聚合规则执行 GB/T 42133-2022 检查。
/// </summary>
public class OFDArchiveChecker
{
    private readonly List<IArchiveRule> _rules;

    /// <summary>使用默认规则集创建检查器。</summary>
    public OFDArchiveChecker() : this(LoadDefaultRules()) { }

    /// <summary>使用自定义规则集创建检查器。</summary>
    public OFDArchiveChecker(List<IArchiveRule> rules) => _rules = new List<IArchiveRule>(rules);

    /// <summary>执行全部检查（单条规则异常不影响其他规则）。</summary>
    public List<ArchiveViolation> Check(OFDReader reader)
    {
        var ofdDir = reader.GetOFDDir();
        var all = new List<ArchiveViolation>();

        foreach (var rule in _rules)
        {
            try
            {
                var violations = rule.Check(reader, ofdDir);
                if (violations != null) all.AddRange(violations);
            }
            catch (Exception e)
            {
                all.Add(new ArchiveViolation(rule.GetType().Name, ArchiveViolation.Severity.Error,
                    "规则执行异常: " + e.Message));
            }
        }

        // 按规则名排序，保证输出稳定
        all.Sort((a, b) => string.CompareOrdinal(a.GetRuleName(), b.GetRuleName()));
        return all;
    }

    /// <summary>对 OFD 文件执行检查（自动管理 Reader 生命周期）。</summary>
    public List<ArchiveViolation> Check(string ofdPath)
    {
        using var reader = new OFDReader(ofdPath);
        return Check(reader);
    }

    /// <summary>
    /// 默认规则集（GB/T 42133-2022 Phase 1）。
    /// <para>注：随规则类移植进度逐步扩充。</para>
    /// </summary>
    private static List<IArchiveRule> LoadDefaultRules() => new()
    {
        // 文件结构规则（6.1-6.2）
        new DocTypeRule(),
        new SingleDocRule(),
        new ExternalResourceRule(),
        new PermissionRule(),
        new NonGotoActionRule(),
        new OutlineActionRule(),
        new ExtensionRule(),

        // 资源规则（6.2.6）
        new ColorSpaceRule(),
        new AudioVideoRule(),
        new ImageExtensionRule(),
        new FontSubsetRule(),
        new TextHScaleRule(),
        new ImageResourceRegRule(),

        // 页面内容规则（6.3）
        new PageBlockDepthRule(),
        new ClipAreaRule(),

        // 注释 / 签名 / 附件规则
        new AnnotationRule(),
        new SignatureRule(),
        new AttachmentRule(),
        new ColorProfileRule(),
        new ImageFormatRule(),
        new ResourcePlacementRule(),

        // 内容规则（6.5-6.6）
        new ImageInterpolateRule(),
        new TextSizeRule(),
    };
}
