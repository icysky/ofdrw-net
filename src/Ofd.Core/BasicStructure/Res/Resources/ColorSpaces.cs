using Ofd.Core.PageDescription.Color.ColorSpace;

namespace Ofd.Core.BasicStructure.Res.Resources;

/// <summary>包含了一组颜色空间的描述（7.9 图 20 表 18）。</summary>
public class ColorSpaces : OFDElement, IOFDResource
{
    public ColorSpaces(Dom.DomElement proxy) : base(proxy) { }

    public ColorSpaces() : base("ColorSpaces") { }

    /// <summary>【必选】增加 颜色空间描述（必须有 ID；null 忽略）。</summary>
    public ColorSpaces AddColorSpace(CT_ColorSpace? colorSpace)
    {
        if (colorSpace == null) return this;
        if (colorSpace.GetID() == null)
            throw new ArgumentException("颜色空间ID不能为空");
        Add(colorSpace);
        return this;
    }

    /// <summary>【必选】获取 颜色空间描述列表。</summary>
    public List<CT_ColorSpace> GetColorSpaces() => GetOFDElements("ColorSpace", e => new CT_ColorSpace(e));
}
