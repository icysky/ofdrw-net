using Ofd.Core.Text.Font;

namespace Ofd.Layout.Engine;

/// <summary>已存在的字体对象（Java ExistCTFont）。</summary>
public class ExistCTFont
{
    /// <summary>在文档中的字体对象。</summary>
    public CT_Font Font;

    /// <summary>字体文件绝对路径（系统字体可能为 null）。</summary>
    public string? AbsPath;

    public ExistCTFont(CT_Font font, string? absPath)
    {
        Font = font;
        AbsPath = absPath;
    }
}
