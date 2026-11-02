using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicType;
using Ofd.Reader.Model;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Reader;

/// <summary>页面信息（Java PageInfo）：页面物理大小、页面对象、模板页与图层汇总。</summary>
public class PageInfo
{
    /// <summary>页面的物理大小。</summary>
    private ST_Box? _size;

    /// <summary>页面底层对象。</summary>
    private PageObj? _obj;

    /// <summary>页面在 OFD 中的对象 ID。</summary>
    private ST_ID? _id;

    /// <summary>页码，从 1 起。</summary>
    private int _index;

    /// <summary>该页面引用的模板页面。</summary>
    private List<TemplatePageEntity> _templates = new();

    /// <summary>页面的绝对路径。</summary>
    private ST_Loc? _pageAbsLoc;

    /// <summary>页码目录文件的序号。</summary>
    private int _pageN;

    public ST_Box? GetSize() => _size;

    public PageInfo SetSize(ST_Box? size)
    {
        _size = size;
        return this;
    }

    public PageObj? GetObj() => _obj;

    public PageInfo SetObj(PageObj? obj)
    {
        _obj = obj;
        return this;
    }

    public ST_ID? GetId() => _id;

    public PageInfo SetId(ST_ID? id)
    {
        _id = id;
        return this;
    }

    public int GetIndex() => _index;

    public PageInfo SetIndex(int index)
    {
        _index = index;
        return this;
    }

    public ST_Loc? GetPageAbsLoc() => _pageAbsLoc;

    /// <summary>设置页面的绝对路径。</summary>
    public PageInfo SetPageAbsLoc(ST_Loc? pageAbsLoc)
    {
        _pageAbsLoc = pageAbsLoc;
        return this;
    }

    /// <summary>获取 Page_N 容器 N 的数字。</summary>
    public int GetPageN() => _pageN;

    /// <summary>设置 Page_N 容器 N 的数字。</summary>
    public PageInfo SetPageN(int pageN)
    {
        _pageN = pageN;
        return this;
    }

    public PageInfo SetTemplates(List<TemplatePageEntity> templates)
    {
        _templates = templates;
        return this;
    }

    /// <summary>获取该页面引用的模板页面（附加访问器，便于外部读取模板信息）。</summary>
    public List<TemplatePageEntity> GetTemplates() => _templates;

    /// <summary>获取按照 ZOrder 与出现顺序排列的页面与模板内容（页面本身按 Body 参与排序）。</summary>
    public List<PageObj> GetOrderRelatedPageList()
    {
        var res = new List<TemplatePageEntity>(_templates);
        if (_obj != null) res.Add(new TemplatePageEntity(LayerType.Body, _obj));
        return res.OrderBy(p => p.GetZOrder().Order()).Select(p => p.GetPage()).ToList();
    }

    /// <summary>获取整个页面的图层列表（包含模板页的图层）。</summary>
    public List<CT_Layer> GetAllLayer()
    {
        var layerList = new List<CT_Layer>();
        foreach (var page in GetOrderRelatedPageList())
        {
            var content = page.GetContent();
            if (content != null)
                layerList.AddRange(content.GetOrderedLayers());
        }
        return layerList;
    }
}
