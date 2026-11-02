namespace Ofd.Core.BasicStructure.Res.Resources;

/// <summary>包含了一组文档所有多媒体的描述（7.9 图 20 表 18）。</summary>
public class MultiMedias : OFDElement, IOFDResource
{
    public MultiMedias(Dom.DomElement proxy) : base(proxy) { }

    public MultiMedias() : base("MultiMedias") { }

    /// <summary>【必选】增加 多媒体资源描述（必须有 ID；null 忽略）。</summary>
    public MultiMedias AddMultiMedia(CT_MultiMedia? multiMedia)
    {
        if (multiMedia == null) return this;
        if (multiMedia.GetID() == null)
            throw new ArgumentException("多媒体资源描述ID不能为空");
        Add(multiMedia);
        return this;
    }

    /// <summary>【必选】获取 多媒体资源描述列表。</summary>
    public List<CT_MultiMedia> GetMultiMedias() => GetOFDElements("MultiMedia", e => new CT_MultiMedia(e));
}
