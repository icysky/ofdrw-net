namespace Ofd.Layout.Exceptions;

/// <summary>文档读取异常（Java DocReadException）。</summary>
public class DocReadException : Exception
{
    public DocReadException() { }

    public DocReadException(string message) : base(message) { }

    public DocReadException(string message, Exception cause) : base(message, cause) { }
}
