using System.Globalization;
using System.Text.RegularExpressions;
using Ofd.Core.Annotation.PageAnnot;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicType;

namespace Ofd.Pkg.Container;

/// <summary>页面目录容器（Java PageDir）：Content.xml / PageRes.xml / Annot_N.xml。</summary>
public class PageDir : VirtualContainer
{
    public static readonly Regex AnnotFileRegex = new(@"Annot_(\d+).xml", RegexOptions.Compiled);

    /// <summary>页面容器名称前缀。</summary>
    public const string PageContainerPrefix = "Page_";

    /// <summary>页面描述文件名称。</summary>
    public const string ContentFileName = "Content.xml";

    /// <summary>页面资源描述文件名称。</summary>
    public const string PageResFileName = "PageRes.xml";

    /// <summary>页面关联的注解对象文件名称。</summary>
    public const string AnnotationFileName = "Annotation.xml";

    /// <summary>注释文件前缀（GMT0099 OFD 2.0）。</summary>
    public const string AnnotFilePrefix = "Annot_";

    /// <summary>页面索引（目录名去前缀后的字符串，可能是非数字的自定义名称）。</summary>
    private readonly string _index;

    /// <summary>最大注释文件索引（-1 表示未扫描）。</summary>
    private int _maxAnnotIndex = -1;

    public PageDir(string fullDir) : base(fullDir)
    {
        _index = ContainerName.Replace(PageContainerPrefix, "");
    }

    /// <summary>获取页面索引（非数字名称抛 FormatException，与 Java NumberFormatException 对应）。</summary>
    public int GetIndex() => int.Parse(_index, CultureInfo.InvariantCulture);

    /// <summary>获取页面索引字符串（非规范时为自定义名称）。</summary>
    public string GetIndexStr() => _index;

    /// <summary>获取页面资源描述文件（不存在抛 FileNotFoundException）。</summary>
    public Res GetPageRes() => new(GetObjByFile(PageResFileName));

    /// <summary>设置页面资源描述对象。</summary>
    public PageDir SetPageRes(Res pageRes)
    {
        PutObj(PageResFileName, pageRes);
        return this;
    }

    /// <summary>获取分页注释文件（传统 Annotation.xml；不存在抛 FileNotFoundException）。</summary>
    public PageAnnot GetPageAnnot() => new(GetObjByFile(AnnotationFileName));

    /// <summary>
    /// 获取容器内所有注释对象（文件名 → 注释对象）。
    /// 注：Java 上游循环内误用固定文件名 Annotation.xml（导致所有键映射同一对象），
    /// C# 已修正为逐个文件按其名读取。
    /// </summary>
    public Dictionary<string, PageAnnot> GetPageAnnots()
    {
        var res = new Dictionary<string, PageAnnot>();
        foreach (var item in Directory.GetFiles(GetContainerPath()))
        {
            string fileName = Path.GetFileName(item).ToLowerInvariant();
            if (!fileName.StartsWith(AnnotFilePrefix.ToLowerInvariant(), StringComparison.Ordinal)) continue;
            if (!fileName.EndsWith(".xml", StringComparison.Ordinal)) continue;

            try
            {
                res[Path.GetFileName(item)] = new PageAnnot(GetObjByFile(Path.GetFileName(item)));
            }
            catch (Exception)
            {
                // 与 Java 一致：单个文件解析失败忽略
            }
        }
        return res;
    }

    /// <summary>设置分页注释文件（Annotation.xml）。</summary>
    public PageDir SetPageAnnot(PageAnnot pageAnnot)
    {
        PutObj(AnnotationFileName, pageAnnot);
        return this;
    }

    /// <summary>获取页面资源目录（不存在则创建）。</summary>
    public ResDir ObtainRes() => ObtainContainer("Res", p => new ResDir(p));

    /// <summary>获取页面资源目录（不存在抛 FileNotFoundException）。</summary>
    public ResDir GetResDir() => GetContainer("Res", p => new ResDir(p));

    /// <summary>向页面中增加页面资源（复制）。</summary>
    public PageDir Add(string resource)
    {
        ObtainRes().Add(resource);
        return this;
    }

    /// <summary>向页面加入新的注释文件（自动编号 Annot_N.xml），返回容器内绝对路径。</summary>
    public ST_Loc? AddAnnot(PageAnnot? pageAnnot)
    {
        if (pageAnnot == null) return null;
        _maxAnnotIndex = GetMaxAnnotFileIndex() + 1;
        string fileName = AnnotFilePrefix + _maxAnnotIndex.ToString(CultureInfo.InvariantCulture) + ".xml";
        return AddAnnot(fileName, pageAnnot);
    }

    /// <summary>向页面内添加指定文件名的注释，返回容器内绝对路径。</summary>
    public ST_Loc AddAnnot(string fileName, PageAnnot pageAnnot)
    {
        PutObj(fileName, pageAnnot);
        return GetAbsLoc().Cat(fileName);
    }

    /// <summary>获取 Page_N 容器中最大的注释文件索引号（无则 -1；结果缓存）。</summary>
    public int GetMaxAnnotFileIndex()
    {
        if (_maxAnnotIndex >= 0) return _maxAnnotIndex;

        int max = -1;
        foreach (var item in Directory.GetFiles(GetContainerPath()))
        {
            string fileName = Path.GetFileName(item).ToLowerInvariant();
            if (!fileName.StartsWith(AnnotFilePrefix.ToLowerInvariant(), StringComparison.Ordinal)) continue;
            string numStr = fileName.Replace(AnnotFilePrefix.ToLowerInvariant(), "").Split('.')[0];
            if (int.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > max)
                max = n;
        }
        _maxAnnotIndex = max;
        return _maxAnnotIndex;
    }

    /// <summary>获取页面资源文件路径（不存在抛 FileNotFoundException）。</summary>
    public string Get(string name) => ObtainRes().GetFileOrThrow(name);

    /// <summary>获取页面描述对象（Content.xml；不存在抛 FileNotFoundException）。</summary>
    public PageObj GetContent() => new(GetObjByFile(ContentFileName));

    /// <summary>设置页面描述。</summary>
    public PageDir SetContent(PageObj content)
    {
        PutObj(ContentFileName, content);
        return this;
    }
}
