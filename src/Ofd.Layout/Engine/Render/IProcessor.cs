using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Layout.Element;

namespace Ofd.Layout.Engine.Render;

/// <summary>
/// 元素绘制器（Java Processor 函数式接口）：把 OFDRW 元素转换为 OFD 图元。
/// </summary>
public interface IProcessor
{
    /// <summary>处理元素转换为 OFD 元素。</summary>
    void Render(ST_Loc pageLoc, CT_PageBlock layer, ResManager resManager, Div e, AtomicCounter maxUnitId);
}
