namespace Ofd.Sign;

/// <summary>签名终止异常（Java SignatureTerminateException）：文档不允许继续追加签名。</summary>
public class SignatureTerminateException : Exception
{
    public SignatureTerminateException(string message) : base(message) { }

    public SignatureTerminateException(string message, Exception cause) : base(message, cause) { }
}
