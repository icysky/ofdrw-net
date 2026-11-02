using Ofd.Archive.Check;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器：清理无效勾边属性。
/// <para>Stroke=false 的 TextObject/PathObject 删除勾边相关属性；CompositeObject 恒删除勾边/填充属性。</para>
/// </summary>
public class CleanStrokeAttrHandler : IArchiveHandler
{
    /// <summary>Stroke=false 时需要删除的属性。</summary>
    private static readonly string[] StrokeRelatedAttrs =
    {
        "LineWidth", "Cap", "Join", "MiterLimit", "DashPattern", "DashOffset", "StrokeColor",
    };

    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            int pageCount = reader.GetNumberOfPages();
            for (int i = 1; i <= pageCount; i++)
            {
                var page = reader.GetPage(i);
                CleanPageElements(page.GetProxy());
            }
        }
        catch (Exception e)
        {
            throw new IOException("清理勾边属性时异常: " + e.Message, e);
        }
    }

    /// <summary>清理页面图元属性。</summary>
    private static void CleanPageElements(DomElement page)
    {
        foreach (var textObj in DomWalker.Descendants(page, "TextObject"))
        {
            if (IsFalse(textObj.Attr("Stroke"))) RemoveAttributes(textObj, StrokeRelatedAttrs);
        }

        foreach (var pathObj in DomWalker.Descendants(page, "PathObject"))
        {
            if (IsFalse(pathObj.Attr("Stroke"))) RemoveAttributes(pathObj, StrokeRelatedAttrs);
        }

        foreach (var compObj in DomWalker.Descendants(page, "CompositeObject"))
        {
            RemoveAttributes(compObj, StrokeRelatedAttrs);
            compObj.SetAttr("Stroke", null);
            compObj.SetAttr("Fill", null);
            compObj.SetAttr("FillColor", null);
        }
    }

    private static bool IsFalse(string? value) =>
        string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);

    private static void RemoveAttributes(DomElement element, IEnumerable<string> names)
    {
        foreach (var name in names) element.SetAttr(name, null);
    }
}
