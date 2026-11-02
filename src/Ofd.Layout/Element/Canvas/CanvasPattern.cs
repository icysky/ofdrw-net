using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.Color.Pattern;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// Canvas 填充底纹（Java CanvasPattern）。
/// <para>重复样式支持 repeat / column / row / row-column，其余抛出异常。</para>
/// </summary>
public class CanvasPattern
{
    /// <summary>OFD 底纹。</summary>
    public readonly CT_Pattern Pattern;

    /// <summary>图片路径（绝对路径）。</summary>
    public readonly string Img;

    /// <summary>图形对象。</summary>
    public readonly ImageObject ImgObj;

    public CanvasPattern(string img, string repetition, ImageObject imgObj)
    {
        Pattern = new CT_Pattern();
        Img = Path.GetFullPath(img);
        ImgObj = imgObj;

        switch ((repetition ?? "").ToLowerInvariant())
        {
            case "repeat":
            case "":
                // 默认即 normal，无需设置
                break;
            case "column":
                Pattern.SetReflectMethod(ReflectMethod.Column);
                break;
            case "row":
                Pattern.SetReflectMethod(ReflectMethod.Row);
                break;
            case "row-column":
                Pattern.SetReflectMethod(ReflectMethod.RowAndColumn);
                break;
            default:
                throw new ArgumentException("不支持的重复样式：" + repetition);
        }

        var cellContent = new CellContent();
        cellContent.AddPageBlock(imgObj);
        Pattern.SetCellContent(cellContent);

        var boundary = imgObj.GetBoundary()
                       ?? throw new ArgumentException("图片对象缺少 Boundary");
        Pattern.SetWidth(boundary.Width);
        Pattern.SetHeight(boundary.Height);
    }

    /// <summary>设置底纹单元变换矩阵（[a,b,c,d,e,f] 6 参数）。</summary>
    public void SetTransform(double[] matrix)
    {
        if (matrix == null || matrix.Length < 6) throw new ArgumentException("矩阵参数错误");
        Pattern.SetCTM(new ST_Array(matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5]));
    }

    /// <summary>通过仿射矩阵（m00 m10 / m01 m11 / m02 m12）设置底纹单元变换矩阵。</summary>
    public void SetTransform(double scaleX, double shearY, double shearX, double scaleY,
        double translateX, double translateY)
    {
        Pattern.SetCTM(new ST_Array(scaleX, shearY, shearX, scaleY, translateX, translateY));
    }

    /// <summary>设置每个重复单元的图片尺寸。</summary>
    public void SetImageSize(double w, double h)
    {
        Pattern.SetWidth(w);
        Pattern.SetHeight(h);
        ImgObj.SetBoundary(new ST_Box(0, 0, w, h));
        ImgObj.SetCTM(new ST_Array(w, 0, 0, h, 0, 0));
    }
}
