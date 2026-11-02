namespace Ofd.Core.Graph.Tight.Method;

/// <summary>自动闭合到当前路径的起始点，并以该点为当前点（表 37）。</summary>
public class Close : Command
{
    public Close(Dom.DomElement proxy) : base(proxy) { }

    public Close() : base("Close") { }

    /// <summary>ABNF 指令 "C"。</summary>
    public override string ToString() => "C";
}
