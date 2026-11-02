using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Ofd.Gm.Sm2Strut;

/// <summary>
/// GB/T 35275 signedData 验证器（Java GBT35275Validate）。
/// <para>GM/T 0099-2020 7.2.2 b)：签名算法使用 SM2 时，签名值数据应符合 GB/T 35275。</para>
/// </summary>
public static class GBT35275Validate
{
    /// <summary>SM2 默认用户标识（与 Java JCE 的 SM2 签名一致）。</summary>
    private static readonly byte[] DefaultUserId = Encoding.UTF8.GetBytes("1234567812345678");

    /// <summary>
    /// 验证 GB/T 35275 SignedData 签名值。
    /// </summary>
    /// <param name="alg">算法名（保留参数以对齐 Java API；C# 固定使用 SM2+SM3 验证）</param>
    /// <param name="tbsContent">待验证原文（不需预计算摘要）</param>
    /// <param name="signedValue">签名值 DER 编码</param>
    public static VerifyInfo Validate(string alg, byte[] tbsContent, byte[] signedValue)
    {
        var contentInfo = ContentInfo.GetInstance(signedValue);
        if (contentInfo == null) throw new ArgumentException("无法解析ContentInfo结构");

        if (!OIDs.SignedData.Equals(contentInfo.GetContentType()))
            throw new ArgumentException("非法的签名数据类型，类型：" + contentInfo.GetContentType());

        var signedData = SignedData.GetInstance(contentInfo.GetContent());
        if (signedData == null) throw new ArgumentException("无法解析签名值格式，不符 GBT35275");

        // a) 计算原文杂凑值
        byte[] digestAct = Digest(tbsContent);
        byte[]? plaintext = null;

        var dataContent = signedData.GetContentInfo()?.GetContent();
        if (dataContent != null)
        {
            plaintext = Asn1OctetString.GetInstance(dataContent).GetOctets();
            if (!digestAct.SequenceEqual(plaintext))
            {
                try
                {
                    // [兼容非规范格式] 原文可能以 Base64 文本形式保存
                    byte[] decode = Convert.FromBase64String(Encoding.UTF8.GetString(plaintext));
                    if (!digestAct.SequenceEqual(decode))
                        return VerifyInfo.Err("待签名原文不符");
                }
                catch (Exception)
                {
                    return VerifyInfo.Err("待签名原文不符");
                }
            }
        }

        // b) 逐个签名者验证
        var signerInfos = signedData.GetSignerInfos();
        if (signerInfos == null) return VerifyInfo.Err("签名值中不存在签名者信息");

        foreach (var item in signerInfos)
        {
            var signerInfo = SignerInfo.GetInstance(item)!;
            var authenticatedAttributes = signerInfo.GetAuthenticatedAttributes();

            if (authenticatedAttributes != null)
            {
                // PKCS#9：messageDigest 属性
                var attributes = new AttributeTable(authenticatedAttributes);
                var attr = attributes[CmsAttributes.MessageDigest]
                           ?? throw new ArgumentException("PKCS#9 message-digest 属性缺失");

                if (attr.AttrValues[0] is not Asn1OctetString signedMessageDigest)
                    throw new ArgumentException("PKCS#9 message-digest 属性类型不是 'OCTET STRING'");

                if (!digestAct.SequenceEqual(signedMessageDigest.GetOctets()))
                    return VerifyInfo.Err("待签名原文不符");

                plaintext = authenticatedAttributes.GetEncoded();
            }

            if (plaintext == null) throw new ArgumentException("GBT35275签名值格式错误");

            var iaSn = signerInfo.GetIssuerAngSerialNumber();
            var cert = signedData.GetSignCertX509(iaSn);
            if (cert == null) return VerifyInfo.Err("没有找到匹配的证书无法验证签名");

            byte[] signature = signerInfo.GetEncryptedDigest()?.GetOctets() ?? Array.Empty<byte>();

            var signer = new SM2Signer();
            signer.Init(false, new ParametersWithID(cert.GetPublicKey(), DefaultUserId));
            signer.BlockUpdate(plaintext, 0, plaintext.Length);
            if (!signer.VerifySignature(signature))
                return VerifyInfo.Err("签名值不一致");
        }

        return VerifyInfo.Ok();
    }

    private static byte[] Digest(byte[] data)
    {
        var md = new SM3Digest();
        md.BlockUpdate(data, 0, data.Length);
        var output = new byte[md.GetDigestSize()];
        md.DoFinal(output, 0);
        return output;
    }
}
