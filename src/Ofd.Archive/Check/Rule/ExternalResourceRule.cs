using System.Text.RegularExpressions;
using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 3：资源必须自包含，禁止外部引用（GB/T 42133-2022 6.1）。
/// </summary>
public class ExternalResourceRule : IArchiveRule
{
    public const string RULE_NAME = "EXTERNAL_RESOURCE";

    private static readonly Regex UrlPattern = new("\"(https?://[^\"]+)\"", RegexOptions.Compiled);

    /// <summary>命名空间声明（xmlns / xmlns:ofd），其值是规范 URI 而非资源引用。</summary>
    private static readonly Regex XmlnsPattern = new(@"xmlns(:[\w.-]+)?\s*=\s*""[^""]*""", RegexOptions.Compiled);

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();
        string workDir = reader.GetWorkDir();

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(workDir, "*.xml", SearchOption.AllDirectories);
        }
        catch (IOException)
        {
            return violations;
        }

        foreach (var xmlFile in files)
        {
            try
            {
                string content = File.ReadAllText(xmlFile);

                // 先剥离命名空间声明：xmlns 的值是规范 URI（如 http://www.ofdspec.org/2016），
                // 不是外部资源引用。Java 原实现直接正则全量扫描会误报，此处修正。
                content = XmlnsPattern.Replace(content, "");

                foreach (Match m in UrlPattern.Matches(content))
                {
                    string extUrl = m.Groups[1].Value;
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Error,
                        "引用外部 URL 资源: " + extUrl,
                        ST_Loc.GetInstance(Path.GetRelativePath(workDir, xmlFile).Replace('\\', '/')),
                        extUrl, "包内路径"));
                }
            }
            catch (IOException)
            {
                // 单个文件读取失败忽略
            }
        }

        return violations;
    }
}
