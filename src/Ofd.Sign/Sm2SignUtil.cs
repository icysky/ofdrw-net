using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Ofd.Sign;

/// <summary>SM2 签名工具（DSA 编码 r,s，供各签名容器复用）。</summary>
internal static class Sm2SignUtil
{
    /// <summary>使用私钥对数据做 SM2 签名，返回 DER 编码签名值。</summary>
    public static byte[] Sign(byte[] data, ECPrivateKeyParameters privateKey)
    {
        var signer = new SM2Signer();
        signer.Init(true, privateKey);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    /// <summary>读取流全部字节（不关闭流）。</summary>
    public static byte[] ReadAll(Stream input)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>计算流的 SM3 摘要。</summary>
    public static byte[] Sm3OfStream(Stream input)
    {
        var md = new Org.BouncyCastle.Crypto.Digests.SM3Digest();
        var buffer = new byte[4096];
        int len;
        while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            md.BlockUpdate(buffer, 0, len);

        var output = new byte[md.GetDigestSize()];
        md.DoFinal(output, 0);
        return output;
    }
}
