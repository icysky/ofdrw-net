namespace Ofd.Layout.Element;

/// <summary>定位方式（Java Position）。</summary>
public enum Position
{
    /// <summary>静态定位（由渲染器决定），默认值。</summary>
    Static,

    /// <summary>相对定位：使用 left/right 在段内定位。</summary>
    Relative,

    /// <summary>绝对定位：使用 x/y 定位，需要确定的宽高。</summary>
    Absolute,
}
