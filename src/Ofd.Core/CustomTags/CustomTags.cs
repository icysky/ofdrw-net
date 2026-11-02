namespace Ofd.Core.CustomTags;

/// <summary>自定义标引（第 16 章 图 82 表 63）：一个文档可带多个自定义标引。</summary>
public class CustomTags : OFDElement
{
    public CustomTags(Dom.DomElement proxy) : base(proxy) { }

    public CustomTags() : base("CustomTags") { }

    /// <summary>【可选】增加 自定义标引入口（null 忽略）。</summary>
    public CustomTags AddCustomTag(CustomTag? customTag)
    {
        if (customTag == null) return this;
        Add(customTag);
        return this;
    }

    /// <summary>【可选】获取 自定义标引入口列表。</summary>
    public List<CustomTag> GetCustomTags() => GetOFDElements("CustomTag", e => new CustomTag(e));
}
