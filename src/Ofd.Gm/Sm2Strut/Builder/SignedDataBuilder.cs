using Ofd.Gm.Cert;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Gm.Sm2Strut.Builder;

/// <summary>
/// 签名数据类型构造器（Java SignedDataBuilder）：把 BC 产生的 SM2 签名组装为 GB/T 35275 signedData。
/// </summary>
public static class SignedDataBuilder
{
    /// <summary>组装签名数据类型（单签名者）。</summary>
    public static SignedData SignedData(byte[] plaintext, byte[] signature, BcX509Certificate certificate)
    {
        if (plaintext == null || plaintext.Length == 0) throw new ArgumentException("签名原文(plaintext)为空");
        if (signature == null || signature.Length == 0) throw new ArgumentException("签名值(signature)为空");
        if (certificate == null) throw new ArgumentException("证书(certificate)为空");

        var certSigArr = new List<CertSigHolder>(1) { new(signature, certificate) };
        return SignedData(plaintext, certSigArr, null);
    }

    /// <summary>组装签名数据类型（多签名者 + 可选额外证书）。</summary>
    public static SignedData SignedData(byte[] plaintext, List<CertSigHolder> certSigArr,
        List<BcX509Certificate>? extCertArr)
    {
        if (plaintext == null || plaintext.Length == 0) throw new ArgumentException("签名原文(plaintext)为空");
        if (certSigArr == null || certSigArr.Count == 0) throw new ArgumentException("证书、签名值列表为空");

        // 摘要算法集合固定 SM3
        var digestAlgorithms = new DerSet(new AlgorithmIdentifier(OIDs.Sm3));
        // 待签名数据内容
        var contentInfo = new ContentInfo(OIDs.Data, new DerOctetString(plaintext));

        var certVector = new Asn1EncodableVector();
        foreach (var holder in certSigArr)
            certVector.Add(holder.GetAsn1Cert());

        if (extCertArr != null)
            foreach (var c in extCertArr)
                certVector.Add(CertTools.Asn1(c));

        var certificates = new DerSet(certVector);

        var signerVector = new Asn1EncodableVector();
        foreach (var item in certSigArr)
            signerVector.Add(Sm2Signer(item.GetSignature(), item.GetAsn1Cert()));

        var signerInfos = new DerSet(signerVector);
        return new SignedData(digestAlgorithms, contentInfo, certificates, signerInfos);
    }

    /// <summary>构造签名者信息（摘要算法 SM3、签名算法 SM2）。</summary>
    public static SignerInfo Sm2Signer(byte[] signature, Asn1Sequence certificate)
    {
        var tbs = TbsCertificateStructure.GetInstance(certificate[0]);
        var issuerAndSerialNumber = new IssuerAndSerialNumber(tbs.Issuer, tbs.SerialNumber);

        return new SignerInfo(
            issuerAndSerialNumber,
            new AlgorithmIdentifier(OIDs.Sm3),
            new AlgorithmIdentifier(OIDs.Sm2Sign),
            new DerOctetString(signature));
    }
}
