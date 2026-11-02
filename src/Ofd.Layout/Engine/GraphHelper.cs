using Ofd.Core.Graph.PathObj;

namespace Ofd.Layout.Engine;

/// <summary>图形工具（Java GraphHelper）：以路径方式快速构建图形。</summary>
public static class GraphHelper
{
    /// <summary>创建一个矩形轮廓路径。</summary>
    public static AbbreviatedData Rect(double x, double y, double w, double h) =>
        new AbbreviatedData()
            .M(x, y)
            .L(x + w, y)
            .L(x + w, y + h)
            .L(x, y + h)
            .C();
}
