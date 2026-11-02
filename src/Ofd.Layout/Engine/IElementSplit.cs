using Ofd.Layout.Element;

namespace Ofd.Layout.Engine;

/// <summary>元素分割（Java ElementSplit）：按给定高度切分元素。</summary>
public interface IElementSplit
{
    /// <summary>根据给定高度切分元素（要求元素已确定宽高）。</summary>
    Div[] Split(double sHeight);
}
