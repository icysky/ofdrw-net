namespace Ofd.Layout.Element;

/// <summary>
/// 区域占位区块（Java AreaHolderBlock）：属性与 Div 一致，仅占位并登记到 AreaHolderBlocks.xml。
/// <para>可绘制区域为去除 Border 与 Padding 后的内部区域。</para>
/// </summary>
public class AreaHolderBlock : Div
{
    /// <summary>占位区域名称（文档范围内唯一）。</summary>
    private string? _areaName;

    /// <summary>创建区域占位区块（宽高必须指定）。</summary>
    public AreaHolderBlock(string areaName, double? width, double? height) : base(width, height)
    {
        _areaName = areaName;
    }

    /// <summary>在指定位置创建区域占位区块。</summary>
    public AreaHolderBlock(string areaName, double x, double y, double w, double h) : base(x, y, w, h)
    {
        _areaName = areaName;
    }

    public string? GetAreaName() => _areaName;

    public AreaHolderBlock SetAreaName(string? areaName)
    {
        _areaName = areaName;
        return this;
    }

    /// <summary>不接受宽度重设，直接返回盒式模型尺寸。</summary>
    public override Rectangle DoPrepare(double? widthLimit)
    {
        double w = (GetWidth() ?? 0) + WidthPlus();
        double h = (GetHeight() ?? 0) + HeightPlus();
        return new Rectangle(w, h);
    }

    public override string ElementType() => "AreaHolderBlock";
}
