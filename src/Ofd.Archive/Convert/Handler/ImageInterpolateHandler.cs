using Ofd.Archive.Check;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>处理器：把所有图像对象的 Interpolate 置为 false（GB/T 42133-2022 6.5）。</summary>
public class ImageInterpolateHandler : IArchiveHandler
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
                    imgObj.SetAttr("Interpolate", "false");
            }
        }
        catch (Exception e)
        {
            throw new IOException("设置插值属性时异常: " + e.Message, e);
        }
    }
}
