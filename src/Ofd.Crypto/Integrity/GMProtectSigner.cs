using Ofd.Gm.Sm2Strut;
using Ofd.Gm.Sm2Strut.Builder;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Crypto.Integrity;

/// <summary>
/// 国密 SM2/SM3 完整性保护签名实现（Java GMProtectSigner），签名值符合 GB/T 35275。
/// </summary>
public class GMProtectSigner : IProtectSigner
{
    /// <summary>版式文件合成者的签名私钥。</summary>
    private readonly ECPrivateKeyParameters _privateKey;

    /// <summary>版式文件合成者的公钥证书。</summary>
    private readonly BcX509Certificate _signCert;

    public GMProtectSigner(ECPrivateKeyParameters privateKey, BcX509Certificate signCert)
    {
        _privateKey = privateKey ?? throw new ArgumentException("版式文件合成者的签名私钥(privateKey)为空");
        _signCert = signCert ?? throw new ArgumentException("版式文件合成者的公钥证书(signCert)为空");
    }

    /// <summary>计算文件 SM3 杂凑值并签名，返回 GB/T 35275 SignedData（DER）。</summary>
    public byte[] DigestThenSign(string tbsPath)
    {
        byte[] raw = File.ReadAllBytes(tbsPath);

        // 计算完整性保护文件的杂凑值，作为待签名原文
        var md = new SM3Digest();
        md.BlockUpdate(raw, 0, raw.Length);
        var plaintext = new byte[md.GetDigestSize()];
        md.DoFinal(plaintext, 0);

        // SM2 签名（DER 编码 r,s）
        var signer = new SM2Signer();
        signer.Init(true, _privateKey);
        signer.BlockUpdate(plaintext, 0, plaintext.Length);
        byte[] signature = signer.GenerateSignature();

        // 构造 GB/T 35275 数据格式
        var signedData = SignedDataBuilder.SignedData(plaintext, signature, _signCert);
        return new ContentInfo(OIDs.SignedData, signedData).GetEncoded();
    }
}
