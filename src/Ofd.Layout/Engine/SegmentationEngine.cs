using Ofd.Layout.Element;

namespace Ofd.Layout.Engine;

/// <summary>
/// 分段引擎（Java SegmentationEngine）：把流式布局元素队列划分为「段」。
/// </summary>
public class SegmentationEngine
{
    private readonly PageLayout _pageLayout;

    public SegmentationEngine(PageLayout pageLayout) => _pageLayout = pageLayout;

    /// <summary>把流式布局元素队列分段。</summary>
    public List<Segment> Process(List<Div>? streamLayoutQueue)
    {
        double width = _pageLayout.ContentWidth();

        if (streamLayoutQueue == null || streamLayoutQueue.Count == 0)
            return new List<Segment>();

        var res = new List<Segment>();
        var seq = new LinkedList<Div>(streamLayoutQueue);
        var segment = new Segment(width);

        while (seq.Count > 0)
        {
            var div = seq.First!.Value;
            seq.RemoveFirst();

            // 绝对定位元素不参与分段
            if (div.GetPosition() == Position.Absolute) continue;

            // 换行符：结束当前段
            if (div is BR)
            {
                if (segment.IsEmpty()) continue; // 空段无需换行
                res.Add(segment);
                segment = new Segment(width);
                continue;
            }

            bool addSuccess = segment.TryAdd(div);
            if (!addSuccess && !segment.IsEmpty())
            {
                // 段已满：封段并把元素放回队列
                res.Add(segment);
                segment = new Segment(width);
                seq.AddFirst(div);
            }
        }

        // 收尾
        if (!segment.IsEmpty()) res.Add(segment);

        return res;
    }
}
