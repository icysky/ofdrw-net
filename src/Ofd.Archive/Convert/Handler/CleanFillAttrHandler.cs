using Ofd.Archive.Check;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器：删除 ImageObject 上无意义的 Fill/FillColor 属性。
/// </summary>
public class CleanFillAttrHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            int pageCount = reader.GetNumberOfPages();
            for (int i = 1; i <= pageCount; i++)
            {
                var page = reader.GetPage(i);

                foreach (var imgObj in DomWalker.Descendants(page.GetProxy(), "ImageObject"))
                {
                    imgObj.SetAttr("Fill", null);
                    imgObj.SetAttr("FillColor", null);
                }
            }
        }
        catch (Exception e)
        {
            throw new IOException("清理填充属性时异常: " + e.Message, e);
        }
    }
}
