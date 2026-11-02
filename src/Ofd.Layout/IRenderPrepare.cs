namespace Ofd.Layout;

/// <summary>渲染准备（Java RenderPrepare）：渲染前完成初步布局与测量。</summary>
public interface IRenderPrepare
{
    /// <summary>执行渲染前的准备工作，返回元素尺寸。</summary>
    Rectangle DoPrepare(double? widthLimit);
}
