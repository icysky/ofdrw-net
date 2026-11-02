namespace Ofd.Sign.Verify.Exceptions;

/// <summary>文件未进行电子签名（Java DocNotSignException）。</summary>
public class DocNotSignException : OFDVerifyException
{
    public DocNotSignException(string message) : base(message) { }
}
