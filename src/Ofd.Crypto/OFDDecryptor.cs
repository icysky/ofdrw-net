using Ofd.Core.BasicType;
using Ofd.Core.Crypto.Encryt;
using Ofd.Core.Dom;
using Ofd.Crypto.Decryptor;
using Ofd.Pkg;
using Ofd.Pkg.Container;
using Ofd.Reader;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace Ofd.Crypto;

/// <summary>
/// OFD 解密器（Java OFDDecryptor）：<see cref="OFDEncryptor"/> 的逆操作。
/// 解压 → 读 Encryptions.xml → 恢复 FEK（解密器匹配 UserInfo）→ 解密密文映射表 → 逐文件解密 → 清理元数据 → 重新打包。
/// </summary>
public class OFDDecryptor : IDisposable
{
    private readonly string _workDir;
    private readonly bool _ownWorkDir;
    private readonly OFDDir _ofdDir;
    private readonly string _dest;
    private readonly List<IUserFekDecryptor> _decryptors = new(3);
    private bool _closed;

    /// <summary>创建解密器（自动创建临时工作目录，Close 时删除）。</summary>
    public OFDDecryptor(string src, string dest)
        : this(src, dest, Path.Combine(Path.GetTempPath(), "ofd-dec-tmp-" + Guid.NewGuid().ToString("N")[..10]), true) { }

    /// <summary>创建解密器（使用调用方指定的工作目录，Close 时不删除）。</summary>
    public OFDDecryptor(string src, string dest, string workDir) : this(src, dest, workDir, false) { }

    private OFDDecryptor(string src, string dest, string workDir, bool ownWorkDir)
    {
        if (src == null || !File.Exists(src)) throw new ArgumentException("加密文件(src)不存在: " + src);
        if (dest == null) throw new ArgumentException("解密后文件输出位置(dest)为空");

        _dest = dest;
        _workDir = workDir;
        _ownWorkDir = ownWorkDir;

        if (!Directory.Exists(workDir)) Directory.CreateDirectory(workDir);

        ZipUtil.UnZipFiles(src, workDir + Path.DirectorySeparatorChar);
        _ofdDir = new OFDDir(workDir);
    }

    /// <summary>添加解密用户（建议先添加匹配概率最高的）。</summary>
    public OFDDecryptor AddUser(IUserFekDecryptor? decryptor)
    {
        if (decryptor != null) _decryptors.Add(decryptor);
        return this;
    }

    /// <summary>执行解密。</summary>
    public void Decrypt()
    {
        string encryptionsPath = Path.Combine(_workDir, "Encryptions.xml");
        if (!File.Exists(encryptionsPath))
        {
            // 未加密，直接打包
            _ofdDir.Jar(_dest);
            return;
        }

        Encryptions encryptions;
        try
        {
            encryptions = new Encryptions(ElemCup.Inject(encryptionsPath));
        }
        catch (Exception e)
        {
            throw new IOException("无法解析 Encryptions.xml: " + e.Message, e);
        }

        var encryptInfoList = encryptions.GetEncryptInfos();
        if (encryptInfoList.Count == 0)
        {
            _ofdDir.Jar(_dest);
            return;
        }

        foreach (var encryptInfo in encryptInfoList)
        {
            // a. 恢复 FEK/IV
            var decryptResult = RecoverFek(encryptInfo);
            // b. 解密密文映射表
            var entries = DecryptEntriesMap(encryptInfo, decryptResult);
            // c. 解密文件
            DecryptFiles(entries, decryptResult);
            // d. 删除密钥描述文件与密文映射表
            DeleteMetadataFile(encryptInfo.GetDecryptSeedLoc());
            DeleteMetadataFile(encryptInfo.GetEntriesMapLoc());
        }

        File.Delete(encryptionsPath);
        _ofdDir.Jar(_dest);
    }

    /// <summary>恢复 FEK/IV：按加密方案与用户名匹配 UserInfo。</summary>
    private DecryptResult RecoverFek(CT_EncryptInfo encryptInfo)
    {
        string seedPath = ResolveContainerPath(encryptInfo.GetDecryptSeedLoc());
        if (!File.Exists(seedPath))
            throw new IOException("密钥描述文件不存在: " + encryptInfo.GetDecryptSeedLoc());

        DecyptSeed decyptSeed;
        try
        {
            decyptSeed = new DecyptSeed(ElemCup.Inject(seedPath));
        }
        catch (Exception e)
        {
            throw new IOException("无法解析密钥描述文件: " + seedPath, e);
        }

        // 加密方案标识可能为 null（老版本未写入），为 null 时跳过方案检查
        string? encryptCaseId = decyptSeed.GetEncryptCaseId();
        var userInfoList = decyptSeed.GetUserInfos();

        if (userInfoList.Count == 0)
            throw new CryptoException("密钥描述文件中没有用户信息(UserInfo)");

        foreach (var decryptor in _decryptors)
        {
            if (encryptCaseId != null && encryptCaseId != decryptor.EncryptCaseId()) continue;

            foreach (var userInfo in userInfoList)
            {
                string? targetUsername = decryptor.GetUsername();
                if (targetUsername != null && targetUsername != userInfo.GetUserName()) continue;

                try
                {
                    return decryptor.Decrypt(userInfo);
                }
                catch (CryptoException)
                {
                    // 当前 UserInfo 解密失败，继续尝试
                }
            }
        }

        var sb = new System.Text.StringBuilder("没有匹配的解密器。");
        sb.Append(" 文件加密方案: ").Append(encryptCaseId);
        sb.Append(", 可用解密方案: ");
        foreach (var d in _decryptors) sb.Append(d.EncryptCaseId()).Append(' ');
        throw new CryptoException(sb.ToString());
    }

    /// <summary>解密明密文映射表（先按密文解密，失败则按明文 XML 解析）。</summary>
    private EncryptEntries DecryptEntriesMap(CT_EncryptInfo encryptInfo, DecryptResult decryptResult)
    {
        string entriesPath = ResolveContainerPath(encryptInfo.GetEntriesMapLoc());
        if (!File.Exists(entriesPath))
            throw new IOException("明密文映射表文件不存在: " + encryptInfo.GetEntriesMapLoc());

        byte[] entriesBytes = File.ReadAllBytes(entriesPath);
        byte[] plainBytes;
        try
        {
            plainBytes = Sm4CbcDecrypt(entriesBytes, decryptResult.GetFek(), decryptResult.GetIv());
        }
        catch (CryptoException)
        {
            // 可能本身就是明文
            plainBytes = entriesBytes;
        }

        try
        {
            using var ms = new MemoryStream(plainBytes);
            return new EncryptEntries(DomParser.Parse(ms));
        }
        catch (Exception e)
        {
            throw new IOException("无法解析明密文映射表: " + e.Message, e);
        }
    }

    /// <summary>解密所有加密文件（写回明文路径并删除密文）。</summary>
    private void DecryptFiles(EncryptEntries entries, DecryptResult decryptResult)
    {
        foreach (var entry in entries.GetEncryptEntries())
        {
            string cipherPath = ResolveContainerPath(entry.GetEPath());
            string plainPath = ResolveContainerPath(entry.GetPathA());

            if (!File.Exists(cipherPath)) continue;

            byte[] plainBytes = Sm4CbcDecrypt(File.ReadAllBytes(cipherPath),
                decryptResult.GetFek(), decryptResult.GetIv());

            string? parent = Path.GetDirectoryName(plainPath);
            if (parent != null) Directory.CreateDirectory(parent);
            File.WriteAllBytes(plainPath, plainBytes);
            File.Delete(cipherPath);
        }
    }

    /// <summary>SM4-CBC 解密（PKCS#7 填充）。</summary>
    private static byte[] Sm4CbcDecrypt(byte[] cipherBytes, byte[] fek, byte[] iv)
    {
        var blockCipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new SM4Engine()), new Pkcs7Padding());
        blockCipher.Init(false, new ParametersWithIV(new KeyParameter(fek), iv));

        var buffOut = new byte[cipherBytes.Length + blockCipher.GetBlockSize()];
        int n = blockCipher.ProcessBytes(cipherBytes, 0, cipherBytes.Length, buffOut, 0);
        try
        {
            n += blockCipher.DoFinal(buffOut, n);
        }
        catch (InvalidCipherTextException e)
        {
            throw new CryptoException("SM4-CBC 解密失败：密钥错误或密文数据损坏", e);
        }

        var plainBytes = new byte[n];
        Array.Copy(buffOut, 0, plainBytes, 0, n);
        return plainBytes;
    }

    /// <summary>容器内绝对路径 → 文件系统路径。</summary>
    private string ResolveContainerPath(ST_Loc? loc)
    {
        string path = loc?.ToString() ?? throw new ArgumentException("容器内路径为空");
        if (path.StartsWith('/')) path = path[1..];
        return Path.Combine(_workDir, path);
    }

    private void DeleteMetadataFile(ST_Loc? loc)
    {
        if (loc == null) return;
        string abs = ResolveContainerPath(loc);
        if (File.Exists(abs)) File.Delete(abs);
    }

    /// <summary>关闭解密器并清理临时目录（仅当工作目录由本类创建）。</summary>
    public void Close()
    {
        if (_closed) return;
        _closed = true;

        if (!_ownWorkDir || !Directory.Exists(_workDir)) return;
        try
        {
            Directory.Delete(_workDir, recursive: true);
        }
        catch (IOException e)
        {
            throw new IOException("无法删除解密器的工作目录: " + e.Message, e);
        }
    }

    public void Dispose() => Close();
}
