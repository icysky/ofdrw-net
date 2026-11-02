using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicType;
using Ofd.Layout.Element;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Layout;

/// <summary>
/// 虚拟页面（Java VirtualPage）：盒式模型与板式模型的中间态，内部元素均为绝对定位。
/// </summary>
public class VirtualPage
{
    /// <summary>页面样式。</summary>
    private PageLayout? _style;

    /// <summary>页面内容。</summary>
    private List<Div> _content = new();

    /// <summary>插入页码（仅非空时表示插入到指定页位置）。</summary>
    private int? _pageNum;

    /// <summary>页面模板。</summary>
    private readonly List<Template> _templates = new();

    protected VirtualPage() { }

    public VirtualPage(PageLayout style) => _style = style;

    public VirtualPage(double? width, double? height) => _style = new PageLayout(width, height);

    public PageLayout? GetStyle() => _style;

    public void SetStyle(PageLayout style) => _style = style;

    /// <summary>不做分析直接加入（调用方需自行保证元素已完成布局）。</summary>
    public VirtualPage AddUnsafe(Div d)
    {
        _content.Add(d);
        return this;
    }

    /// <summary>向虚拟页面加入绝对定位元素。</summary>
    public VirtualPage Add(Div? d)
    {
        if (d == null) return this;

        if (d.GetFloat() != AFloat.Left)
            Console.Error.WriteLine("虚拟页面下不支持浮动属性，仅支持绝对定位");

        if (d.GetPosition() != Position.Absolute)
            throw new ArgumentException("加入虚拟页面的对象应该采用绝对定位（Position: Absolute）");

        if (d.GetX() == null || d.GetY() == null)
            throw new ArgumentException("处于绝对定位的模式下的元素应该设置 X 和 Y 坐标");

        if (d.GetWidth() == null)
            throw new ArgumentException("绝对定位元素至少需要指定元素宽度（Width）");

        // 追加前预处理
        d.DoPrepare(d.GetWidth()!.Value + d.WidthPlus());
        _content.Add(d);
        return this;
    }

    /// <summary>获取内容容器。</summary>
    public List<Div> GetContent() => _content;

    /// <summary>获取指定图层的元素。</summary>
    public List<Div> GetContent(LayerType layer) =>
        _content.Where(d => d.GetLayer() == layer).ToList();

    /// <summary>按图层分组：前景层、正文层、背景层。</summary>
    public List<List<Div>> GetLayerContent()
    {
        var res = new List<List<Div>>(3);
        if (_content.Count == 0) return res;

        var foreground = new List<Div>();
        var body = new List<Div>();
        var background = new List<Div>();

        res.Add(foreground);
        res.Add(body);
        res.Add(background);

        foreach (var div in _content)
        {
            switch (div.GetLayer())
            {
                case LayerType.Foreground:
                    foreground.Add(div);
                    break;
                case LayerType.Body:
                    body.Add(div);
                    break;
                case LayerType.Background:
                    background.Add(div);
                    break;
            }
        }

        return res;
    }

    /// <summary>替换内容容器。</summary>
    internal VirtualPage SetContent(List<Div> content)
    {
        _content = content;
        return this;
    }

    /// <summary>获取页码（从 1 起）。</summary>
    public int? GetPageNum() => _pageNum;

    /// <summary>设置页码（从 1 起，必须大于 0）。</summary>
    public VirtualPage SetPageNum(int pageNum)
    {
        if (pageNum <= 0) throw new ArgumentException("虚拟页面页码(pageNum)错误");
        _pageNum = pageNum;
        return this;
    }

    /// <summary>页面引用的模板序列。</summary>
    public List<Template> GetTemplates() => _templates;

    /// <summary>添加页面模板（order 为 null 时默认背景层）。</summary>
    public void AddTemplate(string id, LayerType? order)
    {
        var tpl = new Template();
        var stId = ST_ID.GetInstance(id) ?? throw new ArgumentException("模板ID格式错误: " + id);
        tpl.SetTemplateID(stId.Ref());
        if (order != null) tpl.SetZOrder(order);

        _templates.Add(tpl);
    }
}
