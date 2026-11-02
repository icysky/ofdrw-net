namespace Ofd.Layout.Element;

/// <summary>
/// 页面剩余空间填充（Java PageAreaFiller）：不参与布局分析，仅作为命令标志使剩余空间为 0。
/// </summary>
public class PageAreaFiller : Div
{
    public PageAreaFiller()
    {
        SetPlaceholder(true);
        SetClear(Clear.Both);
        SetFloat(AFloat.Center);
    }
}
