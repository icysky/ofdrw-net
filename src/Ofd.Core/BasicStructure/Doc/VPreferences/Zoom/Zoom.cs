using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.Doc.VPreferences;

/// <summary>文档的缩放率（7.5 表 9 视图首选项）。</summary>
public class Zoom : ZoomScale
{
    public Zoom(Dom.DomElement proxy) : base(proxy) { }

    public Zoom(double value) : base("Zoom")
    {
        SetValue(value);
    }

    /// <summary>设置 文档的缩放率（注：Java 用 addText 追加语义，重复设置会累积文本，保持一致）。</summary>
    public Zoom SetValue(double value)
    {
        Proxy.Kids.Add(new Dom.DomText(STBase.Fmt(value)));
        return this;
    }

    /// <summary>获取 文档的缩放率。</summary>
    public double GetValue() => double.Parse(ElementText(Proxy), System.Globalization.CultureInfo.InvariantCulture);
}
