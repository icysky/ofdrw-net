namespace Ofd.Reader;

/// <summary>错误 OFD 文件结构和文档格式异常（Java BadOFDException extends RuntimeException）。</summary>
public class BadOFDException : Exception
{
    public BadOFDException() { }

    public BadOFDException(string message) : base(message) { }

    public BadOFDException(string message, Exception? cause) : base(message, cause) { }

    public BadOFDException(Exception cause) : base(cause.Message, cause) { }
}
