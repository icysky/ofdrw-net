using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>处理器 1：设置 DocType = "OFD-A"（GB/T 42133-2022 6.2.1a）。</summary>
public class DocTypeHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            var ofd = ofdDir.GetOfd();
            ofd.SetDocType("OFD-A");
            ofdDir.SetOfd(ofd); // 写回容器缓存
        }
        catch (Exception e)
        {
            throw new IOException("无法读取 OFD.xml: " + e.Message, e);
        }
    }
}
