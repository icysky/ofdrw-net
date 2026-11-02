using Ofd.Core.BasicType;
using Ofd.Core.Signatures;
using Ofd.Gm.Ses.Parse;
using Ofd.Pkg;
using Ofd.Pkg.Container;
using Ofd.Reader;
using Ofd.Sign.Verify.Exceptions;
using Org.BouncyCastle.Security;
using SigSignature = Ofd.Core.Signatures.Sig.Signature;

namespace Ofd.Sign.Verify;

/// <summary>
/// OFD 电子签名验证引擎（Java OFDValidator）：
/// 逐签名校验「保护文件摘要 → 印章匹配 → 签名值」。
/// </summary>
public class OFDValidator : IDisposable
{
    private readonly OFDReader _reader;
    private readonly OFDDir _ofdDir;
    private readonly ResourceLocator _rl;
    private IValidateContainer? _validator;

    public OFDValidator(OFDReader reader)
    {
        _reader = reader;
        _ofdDir = reader.GetOFDDir();
        _rl = reader.GetResourceLocator();
    }

    /// <summary>设置电子签章数据验证容器。</summary>
    public OFDValidator SetValidator(IValidateContainer? validator)
    {
        _validator = validator ?? throw new ArgumentException("电子签章数据验证容器（validator）为空");
        return this;
    }

    /// <summary>执行 OFD 电子签名验证（失败抛 OFDVerifyException 子类）。</summary>
    public void ExeValidate()
    {
        _rl.Save();
        try
        {
            _rl.Cd("/");

            var signsListLoc = _reader.GetDefaultDocSignaturesPath()
                               ?? throw new DocNotSignException("文件未进行电子签名");

            var sigList = _rl.Get(signsListLoc, e => new Signatures(e));

            // 切换到签名列表文件所在目录 /Doc_0/Signs
            _rl.Cd(signsListLoc.Parent());

            var signatures = sigList.GetSignatures();
            if (signatures.Count == 0) throw new DocNotSignException("文件未进行电子签名");

            foreach (var sigRecord in signatures)
            {
                var type = sigRecord.GetType();
                var signFileLoc = sigRecord.GetBaseLoc();

                string signatureFilePath = _rl.GetFile(signFileLoc);
                var sig = _rl.Get(signFileLoc, e => new SigSignature(e));

                _rl.Save();
                try
                {
                    _rl.Cd(signFileLoc.Parent());

                    // 1. 检查被保护文件完整性
                    CheckFileIntegrity(sig);

                    string signedValueFilePath = _rl.GetFile(
                        sig.GetSignedValue() ?? throw new OFDVerifyException("签名描述文件缺少 SignedValue"));

                    // 2. 电子签章：检查印章匹配
                    if (type == SigType.Seal)
                    {
                        var seal = sig.GetSignedInfo().GetSeal();
                        if (seal != null)
                        {
                            string sealFilePath = _rl.GetFile(seal.GetBaseLoc());
                            if (!CheckSealMatch(sealFilePath, signedValueFilePath))
                                throw new OFDVerifyException("印章(Seal.esl)与电子签章数据(SignedValue.dat)中的印章不匹配");
                        }
                    }

                    // 3. 验证签名/签章值
                    string alg = sig.GetSignedInfo().GetSignatureMethod()
                                 ?? throw new OFDVerifyException("签名方法(SignatureMethod)缺失");
                    CheckSignedValue(type, alg, signatureFilePath, signedValueFilePath);
                }
                finally
                {
                    _rl.Restore();
                }
            }
        }
        finally
        {
            _rl.Restore();
        }
    }

    /// <summary>检查电子签章数据（委托给验证容器）。</summary>
    public void CheckSignedValue(SigType type, string alg, string signatureFilePath, string signedValuePath)
    {
        if (_validator == null)
            throw new ArgumentException("电子签章数据验证容器（validator）为空,Call #SetValidator");

        _validator.Validate(type, alg, File.ReadAllBytes(signatureFilePath), File.ReadAllBytes(signedValuePath));
    }

    /// <summary>检查被保护文件的完整性（是否被篡改）。</summary>
    private void CheckFileIntegrity(SigSignature sig)
    {
        var signedInfo = sig.GetSignedInfo();
        var references = signedInfo.GetReferences();

        string checkMethod = references.GetCheckMethod() ?? "SM3";
        var md = DigestUtilities.GetDigest(checkMethod);

        foreach (var reference in references.GetReferences())
        {
            var fileRef = reference.GetFileRef() ?? throw new OFDVerifyException("Reference 缺少 FileRef");
            string file = _rl.GetFile(fileRef);
            byte[] expectDataHash = reference.GetCheckValue();

            using (var input = File.OpenRead(file))
            {
                var buffer = new byte[4096];
                int len;
                while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
                    md.BlockUpdate(buffer, 0, len);
            }

            var actualDataHash = new byte[md.GetDigestSize()];
            md.DoFinal(actualDataHash, 0);

            if (!expectDataHash.SequenceEqual(actualDataHash))
                throw new FileIntegrityException(fileRef, expectDataHash, actualDataHash);

            md.Reset();
        }
    }

    /// <summary>电子印章与电子签章数据的匹配性检查。</summary>
    private static bool CheckSealMatch(string sealPath, string signedValuePath)
    {
        byte[] sesSignatureBin = File.ReadAllBytes(signedValuePath);
        var holder = VersionParser.ParseSesSignatureVersion(sesSignatureBin);

        byte[] expect = holder.GetVersion() switch
        {
            SESVersion.V1 => holder.GetInstanceV1()!.GetToSign()!.GetEseal()!.GetEncoded(),
            SESVersion.V4 => holder.GetInstanceV4()!.GetToSign()!.GetEseal()!.GetEncoded(),
            SESVersion.V5 => holder.GetInstanceV5()!.GetToSign()!.GetEseal()!.GetEncoded(),
            _ => throw new OFDVerifyException("未知的电子签章数据版本，无法解析"),
        };

        return expect.SequenceEqual(File.ReadAllBytes(sealPath));
    }

    public void Close() => _reader.Close();

    public void Dispose() => Close();
}
