using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>处理器 5：删除视图首选项（GB/T 42133-2022 6.2.2b）。</summary>
public class VPrefsHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            reader.GetDoc(0)?.RemoveOFDElemByNames("VPreferences");
        }
        catch (Exception e)
        {
            throw new IOException("无法读取文档: " + e.Message, e);
        }
    }
}
