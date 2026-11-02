using Ofd.Core.BasicStructure.Res;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器 10：删除图像自定义扩展数据（GB/T 42133-2022 6.2.6f）。
/// <para>判定方式：子元素前缀不是空或 <c>ofd</c> 时视为自定义扩展（C# DOM 不保留命名空间 URI）。</para>
/// </summary>
public class ImageExtensionHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        var resMgt = reader.GetResMgt();
        if (resMgt == null) return;

        var mediaList = resMgt.GetMultiMedias();
        if (mediaList == null || mediaList.Count == 0) return;

        foreach (var media in mediaList)
        {
            if (media.GetType() != MediaType.Image) continue;

            var children = media.GetProxy().Kids.OfType<DomElement>().ToList();
            foreach (var child in children)
            {
                string prefix = child.Prefix ?? "";
                if (prefix.Length == 0 || prefix == "ofd") continue;

                child.Detach();
            }
        }
    }
}
