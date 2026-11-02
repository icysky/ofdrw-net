using OFDEntries = Ofd.Core.Integrity.OFDEntries;
using Ofd.Pkg;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Crypto.Integrity;

/// <summary>
/// OFD 完整性协议校验（Java OFDIntegrityVerifier）：GM/T 0099 7.4.6。
/// 校验签名值 + 逐文件比对防夹带清单。
/// </summary>
public class OFDIntegrityVerifier
{
    /// <summary>
    /// 执行完整性校验。
    /// </summary>
    /// <returns>true - 文件完整且没有夹带；false - 校验失败或存在夹带。</returns>
    public bool Integrity(string input, IProtectVerifier verifier)
    {
        if (input == null || !File.Exists(input)) throw new ArgumentException("待校验的OFD文件路径(input)为空或不存在");
        if (verifier == null) throw new ArgumentException("签名值验证器(verifier)为空");

        string workDir = Path.Combine(Path.GetTempPath(), "ofd-tmp-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(workDir);

        try
        {
            ZipUtil.UnZipFiles(input, workDir + Path.DirectorySeparatorChar);
            var ofdDir = new OFDDir(workDir);

            // a) 读取完整性保护描述文件（不存在会抛 FileNotFoundException）
            string ofdEntriesPath = ofdDir.GetFile(OFDDir.OFDEntriesFileName);
            var ofdEntries = new OFDEntries(ElemCup.Inject(ofdEntriesPath));

            // b/c) 计算清单杂凑值并验证签名值
            var signedValueLoc = ofdEntries.GetSignedValueLoc();
            string sigValuePath = Path.Combine(workDir, signedValueLoc.Loc.TrimStart('/'));
            byte[] signedValue = File.ReadAllBytes(sigValuePath);

            bool integrity = verifier.DigestThenVerify(ofdEntriesPath, signedValue);

            // 逐文件比对，确保没有额外夹带
            bool noExtraFile = CheckNoExtraFile(ofdDir, ofdEntries);
            return integrity && noExtraFile;
        }
        finally
        {
            try
            {
                if (Directory.Exists(workDir)) Directory.Delete(workDir, recursive: true);
            }
            catch
            {
                // 清理失败忽略
            }
        }
    }

    /// <summary>检查是否存在清单之外的夹带文件。</summary>
    private static bool CheckNoExtraFile(OFDDir ofdDir, OFDEntries ofdEntries)
    {
        var pkgFileSet = new HashSet<string>();

        foreach (var f in ofdEntries.GetFileList().GetFiles())
        {
            string loc = f.GetFileLoc().ToString();
            if (!loc.StartsWith('/')) loc = "/" + loc;
            pkgFileSet.Add(loc);
        }

        // 签名值文件也纳入
        string signedLoc = ofdEntries.GetSignedValueLoc().ToString();
        if (!signedLoc.StartsWith('/')) signedLoc = "/" + signedLoc;
        pkgFileSet.Add(signedLoc);

        bool noExtra = true;
        ofdDir.Walk(new DelegateIterator((pkgAbsPath, path) =>
        {
            // 忽略防夹带文件本身
            if (pkgAbsPath == "/OFDEntries.xml") return true;

            if (pkgFileSet.Contains(pkgAbsPath)) return true;

            // 清单外的文件 → 存在夹带，停止遍历
            noExtra = false;
            return false;
        }));

        return noExtra;
    }

    private sealed class DelegateIterator : IOFDPackageFileIterator
    {
        private readonly Func<string, string, bool> _f;

        public DelegateIterator(Func<string, string, bool> f) => _f = f;

        public bool Visit(string pkgAbsPath, string path) => _f(pkgAbsPath, path);
    }
}
