using Ofd.Core.BasicType;

namespace Ofd.Core.PageDescription.Color.ColorSpace;

/// <summary>调色板（8.3 表 25），颜色索引编号从 0 开始。</summary>
public class Palette : OFDElement
{
    public Palette(Dom.DomElement proxy) : base(proxy) { }

    public Palette() : base("Palette") { }

    /// <summary>【必选】增加 调色板预定义颜色。</summary>
    public Palette AddCV(CV cv)
    {
        Add(cv);
        return this;
    }

    /// <summary>获取 预定义颜色（index 从 0 起；null/负值抛异常，越界抛异常，与 Java 一致）。</summary>
    public CV GetCVByIndex(int? index)
    {
        if (index == null || index < 0)
            throw new ArgumentException("Index 必须大于等于0");
        var elements = Elements();
        if (index >= elements.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "预定义位置颜色不存在 Index：" + index);
        return new CV(elements[index.Value]);
    }

    /// <summary>获取 预定义颜色值。</summary>
    public ST_Array? GetColorByIndex(int? index) => GetCVByIndex(index).GetColor();

    /// <summary>【必选】获取 调色板预定义颜色列表（只读语义）。</summary>
    public List<CV> GetCVs() => GetOFDElements("CV", e => new CV(e));
}
