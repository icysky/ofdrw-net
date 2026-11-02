namespace Ofd.Core.BasicStructure.Ofd.DocInfo;

/// <summary>
/// 关键词集合，每一个关键词用一个 "Keyword" 子节点来表达。
/// 表 4 文档元数据属性。
/// </summary>
public class Keywords : OFDElement
{
    public Keywords(Dom.DomElement proxy) : base(proxy) { }

    public Keywords() : base("Keywords") { }

    /// <summary>增加关键字。</summary>
    public Keywords AddKeyword(string keyword)
    {
        AddOFDEntity("Keyword", keyword);
        return this;
    }

    /// <summary>
    /// 获取关键字列表。
    /// 注：Java 上游误查 "Keywords"（容器自身名）导致永远返回空列表，
    /// 此处已修正为查子节点 "Keyword"。
    /// </summary>
    public List<string> GetKeywords() =>
        GetOFDElements("Keyword", ElementText).Select(s => s.Trim()).ToList();
}
