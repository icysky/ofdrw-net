using Ofd.Core.BasicType;

namespace Ofd.Reader;

/// <summary>关键字在文档中的位置（页码 + 外接矩形 + 所属关键字）。</summary>
public class KeywordPosition
{
    /// <summary>关键字所在页码（从 1 起）。</summary>
    private int _page;

    /// <summary>矩形区域。</summary>
    private ST_Box _box;

    /// <summary>所属关键字。</summary>
    private string? _keyword;

    public KeywordPosition(int page, ST_Box box)
    {
        _page = page;
        _box = box;
    }

    public int GetPage() => _page;

    public void SetPage(int page) => _page = page;

    public ST_Box GetBox() => _box;

    public void SetBox(ST_Box box) => _box = box;

    public string? GetKeyword() => _keyword;

    public void SetKeyword(string? keyword) => _keyword = keyword;

    public override string ToString() =>
        $"KeywordPosition{{page={_page}, box={_box}, keyword='{_keyword}'}}";
}
