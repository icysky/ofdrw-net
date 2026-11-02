using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>处理器 8：删除扩展信息（GB/T 42133-2022 6.2.2e）。</summary>
public class ExtensionHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            reader.GetDoc(0)?.RemoveOFDElemByNames("Extensions");
        }
        catch (Exception e)
        {
            throw new IOException("无法处理扩展信息: " + e.Message, e);
        }
    }
}
