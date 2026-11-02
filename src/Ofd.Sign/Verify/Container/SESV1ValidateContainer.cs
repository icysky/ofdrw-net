using Ofd.Core.Signatures;
using Ofd.Sign.Verify.Exceptions;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.X509;
using SesSignatureV1 = Ofd.Gm.Ses.V1.SES_Signature;

namespace Ofd.Sign.Verify.Container;

/// <summary>GM/T 0031-2014 电子印章数据验证（Java SESV1ValidateContainer）。</summary>
public class SESV1ValidateContainer : IValidateContainer
{
    public void Validate(SigType type, string signAlgName, byte[] tbsContent, byte[] signedValue)
    {
        if (type == SigType.Sign)
            throw new ArgumentException("签名类型(type)必须是 Seal，不支持电子印章验证");

        // 原文摘要
        var md = new SM3Digest();
        md.BlockUpdate(tbsContent, 0, tbsContent.Length);
        var actualDataHash = new byte[md.GetDigestSize()];
        md.DoFinal(actualDataHash, 0);

        var sesSignature = SesSignatureV1.GetInstance(signedValue)
                           ?? throw new InvalidSignedValueException("无法解析电子签章数据(SignedValue.dat)");
        var toSign = sesSignature.GetToSign()
                     ?? throw new InvalidSignedValueException("电子签章数据缺少待签章信息");

        byte[] expectDataHash = toSign.GetDataHash()?.GetBytes() ?? Array.Empty<byte>();
        if (!actualDataHash.SequenceEqual(expectDataHash))
        {
            throw new InvalidSignedValueException(
                "Signature.xml 文件被篡改，电子签章失效。(" + toSign.GetPropertyInfo()?.GetString() + ")");
        }

        // 用 TBS 内的证书验签
        byte[] expSigVal = sesSignature.GetSignature()?.GetBytes() ?? Array.Empty<byte>();
        byte[] certDer = toSign.GetCert()?.GetOctets()
                         ?? throw new InvalidSignedValueException("电子签章数据缺少签章者证书");

        var signCert = new X509CertificateParser().ReadCertificate(certDer);

        var verifier = new SM2Signer();
        verifier.Init(false, signCert.GetPublicKey());
        byte[] tbs = toSign.GetEncoded();
        verifier.BlockUpdate(tbs, 0, tbs.Length);

        if (!verifier.VerifySignature(expSigVal))
            throw new InvalidSignedValueException("电子签章数据签名值不匹配，电子签章数据失效。");
    }
}
