using Ofd.Core.BasicType;

namespace Ofd.Sign.Verify.Exceptions;

/// <summary>文件完整性验证异常（Java FileIntegrityException）：被保护文件内容与摘要不符。</summary>
public class FileIntegrityException : OFDVerifyException
{
    /// <summary>被检测文件在 OFD 容器中的绝对路径。</summary>
    private readonly ST_Loc? _fileAbsPath;

    /// <summary>预期的文件杂凑值。</summary>
    private readonly byte[]? _exceptDataHash;

    /// <summary>实际的文件杂凑值。</summary>
    private readonly byte[]? _actualDataHash;

    public FileIntegrityException(ST_Loc fileAbsPath, byte[] exceptDataHash, byte[] actualDataHash)
        : base(fileAbsPath.Loc + " 文件被篡改")
    {
        _fileAbsPath = fileAbsPath;
        _exceptDataHash = exceptDataHash;
        _actualDataHash = actualDataHash;
    }

    public FileIntegrityException() { }

    public FileIntegrityException(string msg) : base(msg) { }

    public FileIntegrityException(string message, Exception cause) : base(message, cause) { }

    public ST_Loc? GetFileAbsPath() => _fileAbsPath;

    public byte[]? GetExceptDataHash() => _exceptDataHash;

    public byte[]? GetActualDataHash() => _actualDataHash;
}
