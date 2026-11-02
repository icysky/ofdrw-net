using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>处理器 4：删除权限声明（GB/T 42133-2022 6.2.2a）。</summary>
public class PermissionHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            reader.GetDoc(0)?.RemoveOFDElemByNames("Permissions");
        }
        catch (Exception e)
        {
            throw new IOException("无法读取文档: " + e.Message, e);
        }
    }
}
