namespace Ofd.Pkg.Container;

/// <summary>注释容器（Java AnnotsDir，GMT0099 OFD 2.0）。</summary>
public class AnnotsDir : VirtualContainer
{
    public AnnotsDir(string fullDir) : base(fullDir) { }

    /// <summary>设置 注释列表对象（写入 Annotations.xml）。</summary>
    public AnnotsDir SetAnnotations(Ofd.Core.Annotation.Annotations annotations)
    {
        PutObj(DocDir.AnnotationsFileName, annotations);
        return this;
    }

    /// <summary>通过索引获取页面容器（目录不存在则创建）。</summary>
    public PageDir ObtainByIndex(int index)
    {
        string containerName = PageDir.PageContainerPrefix + index;
        return ObtainContainer(containerName, p => new PageDir(p));
    }

    /// <summary>通过容器名获取页面容器（不存在抛 FileNotFoundException）。</summary>
    public PageDir GetPageDir(string containerName) => GetContainer(containerName, p => new PageDir(p));
}
