using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 18：资源应在正确位置定义（GB/T 42133-2022 6.2.6a）。
/// <para>ColorSpace/Font 应在 PublicRes；Image/VectorG/DrawParam 应在 DocumentRes 或 PageRes。</para>
/// </summary>
public class ResourcePlacementRule : IArchiveRule
{
    public const string RULE_NAME = "RESOURCE_PLACEMENT";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            var docDir = ofdDir.ObtainDocDefault();

            var publicRes = GetRes(docDir, isPublic: true);
            if (publicRes != null) CheckPublicRes(publicRes, violations);

            // 页面级 PageRes 检查（Java 版为 Phase 3 预留，此处保持一致）
            CheckPageResources(reader, ofdDir, violations);
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查资源位置时异常: " + e.Message));
        }

        return violations;
    }

    /// <summary>检查 PublicRes 中的资源类型是否合规。</summary>
    private static void CheckPublicRes(Res publicRes, List<ArchiveViolation> violations)
    {
        var loc = ST_Loc.GetInstance("PublicRes.xml");

        if (HasResourceType(publicRes, "MultiMedias"))
        {
            violations.Add(new ArchiveViolation(
                RULE_NAME, ArchiveViolation.Severity.Warn,
                "MultiMedia 资源应放在 DocumentRes 或 PageRes，而非 PublicRes",
                loc, "PublicRes", "DocumentRes/PageRes"));
        }

        if (HasResourceType(publicRes, "CompositeGraphicUnits"))
        {
            violations.Add(new ArchiveViolation(
                RULE_NAME, ArchiveViolation.Severity.Warn,
                "VectorG 资源应放在 DocumentRes 或 PageRes，而非 PublicRes",
                loc, "PublicRes", "DocumentRes/PageRes"));
        }

        if (HasResourceType(publicRes, "DrawParams"))
        {
            violations.Add(new ArchiveViolation(
                RULE_NAME, ArchiveViolation.Severity.Warn,
                "DrawParam 应放在 DocumentRes 或 PageRes，而非 PublicRes",
                loc, "PublicRes", "DocumentRes/PageRes"));
        }
    }

    /// <summary>页面资源检查（Phase 3 预留，与 Java 一致暂不实现）。</summary>
    private static void CheckPageResources(OFDReader reader, OFDDir ofdDir, List<ArchiveViolation> violations)
    {
        // Phase 3：遍历各页 PageRes.xml，若出现 ColorSpace/Font 定义则告警
    }

    /// <summary>判断 Res 中是否包含指定类型的资源容器。</summary>
    private static bool HasResourceType(Res res, string elementName)
    {
        foreach (var kid in res.GetProxy().Kids)
        {
            if (kid is DomElement el && el.Name == elementName) return true;
        }
        return false;
    }

    /// <summary>安全获取 Res 对象。</summary>
    private static Res? GetRes(DocDir docDir, bool isPublic)
    {
        try
        {
            return isPublic ? docDir.GetPublicRes() : docDir.GetDocumentRes();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
