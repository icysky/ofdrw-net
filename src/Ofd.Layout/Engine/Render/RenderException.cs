namespace Ofd.Layout.Engine.Render;

/// <summary>渲染异常（Java RenderException）。</summary>
public class RenderException : Exception
{
    public RenderException() { }

    public RenderException(string message) : base(message) { }

    public RenderException(string message, Exception cause) : base(message, cause) { }

    public RenderException(Exception cause) : base(cause.Message, cause) { }
}
