using Ofd.Layout.Element;
using Ofd.Layout.Engine;

namespace Ofd.Layout;

/// <summary>
/// 流式布局的页面内容（Java StreamCollect）：编辑文档时向指定页插入流式内容，
/// 内容超过一页时自动分页并追加在原页之后。
/// </summary>
public class StreamCollect
{
    /// <summary>开始页面位置（页码从 1 起）。</summary>
    private int? _pageNum;

    /// <summary>流式页面内容。</summary>
    private readonly List<Div> _content;

    public StreamCollect()
    {
        _pageNum = null;
        _content = new List<Div>();
    }

    /// <summary>创建插入页面流。</summary>
    public StreamCollect(int pageNum) : this() => _pageNum = pageNum;

    public List<Div> GetContent() => _content;

    public StreamCollect Add(Div? element)
    {
        if (element != null) _content.Add(element);
        return this;
    }

    public int? GetPageNum() => _pageNum;

    public StreamCollect SetPageNum(int? pageNum)
    {
        _pageNum = pageNum;
        return this;
    }

    /// <summary>分析流式内容并转换为虚拟页面集合。</summary>
    public List<VirtualPage> Analyze(PageLayout pageLayout)
    {
        var sgmEngine = new SegmentationEngine(pageLayout);
        var analyzer = new StreamingLayoutAnalyzer(pageLayout);

        var sgmQueue = sgmEngine.Process(_content);
        var virtualPageList = analyzer.Analyze(sgmQueue);

        if (_pageNum != null)
        {
            int start = _pageNum.Value;
            foreach (var vPage in virtualPageList)
                vPage.SetPageNum(start++);
        }

        return virtualPageList;
    }
}
