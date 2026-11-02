using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Sm2Strut;

/// <summary>GB/T 35275-2017 第 5 章与 GB/T 33560-2017 附录 A 的对象标识符定义。</summary>
public static class OIDs
{
    /// <summary>SM2 密码算法加密签名消息语法规范。</summary>
    public static readonly DerObjectIdentifier Gmt35275Sm2 = new("1.2.156.10197.6.1.4.2");

    /// <summary>数据类型。</summary>
    public static readonly DerObjectIdentifier Data = Gmt35275Sm2.Branch("1");

    /// <summary>签名数据类型。</summary>
    public static readonly DerObjectIdentifier SignedData = Gmt35275Sm2.Branch("2");

    /// <summary>数字信封类型。</summary>
    public static readonly DerObjectIdentifier EnvelopedData = Gmt35275Sm2.Branch("3");

    /// <summary>签名及数字信封类型。</summary>
    public static readonly DerObjectIdentifier SignedAndEnvelopedData = Gmt35275Sm2.Branch("4");

    /// <summary>加密数据类型。</summary>
    public static readonly DerObjectIdentifier EncryptedData = Gmt35275Sm2.Branch("5");

    /// <summary>密钥协商类型。</summary>
    public static readonly DerObjectIdentifier KeyAgreementInfo = Gmt35275Sm2.Branch("6");

    /// <summary>SM4 分组密码算法。</summary>
    public static readonly DerObjectIdentifier Sm4 = new("1.2.156.10197.1.100");

    /// <summary>SM2 椭圆曲线公钥密码算法。</summary>
    public static readonly DerObjectIdentifier Sm2 = new("1.2.156.10197.1.301");

    /// <summary>SM2-1 数字签名算法。</summary>
    public static readonly DerObjectIdentifier Sm2Sign = Sm2.Branch("1");

    /// <summary>SM2-2 密钥交换协议。</summary>
    public static readonly DerObjectIdentifier Sm2KeyExchange = Sm2.Branch("2");

    /// <summary>SM2-3 公钥加密算法。</summary>
    public static readonly DerObjectIdentifier Sm2Encrypt = Sm2.Branch("3");

    /// <summary>SM3 密码杂凑算法。</summary>
    public static readonly DerObjectIdentifier Sm3 = new("1.2.156.10197.1.401");

    /// <summary>SM3 密码杂凑算法，无密钥使用。</summary>
    public static readonly DerObjectIdentifier Sm3_1 = Sm3.Branch("1");

    /// <summary>SM3 密码杂凑算法，有密钥使用（上游同 Sm3_1，保持原样）。</summary>
    public static readonly DerObjectIdentifier Sm3_2 = Sm3.Branch("1");

    /// <summary>基于 SM2 算法和 SM3 算法的签名。</summary>
    public static readonly DerObjectIdentifier Sm3WithSm2 = new("1.2.156.10197.501");
}
