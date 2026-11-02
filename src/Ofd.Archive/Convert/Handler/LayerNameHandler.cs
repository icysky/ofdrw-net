using Ofd.Archive.Check;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器：确保同一页面内图层名称唯一（重名追加 _2、_3…）。
/// </summary>
public class LayerNameHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            int pageCount = reader.GetNumberOfPages();
            for (int i = 1; i <= pageCount; i++)
            {
                var page = reader.GetPage(i);
                EnsureUniqueLayerNames(page.GetProxy());
            }
        }
        catch (Exception e)
        {
            throw new IOException("处理图层名称时异常: " + e.Message, e);
        }
    }

    /// <summary>确保页面内 Layer 的 Name 唯一。</summary>
    private static void EnsureUniqueLayerNames(Ofd.Core.Dom.DomElement page)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var layer in DomWalker.Descendants(page, "Layer"))
        {
            string? name = layer.Attr("Name");
            if (name == null) continue;

            if (!seen.Contains(name))
            {
                seen.Add(name);
                continue;
            }

            int counter = 2;
            string newName;
            do
            {
                newName = name + "_" + counter;
                counter++;
            } while (seen.Contains(newName));

            layer.SetAttr("Name", newName);
            seen.Add(newName);
        }
    }
}
