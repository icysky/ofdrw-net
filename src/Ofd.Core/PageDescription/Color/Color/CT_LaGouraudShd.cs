using System.Globalization;

namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>
/// 网格高洛德渐变（8.3.4.5 图 43 表 32）：允许 4 个以上控制点，
/// 按每行固定网格数形成行列，相邻 4 点定义一个网格单元。
/// </summary>
public class CT_LaGouraudShd : CT_GouraudShd
{
    public CT_LaGouraudShd(Dom.DomElement proxy) : base(proxy) { }

    public CT_LaGouraudShd() : base("LaGouraudShd") { }

    /// <summary>【必选属性】设置 渐变区域内每行的网格数。</summary>
    public CT_LaGouraudShd SetVerticesPerRow(int verticesPerRow) =>
        (CT_LaGouraudShd)AddAttribute("VerticesPerRow", verticesPerRow.ToString(CultureInfo.InvariantCulture));

    /// <summary>【必选属性】获取 渐变区域内每行的网格数，缺失抛异常（与 Java 一致）。</summary>
    public int GetVerticesPerRow()
    {
        var str = AttributeValue("VerticesPerRow");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("渐变区域内每行的网格数（VerticesPerRow）不能为空");
        return int.Parse(str.Trim(), CultureInfo.InvariantCulture);
    }

    /// <summary>【必选】增加 渐变控制点（至少四个）。</summary>
    public new CT_LaGouraudShd AddPoint(Point? point)
    {
        base.AddPoint(point);
        return this;
    }
}
