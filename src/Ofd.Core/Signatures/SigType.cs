namespace Ofd.Core.Signatures;

/// <summary>签名节点的类型（第 18 章 图 85 表 66），默认 Seal。</summary>
public enum SigType
{
    /// <summary>安全签章（默认）</summary>
    Seal,

    /// <summary>纯数字签名</summary>
    Sign,
}

public static class SigTypeExtensions
{
    /// <summary>获取签名类型，空值默认 Seal，未知值抛异常（与 Java 一致）。</summary>
    public static SigType GetInstance(string? str)
    {
        str = (str ?? "").Trim();
        if (str.Length == 0 || str.Equals("Seal", StringComparison.OrdinalIgnoreCase)) return SigType.Seal;
        if (str.Equals("Sign", StringComparison.OrdinalIgnoreCase)) return SigType.Sign;
        throw new ArgumentException("未知的签名节点的类型：" + str);
    }
}
