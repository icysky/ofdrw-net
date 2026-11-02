namespace Ofd.Sign.Verify.Exceptions;

/// <summary>OFD 电子签名验证异常（Java OFDVerifyException）。</summary>
public class OFDVerifyException : Exception
{
    public OFDVerifyException(string message) : base(message) { }

    public OFDVerifyException(string message, Exception cause) : base(message, cause) { }

    public OFDVerifyException() { }
}
