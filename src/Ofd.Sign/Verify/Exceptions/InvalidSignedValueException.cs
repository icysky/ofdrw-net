namespace Ofd.Sign.Verify.Exceptions;

/// <summary>电子签章数据失效（Java InvalidSignedValueException）。</summary>
public class InvalidSignedValueException : OFDVerifyException
{
    public InvalidSignedValueException(string message) : base(message) { }

    public InvalidSignedValueException(string message, Exception cause) : base(message, cause) { }
}
