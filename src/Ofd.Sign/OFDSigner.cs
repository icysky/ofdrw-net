using Ofd.Core.BasicType;
using Ofd.Core.Signatures;
using Ofd.Core.Signatures.Appearance;
using Ofd.Core.Signatures.Range;
using Ofd.Core.Signatures.Sig;
using Ofd.Pkg;
using Ofd.Pkg.Container;
using Ofd.Reader;
using Ofd.Sign.StampPos;
using Org.BouncyCastle.Crypto;
using SigSignature = Ofd.Core.Signatures.Sig.Signature;

namespace Ofd.Sign;

/// <summary>
/// OFD 文档数字签章引擎（Java OFDSigner）：构造签名列表 → 计算保护文件摘要 → 生成签名值 → 打包。
/// </summary>
public class OFDSigner : IDisposable
{
    /// <summary>时间日期格式。</summary>
    public const string DF = "yyyy-MM-dd HH:mm:ss";

    /// <summary>OFDRW 签名提供者。</summary>
    public static Provider OfdrwProvider() => new Provider()
        .SetProviderName("ofdrw-sign")
        .SetCompany("ofdrw")
        .SetVersion(GlobalVar.Version);

    private readonly OFDDir _ofdDir;
    private readonly OFDReader _reader;
    private readonly ISignIdProvider _maxSignId;
    private SignMode _signMode = SignMode.WholeProtected;
    private Parameters? _parameters;
    private ST_Loc? _signaturesLoc;
    private readonly List<IStampAppearance> _apList = new();
    private ProtectFileFilter? _protectFileFilter;
    private IExtendSignatureContainer? _signContainer;
    private string? _out;
    private Stream? _outStream;
    private bool _hasSign;
    private string? _relativeId;

    /// <summary>创建签名引擎（默认数字类型 ID 提供器）。</summary>
    public OFDSigner(OFDReader reader, Stream outStream) : this(reader, outStream, new NumberFormatAtomicSignId(false)) { }

    /// <summary>创建签名引擎（指定输出流与 ID 提供器）。</summary>
    public OFDSigner(OFDReader reader, Stream outStream, ISignIdProvider idProvider)
    {
        if (reader == null) throw new ArgumentException("OFD解析器（reader）为空");
        if (outStream == null) throw new ArgumentException("电子签名后文件输出流（outStream）为空");
        if (idProvider == null) throw new ArgumentException("签名文件ID提供器（idProvider）为空");

        _outStream = outStream;
        _ofdDir = reader.GetOFDDir();
        _reader = reader;
        _maxSignId = idProvider;
        PreChecker();
    }

    /// <summary>创建签名引擎（指定输出路径与 ID 提供器）。</summary>
    public OFDSigner(OFDReader reader, string outPath, ISignIdProvider idProvider)
    {
        if (reader == null) throw new ArgumentException("OFD解析器（reader）为空");
        if (outPath == null) throw new ArgumentException("电子签名后文件保存位置（out）为空");
        if (idProvider == null) throw new ArgumentException("签名文件ID提供器（idProvider）为空");

        _out = outPath;
        _ofdDir = reader.GetOFDDir();
        _reader = reader;
        _maxSignId = idProvider;
        PreChecker();
    }

    /// <summary>创建签名引擎（默认 'sNNN' 格式 ID 提供器）。</summary>
    public OFDSigner(OFDReader reader, string outPath) : this(reader, outPath, new StandFormatAtomicSignId()) { }

    public SignMode GetSignMode() => _signMode;

    public OFDSigner SetSignMode(SignMode signMode)
    {
        _signMode = signMode;
        return this;
    }

    /// <summary>设置电子签名实现容器。</summary>
    public OFDSigner SetSignContainer(IExtendSignatureContainer signContainer)
    {
        _signContainer = signContainer ?? throw new ArgumentException("签名实现容器（signContainer）为空");
        return this;
    }

    /// <summary>增加签章外观位置。</summary>
    public OFDSigner AddApPos(IStampAppearance? sa)
    {
        if (sa == null) return this;
        _apList.Add(sa);
        return this;
    }

    /// <summary>设置待保护文件过滤器。</summary>
    public OFDSigner SetProtectFileFilter(ProtectFileFilter? filter)
    {
        _protectFileFilter = filter;
        return this;
    }

    /// <summary>【可选 OFD 2.0】设置此签名基于的“基”签名 ID。</summary>
    public OFDSigner SetRelative(string? id)
    {
        _relativeId = id;
        return this;
    }

    /// <summary>设置签名扩展属性。</summary>
    public OFDSigner SetParameters(Parameters? parameters)
    {
        _parameters = parameters;
        return this;
    }

    /// <summary>预检查：是否需要更新 OFD.xml、Signatures.xml 是否已被保护。</summary>
    private void PreChecker()
    {
        var rl = _reader.GetResourceLocator();
        try
        {
            rl.Save();
            rl.Cd("/");

            _signaturesLoc = _reader.GetDefaultDocSignaturesPath();
            if (_signaturesLoc == null || !rl.Exist(_signaturesLoc.ToString())) return;

            var signatures = rl.Get(_signaturesLoc, e => new Signatures(e));

            // 载入文档中已有的最大签名 ID
            _maxSignId.SetCurrentMaxSignId(signatures.GetMaxSignId());

            string parent = _signaturesLoc.Parent();
            rl.Cd(parent);

            foreach (var sig in signatures.GetSignatures())
            {
                var baseLoc = sig.GetBaseLoc();
                var sigObj = rl.Get(baseLoc, e => new SigSignature(e));
                var refList = sigObj.GetSignedInfo().GetReferences();
                if (refList.HasFile(_signaturesLoc.Loc))
                    throw new SignatureTerminateException("签名列表文件（Signatures.xml）已经被保护，文档不允许继续追加签名");
            }
        }
        catch (FileNotFoundException e)
        {
            throw new BadOFDException("错误OFD结构和文件格式", e);
        }
        finally
        {
            rl.Restore();
        }
    }

    /// <summary>获取待计算杂凑值的文件列表（应用过滤器与继续签章规则）。</summary>
    private List<ToDigestFileInfo> ToBeDigestFileList()
    {
        var res = new List<ToDigestFileInfo>();

        string containerPath = _ofdDir.GetContainerPath();
        string sysRoot = Path.GetFullPath(containerPath).Replace('\\', '/');

        foreach (var file in Directory.EnumerateFiles(containerPath, "*", SearchOption.AllDirectories))
        {
            string absPath = Path.GetFullPath(file).Replace('\\', '/').Replace(sysRoot, "");
            var fileInfo = new ToDigestFileInfo(absPath, file);

            // 过滤器选择需要保护的文档
            if (_protectFileFilter != null && !_protectFileFilter(fileInfo.GetSysPath()))
                continue;

            // 继续签章模式跳过 Signatures.xml
            if (_signMode == SignMode.ContinueSign && absPath == _signaturesLoc?.Loc)
                continue;

            res.Add(fileInfo);
        }
        return res;
    }

    /// <summary>执行签名/签章。</summary>
    public Signatures ExeSign()
    {
        if (_signContainer == null)
            throw new ArgumentException("签名实现容器（signContainer）为空，请提供签名实现容器");

        _hasSign = true;

        // 获取数字签名存储目录并创建签名容器
        var signsDir = _ofdDir.ObtainDocDefault().ObtainSigns();
        var signDir = signsDir.NewSignDir();

        // 1. 获取签名列表文件对象（不存在则创建并更新 OFD.xml）
        var signListObj = _reader.GetDefaultSignatures();
        if (_signaturesLoc == null || signListObj == null)
        {
            signListObj = new Signatures();
            signsDir.SetSignatures(signListObj);
            _signaturesLoc = signsDir.GetAbsLoc().Cat(SignsDir.SignaturesFileName);

            _ofdDir.GetOfd().GetDocBody().SetSignatures(_signaturesLoc);
            _ofdDir.FlushFileByName(OFDDir.OFDFileName);
        }

        // 2. 向签名列表文件加入签名记录
        var signatureLoc = signDir.GetAbsLoc().Cat(SignDir.SignatureFileName);
        var signatureRecord = new Ofd.Core.Signatures.Signature()
            .SetId(_maxSignId.IncrementAndGet())
            .SetType(_signContainer.GetSignType())
            .SetBaseLoc(signatureLoc);

        if (!string.IsNullOrWhiteSpace(_relativeId))
            signatureRecord.SetRelative(_relativeId);

        signListObj.AddSignature(signatureRecord);

        // 3. 构建签名文件（算法/提供者/保护文件摘要）
        string signatureFilePath = BuildSignature(signsDir, signDir, signListObj);

        // 4. 计算签名值
        string propertyInfo = signDir.GetAbsLoc().Cat(SignDir.SignatureFileName).ToString();
        byte[] signedValue;
        using (var inData = File.OpenRead(signatureFilePath))
        {
            signedValue = _signContainer.Sign(inData, propertyInfo);
        }

        File.WriteAllBytes(Path.Combine(signDir.GetSysAbsPath(), SignDir.SignedValueFileName), signedValue);
        return signListObj;
    }

    /// <summary>构造签名文件并写入签名容器。</summary>
    private string BuildSignature(SignsDir signsDir, SignDir signDir, Signatures signListObj)
    {
        var signedInfo = new SignedInfo()
            .SetProvider(OfdrwProvider())
            .SetSignatureMethod(_signContainer!.GetSignAlgOID().GetID())
            .SetParameters(_parameters)
            .SetSignatureDateTime(DateTime.Now.ToString(DF));

        var signDirAbsLoc = signDir.GetAbsLoc();

        // 电子签章：写入印章文件并设置 Seal
        if (_signContainer.GetSignType() == SigType.Seal)
        {
            byte[]? sealBin = _signContainer.GetSeal();
            if (sealBin != null && sealBin.Length != 0)
            {
                File.WriteAllBytes(Path.Combine(signDir.GetSysAbsPath(), SignDir.SealFileName), sealBin);
                signedInfo.SetSeal(new Seal().SetBaseLoc(signDirAbsLoc.Cat(SignDir.SealFileName)));
            }
        }

        // 加入签名关联的外观
        foreach (var sa in _apList)
        {
            foreach (var annot in sa.GetAppearance(_reader, _maxSignId))
                signedInfo.AddStampAnnot(annot);
        }

        // 写完除签名值外的所有文件：更新最大 ID 与签名列表
        signListObj.SetMaxSignId(_maxSignId.Get());
        signsDir.FlushFileByName(SignsDir.SignaturesFileName);

        // 计算并设置所保护文件的摘要
        var md = _signContainer.GetDigestFnc();
        var references = new References().SetCheckMethod(md.AlgorithmName);

        foreach (var fileInfo in ToBeDigestFileList())
        {
            byte[] digest = CalculateFileDigest(md, fileInfo.GetSysPath());
            md.Reset();
            references.AddReference(new Reference()
                .SetFileRef(fileInfo.GetAbsPath())
                .SetCheckValue(digest));
        }
        signedInfo.SetReferences(references);

        var signature = new SigSignature()
            .SetSignedValue(signDirAbsLoc.Cat(SignDir.SignedValueFileName))
            .SetSignedInfo(signedInfo);

        signDir.SetSignature(signature);
        signDir.FlushFileByName(SignDir.SignatureFileName);

        return Path.Combine(signDir.GetSysAbsPath(), SignDir.SignatureFileName);
    }

    /// <summary>分块读取计算文件杂凑值。</summary>
    private static byte[] CalculateFileDigest(IDigest md, string path)
    {
        using var input = File.OpenRead(path);
        var buffer = new byte[4096];
        int len;
        while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            md.BlockUpdate(buffer, 0, len);

        var output = new byte[md.GetDigestSize()];
        md.DoFinal(output, 0);
        return output;
    }

    /// <summary>完成签名并打包输出。</summary>
    public void Close()
    {
        if (!_hasSign)
            throw new InvalidOperationException("请先执行 exeSign 在关闭引擎完成数字签名。");

        if (_out != null)
            _ofdDir.Jar(_out);
        else if (_outStream != null)
            _ofdDir.Jar(_outStream);
        else
            throw new ArgumentException("OFD文档输出目录错误或没有设置输出流");

        _reader.Close();
    }

    public void Dispose() => Close();
}
