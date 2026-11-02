using Ofd.Core.Signatures;
using Ofd.Sign.Verify.Exceptions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Signers;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Sign.Verify.Container;

/// <summary>
/// 数字签名验证容器（Java DigitalValidateContainer）。
/// </summary>
[Obsolete("OFD 的数字签名应符合 GB/T 35275，请使用 GBT35275ValidateContainer")]
public class DigitalValidateContainer : IValidateContainer
{
    /// <summary>验证使用的公钥。</summary>
    public readonly AsymmetricKeyParameter Pk;

    public DigitalValidateContainer(AsymmetricKeyParameter pk)
    {
        Pk = pk ?? throw new ArgumentException("验证使用的公钥参数(pk)不能为空");
    }

    public DigitalValidateContainer(BcX509Certificate certificate) : this(certificate.GetPublicKey()) { }

    public void Validate(SigType type, string alg, byte[] tbsContent, byte[] signedValue)
    {
        if (type != SigType.Sign)
            throw new ArgumentException("签名类型(type)必须是 Sign，不支持电子印章验证");

        var verifier = new SM2Signer();
        verifier.Init(false, Pk);
        verifier.BlockUpdate(tbsContent, 0, tbsContent.Length);

        if (!verifier.VerifySignature(signedValue))
            throw new InvalidSignedValueException("签名值不一致");
    }
}
