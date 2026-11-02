using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.Doc;

/// <summary>页面区域结构（图 7）：物理区域/显示区域/版心区域/出血区域。</summary>
public class CT_PageArea : OFDElement
{
    public CT_PageArea(Dom.DomElement proxy) : base(proxy) { }

    public CT_PageArea() : base("PageArea") { }

    /// <summary>以页面物理区域创建。</summary>
    public CT_PageArea(double topLeftX, double topLeftY, double width, double height) : this()
    {
        SetPhysicalBox(topLeftX, topLeftY, width, height);
    }

    /// <summary>【必选】设置 页面物理区域（左上角为页面坐标系原点）。</summary>
    public CT_PageArea SetPhysicalBox(ST_Box physicalBox) => (CT_PageArea)SetOFDEntity("PhysicalBox", physicalBox);

    public CT_PageArea SetPhysicalBox(double topLeftX, double topLeftY, double width, double height) =>
        SetPhysicalBox(new ST_Box(topLeftX, topLeftY, width, height));

    /// <summary>【必选】获取 页面物理区域，不存在 null。</summary>
    public ST_Box? GetPhysicalBox() => ST_Box.GetInstance(GetOFDElementText("PhysicalBox"));

    /// <summary>【可选】设置 显示区域（实际显示或打印输出区域）。</summary>
    public CT_PageArea SetApplicationBox(ST_Box applicationBox) => (CT_PageArea)SetOFDEntity("ApplicationBox", applicationBox);

    public CT_PageArea SetApplicationBox(double topLeftX, double topLeftY, double width, double height) =>
        SetApplicationBox(new ST_Box(topLeftX, topLeftY, width, height));

    /// <summary>【可选】获取 显示区域，不存在 null。</summary>
    public ST_Box? GetApplicationBox() => ST_Box.GetInstance(GetOFDElementText("ApplicationBox"));

    /// <summary>【可选】设置 版心区域（正文区域，位于显示区域内）。</summary>
    public CT_PageArea SetContentBox(ST_Box contentBox) => (CT_PageArea)SetOFDEntity("ContentBox", contentBox);

    public CT_PageArea SetContentBox(double topLeftX, double topLeftY, double width, double height) =>
        SetContentBox(new ST_Box(topLeftX, topLeftY, width, height));

    /// <summary>【可选】获取 版心区域，不存在 null。</summary>
    public ST_Box? GetContentBox() => ST_Box.GetInstance(GetOFDElementText("ContentBox"));

    /// <summary>【可选】设置 出血区域（位于物理区域外，缺省同物理区域）。</summary>
    public CT_PageArea SetBleedBox(ST_Box bleedBox) => (CT_PageArea)SetOFDEntity("BleedBox", bleedBox);

    public CT_PageArea SetBleedBox(double topLeftX, double topLeftY, double width, double height) =>
        SetBleedBox(new ST_Box(topLeftX, topLeftY, width, height));

    /// <summary>【可选】获取 出血区域，不存在 null。</summary>
    public ST_Box? GetBleedBox() => ST_Box.GetInstance(GetOFDElementText("BleedBox"));

    /// <summary>尝试获取页面最大区域（PhysicalBox → ApplicationBox → ContentBox）。</summary>
    public ST_Box? GetBox() => GetPhysicalBox() ?? GetApplicationBox() ?? GetContentBox();
}
