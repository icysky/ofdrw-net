using Ofd.Layout.Element;

namespace Ofd.Layout.Engine;

/// <summary>
/// 流式布局分析器（Java StreamingLayoutAnalyzer）：把「段」装配为「虚拟页面」。
/// </summary>
public class StreamingLayoutAnalyzer
{
    /// <summary>页面固定可用工作区域。</summary>
    private readonly Rectangle _pageWorkArea;

    /// <summary>虚拟页面尺寸。</summary>
    private readonly PageLayout _layout;

    /// <summary>布局产生的虚拟页面序列。</summary>
    private readonly List<VirtualPage> _vPageList;

    /// <summary>当前剩余工作区域。</summary>
    private Rectangle _remainArea = Rectangle.Empty;

    /// <summary>当前虚拟页面。</summary>
    private VirtualPage? _vPage;

    /// <summary>连续无进展（切分后高度未减少）次数。</summary>
    private int _stallCount;

    /// <summary>允许的连续无进展次数上限。</summary>
    private const int MaxStall = 8;

    private const double Epsilon = 1e-9;

    public StreamingLayoutAnalyzer(PageLayout layout)
    {
        _pageWorkArea = layout.GetWorkerArea();
        _layout = layout;
        _vPageList = new List<VirtualPage>();
    }

    /// <summary>分析段序列，输出虚拟页面序列。</summary>
    public List<VirtualPage> Analyze(List<Segment>? segmentSequence)
    {
        if (segmentSequence == null || segmentSequence.Count == 0)
            return new List<VirtualPage>();

        var seq = new LinkedList<Segment>(segmentSequence);
        if (_vPageList.Count == 0) AddNewPage(); // 初始化页面

        while (seq.Count > 0)
        {
            var segment = seq.First!.Value;
            seq.RemoveFirst();

            // 页面无剩余空间 → 换页
            if (_remainArea.GetHeight() == 0) AddNewPage();

            // 填充段：占满剩余空间
            if (segment.IsRemainAreaFiller())
            {
                _remainArea.SetHeight(0d);
                continue;
            }

            // 高度足够
            if (segment.GetHeight() <= _remainArea.GetHeight())
            {
                var area = _remainArea.Reduce(segment.GetHeight());
                ElementPositioning(segment, area);
                continue;
            }

            // 不可拆分
            if (!segment.IsBlockable())
            {
                if (segment.GetHeight() > _pageWorkArea.GetHeight())
                {
                    // 不可拆分且高于整页 → 舍弃（与 Java 一致：仅注释告警）
                }
                else
                {
                    // 高度小于整页 → 换页后重新入队
                    AddNewPage();
                    seq.AddFirst(segment);
                }
                continue;
            }

            // 可拆分 → 按剩余空间切分为两块
            var blocks = SegmentBlocking(segment, _remainArea.Clone());
            if (blocks != null && blocks.Length > 0)
            {
                // 防御：切分后首段高度若未减少，说明无法推进（会无限切分并耗尽内存）。
                // 这里限制连续无进展次数，超过即抛出明确异常而不是 OOM。
                if (blocks[0].GetHeight() >= segment.GetHeight() - Epsilon)
                {
                    if (++_stallCount > MaxStall)
                    {
                        throw new InvalidOperationException(
                            "布局无法推进：元素高度超出可用空间且切分后高度未减少（请检查元素的固定高度与 Integrity 设置）");
                    }
                }
                else
                {
                    _stallCount = 0;
                }

                // 逆序入队，保证原顺序
                for (int i = blocks.Length - 1; i >= 0; i--) seq.AddFirst(blocks[i]);
            }
        }

        return _vPageList;
    }

    /// <summary>按剩余空间切分段（返回两块：本页与下一页）。</summary>
    private static Segment[] SegmentBlocking(Segment segment, Rectangle area)
    {
        var sgm1 = new Segment(segment.GetWidth());
        var sgmNext = new Segment(segment.GetWidth());

        foreach (var item in segment)
        {
            var div = item.Key;
            var rec = item.Value;
            double availableHeight = area.GetHeight();

            // 1) 能放入剩余空间
            if (rec.GetHeight() <= availableHeight)
            {
                sgm1.TryAdd(div);
                sgmNext.TryAdd(Div.Placeholder(rec, div.GetFloat()));
                continue;
            }

            // 2) 放不下且元素不可分割
            if (div.IsIntegrity() == true)
            {
                sgm1.TryAdd(Div.Placeholder(rec.GetWidth(), availableHeight, div.GetFloat()));
                sgmNext.TryAdd(div);
                continue;
            }

            // 3) 放不下且可分割 → 切分元素
            var split = div.Split(availableHeight);
            if (split.Length >= 2)
            {
                sgm1.TryAdd(split[0]);
                sgmNext.TryAdd(split[1]);
            }
            else
            {
                sgm1.TryAdd(div);
            }
        }

        return new[] { sgm1, sgmNext };
    }

    /// <summary>段内元素定位并加入虚拟页面。</summary>
    private void ElementPositioning(Segment segment, Rectangle area)
    {
        // 居中布局
        if (segment.IsCenterFloat())
        {
            double totalWidth = segment.GetSizeList().Sum(r => r.GetWidth());
            double offsetX = area.GetX() + (area.GetWidth() - totalWidth) / 2;

            foreach (var item in segment)
            {
                item.Key.SetX(offsetX);
                offsetX += item.Value.GetWidth();
            }
        }
        else
        {
            // 左右浮动
            double startX = area.GetX();
            double endX = startX + area.GetWidth();

            foreach (var item in segment)
            {
                var itemDiv = item.Key;
                var box = item.Value;

                if (itemDiv.GetFloat() == AFloat.Left)
                {
                    itemDiv.SetX(startX);
                    startX += box.GetWidth();
                }
                else if (itemDiv.GetFloat() == AFloat.Right)
                {
                    itemDiv.SetX(endX - box.GetWidth());
                    endX -= box.GetWidth();
                }
                // center / 其它忽略
            }
        }

        foreach (var item in segment)
        {
            var itemDiv = item.Key;
            if (itemDiv.IsPlaceholder()) continue; // 占位符不参与绘制

            // 流式 → 绝对定位
            itemDiv.SetY(area.GetY());

            if (itemDiv.GetPosition() == Position.Relative)
            {
                double x = itemDiv.GetX() ?? 0;
                double y = itemDiv.GetY() ?? 0;
                itemDiv.SetX(x + itemDiv.GetLeft() - itemDiv.GetRight()).SetY(y + itemDiv.GetTop());
            }

            itemDiv.SetPosition(Position.Absolute);
            _vPage!.AddUnsafe(itemDiv);
        }
    }

    /// <summary>创建新页面。</summary>
    private void AddNewPage()
    {
        _remainArea = _pageWorkArea.Clone();
        _vPage = new VirtualPage(_layout);
        _vPageList.Add(_vPage);
    }
}
