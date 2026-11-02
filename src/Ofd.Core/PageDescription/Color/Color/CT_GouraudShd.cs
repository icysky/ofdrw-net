using System.Globalization;

namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>
/// 高洛德渐变（8.3.4.4 图 41 表 31）：指定三个带可选颜色的顶点，
/// 在构成的三角形区域内采用高洛德算法绘制渐变。
/// </summary>
public class CT_GouraudShd : OFDElement, IColorClusterType
{
    public CT_GouraudShd(Dom.DomElement proxy) : base(proxy) { }

    public CT_GouraudShd() : base("GouraudShd") { }

    protected CT_GouraudShd(string name) : base(name) { }

    /// <summary>【可选属性】设置 控制点所确定的部分是否填充（"1"/"0"；null 删除属性，缺省 0 不填充）。</summary>
    public CT_GouraudShd SetExtend(bool? extend)
    {
        if (extend == null)
        {
            RemoveAttr("Extend");
            return this;
        }
        return (CT_GouraudShd)AddAttribute("Extend", extend.Value ? "1" : "0");
    }

    /// <summary>【可选属性】获取 控制点所确定的部分是否填充，缺省 false。</summary>
    public bool GetExtend()
    {
        var str = AttributeValue("Extend");
        if (str == null || str.Trim().Length == 0) return false;
        return int.Parse(str.Trim(), CultureInfo.InvariantCulture) == 1;
    }

    /// <summary>【必选】增加 渐变控制点（至少三个；null 忽略）。</summary>
    public CT_GouraudShd AddPoint(Point? point)
    {
        if (point == null) return this;
        Add(point);
        return this;
    }

    /// <summary>【必选】获取 渐变控制点列表。</summary>
    public List<Point> GetPoints() => GetOFDElements("Point", e => new Point(e));

    /// <summary>
    /// 【可选】设置 渐变范围外的填充颜色（应为基本颜色；null 删除元素）。
    /// 注：Java 上游 null 分支误调 removeAttr("BackColor")（BackColor 是子元素而非属性），
    /// C# 已修正为删除元素。
    /// </summary>
    public CT_GouraudShd SetBackColor(CT_Color? backColor)
    {
        if (backColor == null)
        {
            RemoveOFDElemByNames("BackColor");
            return this;
        }
        backColor.SetOFDName("BackColor");
        Set(backColor);
        return this;
    }

    /// <summary>【可选】获取 渐变范围外的填充颜色，不存在 null。</summary>
    public CT_Color? GetBackColor()
    {
        var e = GetOFDElement("BackColor");
        return e == null ? null : new CT_Color(e);
    }
}
