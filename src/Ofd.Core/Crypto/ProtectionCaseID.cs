namespace Ofd.Core.Crypto;

/// <summary>密码保护方案标识符（GMT 0099-2020 A.1 表 A.1）。</summary>
public enum ProtectionCaseID
{
    /// <summary>口令方式加密（国密）：SM4-CBC + PKCS#7，IV 放密钥描述文件</summary>
    EncryptGMPassword,

    /// <summary>证书方式加密（国密）：SM4-CBC + SM2</summary>
    EncryptGMCert,

    /// <summary>证书数字签名（GB/T 35275 SM2 签名消息语法）</summary>
    Signature,
}

public static class ProtectionCaseIDExtensions
{
    /// <summary>获取 ID 实例，未知返回 null（与 Java 一致）。</summary>
    public static ProtectionCaseID? GetInstance(string? id) => id switch
    {
        "1.1.1" => ProtectionCaseID.EncryptGMPassword,
        "1.1.2" => ProtectionCaseID.EncryptGMCert,
        "1.2.1" => ProtectionCaseID.Signature,
        _ => null,
    };

    /// <summary>方案标识符字符串（1.1.1 / 1.1.2 / 1.2.1）。</summary>
    public static string ToIdValue(this ProtectionCaseID caseId) => caseId switch
    {
        ProtectionCaseID.EncryptGMPassword => "1.1.1",
        ProtectionCaseID.EncryptGMCert => "1.1.2",
        _ => "1.2.1",
    };
}
