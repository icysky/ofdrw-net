using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.Clips;

namespace Ofd.Core.PageDescription;

/// <summary>
/// 图元对象的非泛型视图（对应 Java 的 CT_GraphicUnit&lt;?&gt; 通配用法），
/// 供绘制上下文统一设置边界、透明度、变换矩阵、绘制参数与裁剪区。
/// </summary>
public interface IGraphicUnit
{
    /// <summary>获取外接矩形。</summary>
    ST_Box? GetBoundaryBox();

    /// <summary>设置外接矩形。</summary>
    void ApplyBoundary(ST_Box boundary);

    /// <summary>设置透明度 [0,255]。</summary>
    void ApplyAlpha(int? alpha);

    /// <summary>获取变换矩阵。</summary>
    ST_Array? GetCtmArray();

    /// <summary>设置变换矩阵。</summary>
    void ApplyCtm(ST_Array ctm);

    /// <summary>设置绘制参数引用。</summary>
    void ApplyDrawParam(ST_RefID id);

    /// <summary>设置裁剪区。</summary>
    void ApplyClips(global::Ofd.Core.PageDescription.Clips.Clips? clips);
}
