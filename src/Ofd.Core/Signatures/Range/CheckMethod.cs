namespace Ofd.Core.Signatures.Range;

/// <summary>摘要算法（References.CheckMethod 的可选值），默认 SM3 用于国密场景。</summary>
public enum CheckMethod
{
    MD5,
    SHA1,
    SHA256,
    SM3,
}
