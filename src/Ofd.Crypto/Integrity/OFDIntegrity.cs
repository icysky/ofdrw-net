using Ofd.Core.BasicType;
using OFDEntries = Ofd.Core.Integrity.OFDEntries;
using FileList = Ofd.Core.Integrity.FileList;
using Ofd.Pkg;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Crypto.Integrity;

/// <summary>
/// OFD 完整性保护协议实现（Java OFDIntegrity）：GM/T 0099 7.4.3。
/// 生成防夹带清单 OFDEntries.xml + 签名值 signedvalue.dat，并打包。
/// </summary>
public class OFDIntegrity : IDisposable
{
    private readonly OFDDir _ofdDir;
    private readonly string _dest;
    private readonly string _workDir;
    private bool _closed;

    /// <summary>ID 提供器（清单 ID 与文件 ID 共用一个递增计数）。</summary>
    private int _idProvider;

    public OFDIntegrity(string ofdFile, string dest)
    {
        if (ofdFile == null || !File.Exists(ofdFile)) throw new ArgumentException("待保护文件位置(ofdFile)不正确");
        if (dest == null) throw new ArgumentException("完整性保护文件输出位置(dest)为空");

        _dest = dest;
        _workDir = Path.Combine(Path.GetTempPath(), "ofd-tmp-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(_workDir);
        ZipUtil.UnZipFiles(ofdFile, _workDir + Path.DirectorySeparatorChar);
        _ofdDir = new OFDDir(_workDir);
    }

    /// <summary>对文件实行完整性保护（完成后请 Close 清理临时目录）。</summary>
    public void Protect(IProtectSigner signer)
    {
        if (signer == null) throw new ArgumentException("请提供 签名实现");

        var fileList = new FileList();
        var ofdEntries = new OFDEntries()
            .SetId((++_idProvider).ToString())
            .SetCreatorName("ofdrw-crypto")
            .SetVersion(GlobalVar.Version)
            .SetCreationDate(DateTime.Now);

        ofdEntries.SetSignedValueLoc(new ST_Loc("/signedvalue.dat"))
            .SetFileList(fileList);

        string ofdEntriesPath = Path.Combine(_workDir, "OFDEntries.xml");
        string signedValuePath = Path.Combine(_workDir, "signedvalue.dat");

        // a) 确认包内的所有文件
        string sysRoot = Path.GetFullPath(_workDir).Replace('\\', '/');
        foreach (var file in Directory.EnumerateFiles(_workDir, "*", SearchOption.AllDirectories))
        {
            string abs = Path.GetFullPath(file).Replace('\\', '/').Replace(sysRoot, "");
            // b) 组装完整性保护清单
            fileList.AddFile((++_idProvider).ToString(), abs);
        }

        ElemCup.Dump(ofdEntries.GetProxy(), ofdEntriesPath);

        // c) 计算清单杂凑值 d) 私钥签名
        byte[] signature = signer.DigestThenSign(ofdEntriesPath);

        // e) 写入签名值文件
        File.WriteAllBytes(signedValuePath, signature);

        _ofdDir.Jar(_dest);
    }

    /// <summary>清理临时目录。</summary>
    public void Close()
    {
        if (_closed) return;
        _closed = true;
        if (!Directory.Exists(_workDir)) return;

        try
        {
            Directory.Delete(_workDir, recursive: true);
        }
        catch (IOException e)
        {
            throw new IOException("无法删除完整性保护的工作空间，原因：" + e.Message, e);
        }
    }

    public void Dispose() => Close();
}
