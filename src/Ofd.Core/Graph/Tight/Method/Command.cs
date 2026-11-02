namespace Ofd.Core.Graph.Tight.Method;

/// <summary>路径操作（表 37 图形对象描述方法：移动、划线等）的抽象基类。</summary>
public abstract class Command : OFDElement
{
    protected Command(Dom.DomElement proxy) : base(proxy) { }

    protected Command(string name) : base(name) { }
}
