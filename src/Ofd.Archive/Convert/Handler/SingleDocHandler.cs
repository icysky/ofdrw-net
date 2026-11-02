using Ofd.Archive.Check;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器 2：只保留第一个 DocBody（GB/T 42133-2022 6.2.1c）。
/// <para>移除多余的 DocBody 节点并删除其 Doc_N 目录（不做文档合并）。</para>
/// </summary>
public class SingleDocHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        Ofd.Core.BasicStructure.Ofd.OFD ofd;
        try
        {
            ofd = ofdDir.GetOfd();
        }
        catch (Exception e)
        {
            throw new IOException("无法读取 OFD.xml: " + e.Message, e);
        }

        var docBodies = ofd.GetDocBodies();
        if (docBodies.Count <= 1) return; // 已是单文档

        string workDir = reader.GetWorkDir();

        // 删除 DocBody[1..N] 对应的 Doc_N 目录
        for (int i = 1; i < docBodies.Count; i++)
        {
            string? docRoot = docBodies[i].GetDocRoot()?.ToString();
            if (string.IsNullOrEmpty(docRoot)) continue;

            string docDirName = docRoot;
            int slashIdx = docRoot.IndexOf('/');
            if (slashIdx > 0) docDirName = docRoot[..slashIdx];

            string docDir = Path.Combine(workDir, docDirName);
            try
            {
                if (Directory.Exists(docDir)) Directory.Delete(docDir, true);
            }
            catch (IOException)
            {
                // 删除失败不阻断（后续白名单复制会排除）
            }
        }

        // 移除多余 DocBody 元素（保留第一个）
        var bodyElements = DomWalker.Descendants(ofd.GetProxy(), "DocBody");
        for (int i = bodyElements.Count - 1; i >= 1; i--) bodyElements[i].Detach();

        ofdDir.SetOfd(ofd); // 写回
    }
}
