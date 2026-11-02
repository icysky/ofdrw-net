namespace Ofd.Core.Extensions;

/// <summary>扩展信息列表（第 17 章 图 83 表 64）：根节点 Extensions，含 0~多个 Extension。</summary>
public class Extensions : OFDElement
{
    public Extensions(Dom.DomElement proxy) : base(proxy) { }

    public Extensions() : base("Extensions") { }

    /// <summary>【可选】增加 扩展信息节点（null 忽略）。</summary>
    public Extensions AddExtension(CT_Extension? extension)
    {
        if (extension == null) return this;
        Add(extension);
        return this;
    }

    /// <summary>【可选】获取 扩展信息节点序列。</summary>
    public List<CT_Extension> GetExtensions() => GetOFDElements("Extension", e => new CT_Extension(e));
}
