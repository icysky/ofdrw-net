namespace Ofd.Layout.Handler;

/// <summary>
/// 虚拟页面处理器（Java VPageHandler）：在生成 Content.xml 前处理虚拟页面（页头/页脚等）。
/// </summary>
public interface IVPageHandler
{
    /// <summary>执行处理。</summary>
    void Handle(VirtualPage page);
}

/// <summary>
/// 文件渲染结束回调（Java RenderFinishHandler）。
/// </summary>
public interface IRenderFinishHandler
{
    /// <summary>文件渲染结束时触发。</summary>
    void Handle(Engine.AtomicCounter maxUnitId, Ofd.Pkg.Container.OFDDir ofdDir, int index);
}
