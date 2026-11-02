using Ofd.Core.Signatures;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;

namespace Ofd.Sign;

/// <summary>扩展数字签名容器（Java ExtendSignatureContainer）。</summary>
public interface IExtendSignatureContainer
{
    /// <summary>提供文件的摘要算法功能（如 SM3）。</summary>
    IDigest GetDigestFnc();

    /// <summary>签名方法 OID。</summary>
    DerObjectIdentifier GetSignAlgOID();

    /// <summary>对待签名数据签名（实现中不要关闭传入流）。</summary>
    byte[] Sign(Stream inData, string propertyInfo);

    /// <summary>获取电子印章二进制编码（纯签名返回 null）。</summary>
    byte[]? GetSeal();

    /// <summary>获取签名节点类型。</summary>
    SigType GetSignType();
}
