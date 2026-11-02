using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.PageDescription.Color.Color;
using Ofd.Core.Text;
using Ofd.Core.Text.Text;
using Ofd.Layout.Element;

namespace Ofd.Layout.Engine.Render;

/// <summary>
/// 段落渲染器（Java ParagraphRender）：逐行输出文字对象（含对齐、占位符偏移、下划线）。
/// </summary>
public class ParagraphRender : IProcessor
{
    public void Render(ST_Loc pageLoc, CT_PageBlock layer, ResManager resManager, Div e, AtomicCounter maxUnitId)
    {
        if (e is Paragraph paragraph)
            Render(layer, resManager, paragraph, maxUnitId);
    }

    /// <summary>把段落渲染到图层。</summary>
    public static void Render(CT_PageBlock layer, ResManager resManager, Paragraph? e, AtomicCounter maxUnitId)
    {
        if (e == null) return;

        var lines = e.GetLines();
        if (lines == null || lines.Count == 0)
        {
            // 直接加入虚拟页面的段落已预处理；此处再尝试一次
            e.DoPrepare((e.GetWidth() ?? 0) + e.WidthPlus());
            lines = e.GetLines();
            if (lines == null || lines.Count == 0) return; // 空段落
        }

        double? containerHeight = e.GetHeight();

        // 行起点
        double lineTopX = (e.GetX() ?? 0) + e.GetMarginLeft() + e.GetBorderLeft() + e.GetPaddingLeft();
        double offsetY = (e.GetY() ?? 0) + e.GetMarginTop() + e.GetBorderTop() + e.GetPaddingTop();
        double hCount = 0;

        foreach (var txtLine in lines)
        {
            // 行的 X 起点（按对齐方式）
            double offsetX = txtLine.GetTextAlign() switch
            {
                TextAlign.Right => lineTopX + (txtLine.GetLineMaxAvailableWidth() - txtLine.GetWidth()),
                TextAlign.Center => lineTopX + (txtLine.GetLineMaxAvailableWidth() - txtLine.GetWidth()) / 2,
                _ => lineTopX,
            };

            double h = txtLine.GetHeight();
            if (containerHeight != null && hCount + h > containerHeight)
            {
                // 超出容器高度 → 舍弃后续内容
                break;
            }

            foreach (var s in txtLine.GetInlineSpans())
            {
                double w = s.BlockSize().GetWidth();
                if (w == 0) continue; // 空行

                if (s is PlaceholderSpan)
                {
                    // 占位符只推进坐标
                    offsetX += w;
                    continue;
                }

                // 字体加入资源
                ST_ID id;
                try
                {
                    id = resManager.AddFont(s.GetFont())!;
                }
                catch (IOException ex)
                {
                    throw new RenderException("渲染异常，字体复制失败：" + ex.Message, ex);
                }

                var txtObj = new TextObject(maxUnitId.IncrementAndGet());
                var boundary = new ST_Box(offsetX, offsetY, w, h);
                txtObj.SetBoundary(boundary);
                txtObj.SetFont(new ST_RefID(id));
                txtObj.SetSize(s.GetFontSize() ?? 0);

                // 粗细
                if (s.GetWeight() != null) txtObj.SetWeight(s.GetWeight());
                else if (s.IsBold()) txtObj.SetWeight(Weight.W_800);

                if (s.IsItalic()) txtObj.SetItalic(true);
                if (s.IsFill() == false) txtObj.SetFill(false);

                // 字体颜色
                int[]? color = s.GetColor();
                if (color != null && color.Length >= 3) txtObj.SetFillColor(CT_Color.Rgb(color));

                // 文字定位：Y 为该行最高文字高度（左下角基线）
                double offset = txtLine.GetMaxSpanHeight();
                var tc = new TextCode()
                    .SetCoordinate(0d, offset)
                    .SetContent(s.GetText() ?? "");

                double[] deltaX = s.GetDeltaX();
                if (deltaX.Length > 0) tc.SetDeltaX(deltaX);

                txtObj.AddTextCode(tc);

                if (e.GetOpacity() is { } opacity) txtObj.SetAlpha((int)(opacity * 255));

                layer.AddPageBlock(txtObj);

                // 下划线
                if (s.IsUnderline())
                {
                    double underlineOffset = offset + s.GetUnderlineOffset();
                    double underlineWidth = s.GetUnderlineWidth();
                    if (underlineWidth == 0) underlineWidth = (s.GetFontSize() ?? 0) * 0.05; // 经验比例

                    var underline = DrawUnderline(new ST_ID(maxUnitId.IncrementAndGet()), boundary, color,
                        underlineOffset, underlineWidth);

                    if (e.GetOpacity() is { } op2) underline.SetAlpha((int)(op2 * 255));

                    layer.AddPageBlock(underline);
                }

                offsetX += w;
            }

            offsetY += h;
            hCount += h;
        }
    }

    /// <summary>绘制下划线路径。</summary>
    private static PathObject DrawUnderline(ST_ID id, ST_Box boundary, int[]? color, double offset, double width)
    {
        var res = new PathObject(id);
        if (color != null && color.Length >= 3) res.SetStrokeColor(CT_Color.Rgb(color));

        res.SetBoundary(boundary);
        res.SetAbbreviatedData(new AbbreviatedData().M(0, offset).L(boundary.Width, offset));
        res.SetLineWidth(width);
        return res;
    }
}
