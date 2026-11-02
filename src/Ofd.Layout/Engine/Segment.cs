using Ofd.Layout.Element;

namespace Ofd.Layout.Engine;

/// <summary>
/// 段对象（Java Segment）：同一行内的元素集合及其尺寸，含剩余宽度与可拆分标记。
/// <para>可枚举（元素, 尺寸）键值对。</para>
/// </summary>
public class Segment : IEnumerable<KeyValuePair<Div, Rectangle>>
{
    /// <summary>段中内容。</summary>
    private readonly List<Div> _content;

    private readonly List<Rectangle> _sizeList;

    /// <summary>段高度（取段内最高元素）。</summary>
    private double _height;

    /// <summary>段最大宽度。</summary>
    private readonly double _width;

    /// <summary>段剩余宽度。</summary>
    private double _remainWidth;

    /// <summary>段是否可拆分（段内出现任一可拆分元素即为 true）。</summary>
    private bool _blockable;

    /// <summary>是否为剩余页面空间填充段。</summary>
    private bool _isRemainAreaFiller;

    public Segment(double width)
    {
        _width = width;
        _remainWidth = width;
        _content = new List<Div>(5);
        _sizeList = new List<Rectangle>(5);
    }

    public double GetHeight() => _height;

    /// <summary>尝试加入元素：能加入返回 true。</summary>
    public bool TryAdd(Div? div)
    {
        if (div == null) return true;

        // 剩余空间已耗尽
        if (_remainWidth == 0) return false;

        // 按段宽度重算元素尺寸
        var blockSize = div.DoPrepare(_width);
        if (blockSize.GetWidth() > _remainWidth) return false;

        // 独占段元素：段内已有元素则无法加入
        if (div.IsBlockElement())
        {
            if (!IsEmpty()) return false;

            _remainWidth = 0;
            Add(div, blockSize);
            return true;
        }

        if (!IsEmpty())
        {
            if (IsCenterFloat())
            {
                // 段内为居中布局
                if (div.GetFloat() != AFloat.Center) return false;
                if (div.GetClear() == Clear.Left) return false;
                if (_content[^1].GetClear() == Clear.Right) return false;
            }
            else
            {
                // 段内非居中布局
                if (div.GetFloat() == AFloat.Left && div.GetClear() == Clear.Left)
                {
                    if (_content.Any(d => d.GetFloat() == AFloat.Left)) return false;
                }

                if (div.GetFloat() == AFloat.Right && div.GetClear() == Clear.Right)
                {
                    if (_content.Any(d => d.GetFloat() == AFloat.Right)) return false;
                }

                if (div.GetFloat() == AFloat.Center) return false;
            }
        }

        // 段内无元素时直接加入
        _remainWidth -= blockSize.GetWidth();
        Add(div, blockSize);
        return true;
    }

    /// <summary>加入元素并更新段高度/可拆分/填充标记。</summary>
    private void Add(Div div, Rectangle blockSize)
    {
        if (_height < blockSize.GetHeight()) _height = blockSize.GetHeight();

        // 出现任一可拆分元素 → 段可拆分
        if (div.IsIntegrity() == false) _blockable = true;

        if (div is PageAreaFiller) _isRemainAreaFiller = true;

        _content.Add(div);
        _sizeList.Add(blockSize);
    }

    /// <summary>段是否可拆分。</summary>
    public bool IsBlockable() => _blockable;

    /// <summary>段内是否为居中布局。</summary>
    public bool IsCenterFloat()
    {
        if (IsEmpty()) return true;
        return _content.All(d => d.GetFloat() == AFloat.Center);
    }

    /// <summary>是否空段。</summary>
    public bool IsEmpty() => _content.Count == 0;

    /// <summary>是否剩余空间填充段。</summary>
    public bool IsRemainAreaFiller() => _isRemainAreaFiller;

    public List<Div> GetContent() => _content;

    public List<Rectangle> GetSizeList() => _sizeList;

    public double GetWidth() => _width;

    public IEnumerator<KeyValuePair<Div, Rectangle>> GetEnumerator()
    {
        for (int i = 0; i < _content.Count; i++)
            yield return new KeyValuePair<Div, Rectangle>(_content[i], _sizeList[i]);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
