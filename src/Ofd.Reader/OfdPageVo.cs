using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;

namespace Ofd.Reader;

/// <summary>页面内容与模板页面（已过时，改用 <see cref="PageInfo"/>）。</summary>
[Obsolete("请使用 PageInfo")]
public class OfdPageVo
{
    private PageObjType? _contentPage;
    private PageObjType? _templatePage;

    public OfdPageVo(PageObjType? contentPage, PageObjType? templatePage)
    {
        _contentPage = contentPage;
        _templatePage = templatePage;
    }

    public PageObjType? GetContentPage() => _contentPage;

    public void SetContentPage(PageObjType? contentPage) => _contentPage = contentPage;

    public PageObjType? GetTemplatePage() => _templatePage;

    public void SetTemplatePage(PageObjType? templatePage) => _templatePage = templatePage;
}
