namespace Ofd.Sign;

/// <summary>签章模式（Java SignMode）。</summary>
public enum SignMode
{
    /// <summary>保护整个文件：该模式下不允许继续签名/章。</summary>
    WholeProtected,

    /// <summary>继续签章：忽略对 Signatures.xml 的保护。</summary>
    ContinueSign,
}
