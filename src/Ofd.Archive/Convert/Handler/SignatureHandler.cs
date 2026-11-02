using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器：数字签名/签章去技术化（GB/T 42133-2022 6.13）。
/// <para>删除 Sign_N 目录与 Signatures.xml，并从 OFD.xml 的 DocBody 中移除签名引用。</para>
/// </summary>
public class SignatureHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        string workDir = reader.GetWorkDir();

        var signatures = reader.GetDefaultSignatures();
        if (signatures == null) return;

        var sigList = signatures.GetSignatures();
        if (sigList.Count == 0) return;

        // 签名目录（Signatures.xml 所在目录）
        var signLoc = reader.GetDefaultDocSignaturesPath();
        string? signsDir = null;
        if (signLoc != null)
        {
            string? parent = signLoc.Parent();
            if (parent != null)
            {
                signsDir = Path.Combine(workDir, parent.StartsWith('/') ? parent[1..] : parent);
            }
        }

        // 删除各签名描述目录 Sign_N/
        foreach (var sig in sigList)
        {
            var baseLoc = sig.GetBaseLoc();
            if (baseLoc == null || signsDir == null) continue;

            string locStr = baseLoc.ToString();
            string signNDir = locStr;
            int slashIdx = locStr.IndexOf('/');
            if (slashIdx > 0) signNDir = locStr[..slashIdx];

            TryDeleteDirectory(Path.Combine(signsDir, signNDir));
        }

        // 清除签名记录
        foreach (var sig in sigList.ToList()) sig.GetProxy().Detach();

        // 删除 Signatures.xml 并从 DocBody 移除引用
        if (signLoc != null)
        {
            string sigPath = signLoc.ToString();
            if (sigPath.StartsWith('/')) sigPath = sigPath[1..];

            try
            {
                string sigFile = Path.Combine(workDir, sigPath);
                if (File.Exists(sigFile)) File.Delete(sigFile);
            }
            catch (IOException)
            {
                // 忽略
            }

            try
            {
                var ofd = ofdDir.GetOfd();
                foreach (var body in ofd.GetDocBodies()) body.RemoveOFDElemByNames("Signatures");
                ofdDir.SetOfd(ofd);
            }
            catch (Exception e)
            {
                throw new IOException("无法更新 OFD.xml: " + e.Message, e);
            }
        }

        // Signs 目录若为空则删除
        if (signsDir != null && Directory.Exists(signsDir))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(signsDir).Any()) TryDeleteDirectory(signsDir);
            }
            catch (IOException)
            {
                // 忽略
            }
        }
    }

    private static void TryDeleteDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch (IOException)
        {
            // 忽略
        }
    }
}
