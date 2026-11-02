using Ofd.Gm.Cert;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Digests;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using CmsAttribute = Org.BouncyCastle.Asn1.Cms.Attribute;

namespace Ofd.Gm.Sm2Strut.Builder;

/// <summary>
/// 签名数据类型构造器（Java PKCS9SignedDataBuilder）：
/// 签名对象为 signerInfos 的 authenticatedAttributes（内部为 PKCS#9 属性）。
/// </summary>
public static class PKCS9SignedDataBuilder
{
    /// <summary>
    /// 组装签名数据类型。
    /// </summary>
    /// <param name="plaintext">需要保护的原文</param>
    /// <param name="signFnc">签名函数：输入待签字节，返回签名值（DER）</param>
    /// <param name="signCert">签名使用的证书</param>
    /// <param name="extCertArr">扩展证书序列（如根证书），可选</param>
    public static SignedData SignedData(byte[] plaintext, Func<byte[], byte[]> signFnc,
        BcX509Certificate signCert, List<BcX509Certificate>? extCertArr)
    {
        if (plaintext == null || plaintext.Length == 0) throw new ArgumentException("签名原文(plaintext)为空");
        if (signCert == null) throw new ArgumentException("证书(signCert)为空");
        if (signFnc == null) throw new ArgumentException("签名函数(signFnc)为空");

        var digestAlgorithms = new DerSet(new AlgorithmIdentifier(OIDs.Sm3));
        // PKCS#9 模式下原文不放入 content
        var contentInfo = new ContentInfo(OIDs.Data, null);

        byte[] digest = Sm3(plaintext);

        var certVector = new Asn1EncodableVector();
        var signerCert = CertTools.Asn1(signCert);
        certVector.Add(signerCert);
        if (extCertArr != null)
            foreach (var c in extCertArr)
                certVector.Add(CertTools.Asn1(c));
        var certificates = new DerSet(certVector);

        var tbs = TbsCertificateStructure.GetInstance(signerCert[0]);
        var isn = new IssuerAndSerialNumber(tbs.Issuer, tbs.SerialNumber);

        var signerInfo = Sign(digest, signFnc, isn);
        var signerInfos = new DerSet(new Asn1EncodableVector { signerInfo });

        return new SignedData(digestAlgorithms, contentInfo, certificates, signerInfos);
    }

    /// <summary>构造签名者信息：authenticatedAttributes 含 contentType/signingTime/messageDigest。</summary>
    public static SignerInfo Sign(byte[] digest, Func<byte[], byte[]> signFnc, IssuerAndSerialNumber isn)
    {
        var contentType = new CmsAttribute(CmsAttributes.ContentType, new DerSet(OIDs.Data));
        var signingTime = new CmsAttribute(CmsAttributes.SigningTime, new DerSet(new DerUtcTime(DateTime.Now)));
        var messageDigest = new CmsAttribute(CmsAttributes.MessageDigest, new DerSet(new DerOctetString(digest)));

        var authenticatedAttributes = new DerSet(new Asn1EncodableVector
        {
            contentType, signingTime, messageDigest,
        });

        byte[] signature = signFnc(authenticatedAttributes.GetEncoded());

        var res = new SignerInfo(
            isn,
            new AlgorithmIdentifier(OIDs.Sm3),
            new AlgorithmIdentifier(OIDs.Sm2Sign),
            new DerOctetString(signature));

        res.SetAuthenticatedAttributes(authenticatedAttributes);
        return res;
    }

    /// <summary>SM3 摘要。</summary>
    public static byte[] Sm3(byte[] data)
    {
        var md = new SM3Digest();
        md.BlockUpdate(data, 0, data.Length);
        var output = new byte[md.GetDigestSize()];
        md.DoFinal(output, 0);
        return output;
    }
}
