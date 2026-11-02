using Ofd.Core.Signatures;

namespace Ofd.Sign.Verify;

/// <summary>电子签章数据验证容器（Java SignedDataValidateContainer）。</summary>
public interface IValidateContainer
{
    /// <summary>
    /// 验证签名/签章数据。
    /// </summary>
    /// <param name="type">验证类型（数字签名 / 电子签章）</param>
    /// <param name="signAlgName">签名算法名称（OID 字符串）</param>
    /// <param name="tbsContent">被签名的原文（Signature.xml 字节）</param>
    /// <param name="signedValue">签名值（SignedValue.dat 字节）</param>
    void Validate(SigType type, string signAlgName, byte[] tbsContent, byte[] signedValue);
}
