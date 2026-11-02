using System.Globalization;
using Ofd.Core.Annotation;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Res;

namespace Ofd.Pkg.Container;

/// <summary>文档容器（Java DocDir）：Doc_N/Document.xml + Pages/Signs/Res/Tags/Temps/Annots 子容器。</summary>
public class DocDir : VirtualContainer
{
    /// <summary>文档容器名称前缀。</summary>
    public const string DocContainerPrefix = "Doc_";

    /// <summary>文档根节点描述文件名称。</summary>
    public const string DocumentFileName = "Document.xml";

    /// <summary>文档公共资源索引描述文件名称。</summary>
    public const string PublicResFileName = "PublicRes.xml";

    /// <summary>文档自身资源索引描述文件名称。</summary>
    public const string DocumentResFileName = "DocumentRes.xml";

    /// <summary>数字签名容器名称。</summary>
    public const string SignsDir = "Signs";

    /// <summary>数字签名容器名称前缀。</summary>
    public const string SignContainerPrefix = "Sign_";

    /// <summary>自定义标签容器名称（GMT0099 OFD 2.0）。</summary>
    public const string TagsDir = "Tags";

    /// <summary>临时文件容器（OFD 2.0）。</summary>
    public const string TempsDir = "Temps";

    /// <summary>页面容器。</summary>
    public const string PagesDir = "Pages";

    /// <summary>页面容器名称前缀。</summary>
    public const string PageContainerPrefix = "Page_";

    /// <summary>资源容器。</summary>
    public const string ResDir = "Res";

    /// <summary>注释文件虚拟容器（GMT0099 OFD 2.0）。</summary>
    public const string AnnotsDir = "Annots";

    /// <summary>注释入口文件名称。</summary>
    public const string AnnotationsFileName = "Annotations.xml";

    /// <summary>附件入口文件名称。</summary>
    public const string Attachments = "Attachments.xml";

    /// <summary>表示第几份文档，从 0 开始。</summary>
    private readonly int _index;

    public DocDir(string fullDir) : base(fullDir)
    {
        string indexStr = ContainerName.Replace(DocContainerPrefix, "");
        if (!int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out _index))
        {
            Clean();
            throw new ArgumentException($"不合法的文件目录名称：{ContainerName}，目录名称应为 Doc_N");
        }
    }

    /// <summary>获取文档索引（从 0 起）。</summary>
    public int GetIndex() => _index;

    /// <summary>获取文档根节点（不存在抛 FileNotFoundException）。</summary>
    public Document GetDocument() => new(GetObjByFile(DocumentFileName));

    /// <summary>设置文档根节点。</summary>
    public DocDir SetDocument(Document document)
    {
        PutObj(DocumentFileName, document);
        return this;
    }

    /// <summary>获取文档公共资源索引（不存在抛 FileNotFoundException）。</summary>
    public Res GetPublicRes() => new(GetObjByFile(PublicResFileName));

    /// <summary>设置文档公共资源索引。</summary>
    public DocDir SetPublicRes(Res publicRes)
    {
        PutObj(PublicResFileName, publicRes);
        return this;
    }

    /// <summary>获取文档自身资源索引对象（不存在抛 FileNotFoundException）。</summary>
    public Res GetDocumentRes() => new(GetObjByFile(DocumentResFileName));

    /// <summary>设置文档自身资源索引。</summary>
    public DocDir SetDocumentRes(Res documentRes)
    {
        PutObj(DocumentResFileName, documentRes);
        return this;
    }

    /// <summary>获取注释列表对象（Annotations.xml；不存在抛 FileNotFoundException）。</summary>
    public Annotations GetAnnotations() => new(GetObjByFile(AnnotationsFileName));

    /// <summary>设置注释列表对象（推荐使用 AnnotsDir.SetAnnotations）。</summary>
    public DocDir SetAnnotations(Annotations annotations)
    {
        PutObj(AnnotationsFileName, annotations);
        return this;
    }

    /// <summary>获取资源容器（不存在抛 FileNotFoundException）。</summary>
    public ResDir GetRes() => GetContainer(ResDir, p => new global::Ofd.Pkg.Container.ResDir(p));

    /// <summary>获取资源文件夹（不存在则创建）。</summary>
    public ResDir ObtainRes() => ObtainContainer(ResDir, p => new global::Ofd.Pkg.Container.ResDir(p));

    /// <summary>获取数字签名存储目录（不存在抛 FileNotFoundException）。</summary>
    public SignsDir GetSigns() => GetContainer(SignsDir, p => new global::Ofd.Pkg.Container.SignsDir(p));

    /// <summary>获取数字签名存储目录（不存在则创建）。</summary>
    public SignsDir ObtainSigns() => ObtainContainer(SignsDir, p => new global::Ofd.Pkg.Container.SignsDir(p));

    /// <summary>获取页面存储目录（不存在抛 FileNotFoundException）。</summary>
    public PagesDir GetPages() => GetContainer(PagesDir, p => new global::Ofd.Pkg.Container.PagesDir(p));

    /// <summary>获取页面存储目录（不存在则创建）。</summary>
    public PagesDir ObtainPages() => ObtainContainer(PagesDir, p => new global::Ofd.Pkg.Container.PagesDir(p));

    /// <summary>获取自定义标签容器（不存在则创建；GMT0099 OFD 2.0）。</summary>
    public VirtualContainer ObtainTags() => ObtainContainer(TagsDir, p => new VirtualContainer(p));

    /// <summary>获取自定义标签容器（不存在抛 FileNotFoundException）。</summary>
    public VirtualContainer GetTags() => GetContainer(TagsDir, p => new VirtualContainer(p));

    /// <summary>获取模板容器（不存在则创建；GMT0099 OFD 2.0）。</summary>
    public TempsDir ObtainTemps() => ObtainContainer(TempsDir, p => new global::Ofd.Pkg.Container.TempsDir(p));

    /// <summary>获取模板容器（不存在抛 FileNotFoundException）。</summary>
    public TempsDir GetTemps() => GetContainer(TempsDir, p => new global::Ofd.Pkg.Container.TempsDir(p));

    /// <summary>获取注释容器（不存在则创建；GMT0099 OFD 2.0）。</summary>
    public AnnotsDir ObtainAnnots() => ObtainContainer(AnnotsDir, p => new global::Ofd.Pkg.Container.AnnotsDir(p));

    /// <summary>获取注释容器（不存在抛 FileNotFoundException）。</summary>
    public AnnotsDir GetAnnots() => GetContainer(AnnotsDir, p => new global::Ofd.Pkg.Container.AnnotsDir(p));

    /// <summary>增加资源（复制到 Res 目录）。</summary>
    public DocDir AddResource(string resource)
    {
        ObtainRes().Add(resource);
        return this;
    }

    /// <summary>增加资源并返回资源在文档中的实际路径。</summary>
    public string? AddResourceWithPath(string resource) => ObtainRes().AddWithPath(resource);

    /// <summary>获取资源路径（不存在抛 FileNotFoundException）。</summary>
    public string GetResource(string name) => ObtainRes().Get(name);
}
