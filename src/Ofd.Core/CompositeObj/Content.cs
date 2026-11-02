using Ofd.Core.BasicStructure.PageObj.Layer.Block;

namespace Ofd.Core.CompositeObj;

/// <summary>内容的矢量描述（第 13 章 表 50）：以 CT_PageBlock 承载，元素名 Content。</summary>
public class Content : CT_PageBlock
{
    public Content(Dom.DomElement proxy) : base(proxy) { }

    public Content() : base("Content") { }
}
