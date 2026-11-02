namespace Ofd.Reader;

/// <summary>错误路径异常（Java ErrorPathException extends RuntimeException）。</summary>
public class ErrorPathException : Exception
{
    public ErrorPathException() { }

    public ErrorPathException(string message) : base(message) { }

    public ErrorPathException(string message, Exception? cause) : base(message, cause) { }

    public ErrorPathException(Exception cause) : base(cause.Message, cause) { }
}
