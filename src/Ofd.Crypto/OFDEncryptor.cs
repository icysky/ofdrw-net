using Ofd.Core.Crypto;
using Ofd.Core.Crypto.Encryt;
using Ofd.Core.Signatures.Sig;
using Ofd.Crypto.Encryptor;
using Ofd.Pkg;
using Ofd.Pkg.Container;
using Ofd.Reader;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Ofd.Crypto;

/// <summary>
/// OFD 加密器（Java OFDEncryptor）：GM/T 0099 附录 A 加密流程——
/// 生成 FEK/IV → 加密包内文件 → 明密文映射表 → 密钥描述文件 → 打包。
/// </summary>
public class OFDEncryptor : IDisposable
{
    /// <summary>OFD 虚拟容器根目录。</summary>
    private readonly OFDDir _ofdDir;

    /// <summary>加密后文件输出位置。</summary>
    private readonly string _dest;

    /// <summary>工作目录（解压后的 OFD 内容）。</summary>
    private readonly string _workDir;

    private bool _closed;

    /// <summary>随机源（默认软件随机源）。</summary>
    private SecureRandom _random = new();

    /// <summary>SM4-CBC 分块加密（PKCS#7 填充）。</summary>
    private readonly PaddedBufferedBlockCipher _blockCipher =
        new(new CbcBlockCipher(new SM4Engine()), new Pkcs7Padding());

    /// <summary>用户提供的加密器列表。</summary>
    private readonly List<IUserFekEncryptor> _userEncryptorList = new(3);

    /// <summary>容器文件过滤器（返回 false 的文件不加密）。</summary>
    private IContainerFileFilter? _cfFilter;

    /// <summary>加密操作的附加描述集合。</summary>
    private Parameters? _parameters;

    /// <summary>创建 OFD 加密器（解压到临时工作目录）。</summary>
    public OFDEncryptor(string ofdFile, string dest)
    {
        if (ofdFile == null || !File.Exists(ofdFile)) throw new ArgumentException("文件位置(ofdFile)不正确");
        if (dest == null) throw new ArgumentException("加密后文件位置(dest)为空");

        _dest = dest;
        _workDir = Path.Combine(Path.GetTempPath(), "ofd-tmp-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(_workDir);
        ZipUtil.UnZipFiles(ofdFile, _workDir + Path.DirectorySeparatorChar);
        _ofdDir = new OFDDir(_workDir);
    }

    /// <summary>添加加密用户。</summary>
    public OFDEncryptor AddUser(IUserFekEncryptor? encryptor)
    {
        if (encryptor == null) return this;
        _userEncryptorList.Add(encryptor);
        return this;
    }

    /// <summary>设置随机源。</summary>
    public OFDEncryptor SetRandom(SecureRandom? random)
    {
        if (random == null) return this;
        _random = random;
        return this;
    }

    /// <summary>设置容器文件过滤器。</summary>
    public OFDEncryptor SetContainerFileFilter(IContainerFileFilter? filter)
    {
        if (filter == null) return this;
        _cfFilter = filter;
        return this;
    }

    /// <summary>获取加密操作附加描述集合。</summary>
    public Parameters? GetParameters() => _parameters;

    /// <summary>设置加密操作附加描述集合。</summary>
    public OFDEncryptor SetParameters(Parameters? parameters)
    {
        _parameters = parameters;
        return this;
    }

    /// <summary>执行加密。</summary>
    public OFDEncryptor Encrypt()
    {
        if (_userEncryptorList.Count == 0)
            throw new ArgumentException("没有可用加密用户(UserFEKEncryptor)");

        string? destDir = Path.GetDirectoryName(_dest);
        if (destDir != null && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

        // 解密入口文件对象
        var encryptions = _ofdDir.ObtainEncryptions();
        string id = (encryptions.MaxId() + 1).ToString();

        // 待加密文件列表
        var tbEncArr = GetToBeEncFiles();

        int blockSize = _blockCipher.GetBlockSize();
        var fek = new byte[blockSize];
        var iv = new byte[blockSize];
        // a) 生成用于包内文件加密的对称密钥
        _random.NextBytes(fek);
        _random.NextBytes(iv);
        var keyParam = new ParametersWithIV(new KeyParameter(fek), iv);

        // b/c) 加密包内文件并生成明密文映射表
        var encryptEntries = EncryptFiles(tbEncArr, keyParam);
        encryptEntries.SetId(id);

        // d) 组装明密文映射表并加密写入
        var entriesMapCp = EncryptElement(encryptEntries, "entriesmap.dat", keyParam);

        // e) 组装加密入口文件（明文）
        var encryptInfo = NewEncryptInfo(id);
        encryptions.AddEncryptInfo(encryptInfo);
        encryptInfo.SetEncryptScope("All");

        var decryptseedCp = ContainerPath.NewDatFile("decryptseed", _workDir);
        encryptInfo.SetDecryptSeedLoc(decryptseedCp.GetPath());
        encryptInfo.SetEntriesMapLoc(entriesMapCp.GetPath());

        // 创建密钥描述文件
        var decyptSeedObj = new DecyptSeed()
            .SetId(id)
            .SetExtendParams(new ExtendParams());

        // f) 对 FEK 进行包装/非对称加密
        foreach (var fekEncryptor in _userEncryptorList)
        {
            var userInfo = fekEncryptor.Encrypt(fek, iv);

            // 证书加密需要附带证书
            if (ProtectionCaseID.EncryptGMCert.ToIdValue() == fekEncryptor.EncryptCaseId())
            {
                var certBin = fekEncryptor.UserCert();
                if (certBin == null || certBin.Length == 0)
                    throw new CryptoException("无法获取加密证书");
                userInfo.SetUserCert(certBin);
            }
            decyptSeedObj.AddUserInfo(userInfo);
        }

        // 密钥中记录加密方案（便于解密时匹配；与 Java 保持兼容的多用户同方案前提）
        var caseId = _userEncryptorList[0].EncryptCaseId();
        decyptSeedObj.SetEncryptCaseId(caseId);

        // h) 组装密钥描述文件并写入
        ElemCup.Dump(decyptSeedObj.GetProxy(), decryptseedCp.GetAbs());

        // 打包
        _ofdDir.Jar(_dest);
        return this;
    }

    private CT_EncryptInfo NewEncryptInfo(string id)
    {
        var encryptInfo = new CT_EncryptInfo().SetId(id);
        if (_parameters != null) encryptInfo.SetParameters(_parameters);

        var provider = new Ofd.Core.Crypto.Encryt.Provider();
        provider.SetCompany("ofdrw.org");
        provider.SetProviderName("ofdrw-crypto");
        provider.SetVersion(GlobalVar.Version);
        encryptInfo.SetProvider(provider);
        encryptInfo.SetEncryptDate(DateTime.Now);
        return encryptInfo;
    }

    /// <summary>获取待加密文件列表（应用过滤器）。</summary>
    private List<ContainerPath> GetToBeEncFiles()
    {
        string root = Path.GetFullPath(_workDir);
        var result = new List<ContainerPath>();

        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            string abs = Path.GetFullPath(path);
            string cfPath = abs.Replace(root, "").Replace('\\', '/');
            var cp = new ContainerPath(cfPath, abs);
            if (_cfFilter == null || _cfFilter.Filter(cp.GetPath(), cp.GetAbs()))
                result.Add(cp);
        }
        return result;
    }

    /// <summary>加密文件列表并返回明密文映射表。</summary>
    private EncryptEntries EncryptFiles(List<ContainerPath> tbEncArr, ICipherParameters keyParam)
    {
        var entriesMap = new EncryptEntries();
        foreach (var plaintextCp in tbEncArr)
        {
            var encryptedCp = EncryptFile(plaintextCp, keyParam);
            entriesMap.AddEncryptEntry(plaintextCp.GetPath(), encryptedCp.GetPath());
        }
        return entriesMap;
    }

    /// <summary>加密单个文件（加密后删除原文件）。</summary>
    private ContainerPath EncryptFile(ContainerPath plaintextCp, ICipherParameters keyParam)
    {
        var buffIn = new byte[4096];
        var buffOut = new byte[4096 + _blockCipher.GetBlockSize()];

        var encryptedCp = plaintextCp.CreateEncryptedFile();
        using (var input = File.OpenRead(plaintextCp.GetAbs()))
        using (var output = File.Create(encryptedCp.GetAbs()))
        {
            _blockCipher.Init(true, keyParam);
            int len;
            while ((len = input.Read(buffIn, 0, buffIn.Length)) > 0)
            {
                int processed = _blockCipher.ProcessBytes(buffIn, 0, len, buffOut, 0);
                if (processed > 0) output.Write(buffOut, 0, processed);
            }
            int tail = _blockCipher.DoFinal(buffOut, 0);
            output.Write(buffOut, 0, tail);
            _blockCipher.Reset();
        }

        File.Delete(plaintextCp.GetAbs());
        return encryptedCp;
    }

    /// <summary>加密 OFD 对象并写入容器。</summary>
    private ContainerPath EncryptElement(Ofd.Core.OFDElement obj, string outPath, ICipherParameters keyParam)
    {
        if (outPath.StartsWith('/')) outPath = outPath[1..];
        var res = ContainerPath.NewDatFile(outPath, _workDir);

        byte[] data = ElemCup.Dump(obj.GetProxy());
        var buffIn = new MemoryStream(data);
        var buffOut = new byte[4096 + _blockCipher.GetBlockSize()];

        _blockCipher.Init(true, keyParam);
        using (var output = File.Create(res.GetAbs()))
        {
            int len;
            while ((len = buffIn.Read(buffOut, 0, 4096)) > 0)
            {
                int processed = _blockCipher.ProcessBytes(buffOut, 0, len, buffOut, 0);
                if (processed > 0) output.Write(buffOut, 0, processed);
            }
            int tail = _blockCipher.DoFinal(buffOut, 0);
            output.Write(buffOut, 0, tail);
            _blockCipher.Reset();
        }
        return res;
    }

    /// <summary>关闭并删除工作目录。</summary>
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
            throw new IOException("无法删除加密器的工作空间，原因：" + e.Message, e);
        }
    }

    public void Dispose() => Close();
}
