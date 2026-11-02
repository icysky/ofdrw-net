namespace Ofd.Layout.Element;

/// <summary>Div 在行内的表现形式（Java Display）。</summary>
public enum Display
{
    /// <summary>块级元素：前后产生换行。</summary>
    Block,

    /// <summary>内联块级元素：不产生换行（除非前一个是块级元素）。</summary>
    InlineBlock,
}
