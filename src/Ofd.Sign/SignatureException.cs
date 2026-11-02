namespace Ofd.Sign;

/// <summary>签名异常（Java SignatureException）。</summary>
public class SignatureException : Exception
{
    public SignatureException(string message) : base(message) { }

    public SignatureException(string message, Exception cause) : base(message, cause) { }
}
