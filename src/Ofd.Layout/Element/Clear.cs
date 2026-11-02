namespace Ofd.Layout.Element;

/// <summary>对段的占用情况（Java Clear）。</summary>
public enum Clear
{
    /// <summary>共享：两侧都允许出现元素。</summary>
    None,

    /// <summary>左侧不允许出现元素。</summary>
    Left,

    /// <summary>右侧不允许出现元素。</summary>
    Right,

    /// <summary>两侧不允许出现元素，默认值。</summary>
    Both,
}
