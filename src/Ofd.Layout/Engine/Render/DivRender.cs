using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.PageDescription.Color.Color;
using Ofd.Layout.Element;

namespace Ofd.Layout.Engine.Render;

/// <summary>
/// 盒式模型渲染器（Java DivRender）：绘制背景与边框（等宽单框 / 不等宽四边）。
/// </summary>
public class DivRender : IProcessor
{
    public void Render(ST_Loc pageLoc, CT_PageBlock layer, ResManager resManager, Div e, AtomicCounter maxUnitId) =>
        Render(layer, e, maxUnitId);

    /// <summary>渲染 Div 到指定图层。</summary>
    public static void Render(CT_PageBlock layer, Div e, AtomicCounter maxUnitId)
    {
        int[]? bgColor = e.GetBackgroundColor();
        if (bgColor == null && e.IsNoBorder())
        {
            // 无背景无色边框 → 无需绘制
            return;
        }

        // 图元透明度
        int? alpha = e.GetOpacity() is { } opacity ? (int)(opacity * 255) : null;

        if (e.GetHeight() == null) throw new ArgumentException("Div元素的高度必须指定");

        double borderTop = e.GetBorderTop();
        double borderRight = e.GetBorderRight();
        double borderBottom = e.GetBorderBottom();
        double borderLeft = e.GetBorderLeft();

        double eleContentHeight = e.GetPaddingTop() + e.GetHeight()!.Value + e.GetPaddingBottom();

        // 1) 背景
        if (bgColor != null && eleContentHeight > 0)
        {
            var bg = new PathObject(new ST_ID(maxUnitId.IncrementAndGet()));
            double x = (e.GetX() ?? 0) + e.GetMarginLeft() + borderLeft;
            double y = (e.GetY() ?? 0) + e.GetMarginTop() + borderTop;
            double w = e.GetPaddingLeft() + e.GetWidth()!.Value + e.GetPaddingRight();

            bg.SetBoundary(x, y, w, eleContentHeight);
            bg.SetAbbreviatedData(GraphHelper.Rect(0, 0, w, eleContentHeight));
            bg.SetStroke(false);
            bg.SetFill(true);
            bg.SetFillColor(CT_Color.Rgb(bgColor));
            if (alpha != null) bg.SetAlpha(alpha);

            layer.AddPageBlock(bg);
        }

        double[]? borderDash = e.GetBorderDash();

        // 2) 四边等宽 → 一个矩形路径
        if (Eq(borderTop, borderRight, borderBottom, borderLeft))
        {
            double lineWidth = borderTop;
            var border = new PathObject(new ST_ID(maxUnitId.IncrementAndGet()));

            double x = (e.GetX() ?? 0) + e.GetMarginLeft();
            double y = (e.GetY() ?? 0) + e.GetMarginTop();
            double w = lineWidth + e.GetPaddingLeft() + e.GetWidth()!.Value + e.GetPaddingRight() + lineWidth;
            double h = lineWidth + e.GetPaddingTop() + e.GetHeight()!.Value + e.GetPaddingBottom() + lineWidth;

            border.SetBoundary(x, y, w, h);
            border.SetLineWidth(lineWidth);
            border.SetAbbreviatedData(GraphHelper.Rect(lineWidth / 2, lineWidth / 2, w - lineWidth, h - lineWidth));

            int[]? borderColor = e.GetBorderColor();
            if (borderColor != null) border.SetStrokeColor(CT_Color.Rgb(borderColor));
            if (alpha != null) border.SetAlpha(alpha);
            if (borderDash != null) SetLineDash(border, borderDash);

            layer.AddPageBlock(border);
        }
        else
        {
            // 3) 四边不等宽 → 逐边绘制
            double topWidth = borderTop;
            const double ZERO = 0.00001;

            if (topWidth > ZERO)
            {
                var topBorder = new PathObject(new ST_ID(maxUnitId.IncrementAndGet()));
                double x = (e.GetX() ?? 0) + e.GetMarginLeft();
                double y = (e.GetY() ?? 0) + e.GetMarginTop();
                double w = borderLeft + e.GetPaddingLeft() + e.GetWidth()!.Value + e.GetPaddingRight() + borderRight;

                topBorder.SetBoundary(x, y, w, topWidth);
                topBorder.SetLineWidth(topWidth);
                topBorder.SetAbbreviatedData(new AbbreviatedData().M(0, topWidth / 2).L(w, topWidth / 2));

                int[]? borderColor = e.GetBorderColor();
                if (borderColor != null) topBorder.SetStrokeColor(CT_Color.Rgb(borderColor));
                if (alpha != null) topBorder.SetAlpha(alpha);
                if (borderDash != null) SetLineDash(topBorder, borderDash);

                layer.AddPageBlock(topBorder);
            }

            double bottomWidth = borderBottom;
            if (bottomWidth > ZERO)
            {
                var bottomBorder = new PathObject(new ST_ID(maxUnitId.IncrementAndGet()));
                double x = (e.GetX() ?? 0) + e.GetMarginLeft();
                double y = (e.GetY() ?? 0) + e.GetMarginTop() + borderTop + e.GetPaddingTop()
                           + e.GetHeight()!.Value + e.GetPaddingBottom();
                double w = borderLeft + e.GetPaddingLeft() + e.GetWidth()!.Value + e.GetPaddingRight() + borderRight;

                bottomBorder.SetBoundary(x, y, w, bottomWidth);
                bottomBorder.SetLineWidth(bottomWidth);
                bottomBorder.SetAbbreviatedData(new AbbreviatedData().M(0, bottomWidth / 2).L(w, bottomWidth / 2));

                int[]? borderColor = e.GetBorderColor();
                if (borderColor != null) bottomBorder.SetStrokeColor(CT_Color.Rgb(borderColor));
                if (alpha != null) bottomBorder.SetAlpha(alpha);
                if (borderDash != null) SetLineDash(bottomBorder, borderDash);

                layer.AddPageBlock(bottomBorder);
            }

            // 无内容也无上下边框 → 视为占位符，跳过
            if (topWidth + bottomWidth + eleContentHeight == 0) return;

            double leftWidth = borderLeft;
            if (leftWidth > ZERO)
            {
                var leftBorder = new PathObject(new ST_ID(maxUnitId.IncrementAndGet()));
                double x = (e.GetX() ?? 0) + e.GetMarginLeft();
                double y = (e.GetY() ?? 0) + e.GetMarginTop() + topWidth;
                double h = borderTop + e.GetPaddingTop() + e.GetHeight()!.Value + e.GetPaddingBottom()
                           + borderBottom - topWidth - bottomWidth;

                leftBorder.SetBoundary(x, y, leftWidth, h);
                leftBorder.SetLineWidth(leftWidth);
                leftBorder.SetAbbreviatedData(new AbbreviatedData().M(leftWidth / 2, 0).L(leftWidth / 2, h));

                int[]? borderColor = e.GetBorderColor();
                if (borderColor != null) leftBorder.SetStrokeColor(CT_Color.Rgb(borderColor));
                if (alpha != null) leftBorder.SetAlpha(alpha);
                if (borderDash != null) SetLineDash(leftBorder, borderDash);

                layer.AddPageBlock(leftBorder);
            }

            double rightWidth = borderRight;
            if (rightWidth > ZERO)
            {
                var rightBorder = new PathObject(new ST_ID(maxUnitId.IncrementAndGet()));
                double x = (e.GetX() ?? 0) + e.GetMarginLeft() + borderLeft + e.GetPaddingLeft()
                           + e.GetWidth()!.Value + e.GetPaddingRight();
                double y = (e.GetY() ?? 0) + e.GetMarginTop() + topWidth;
                double h = borderTop + e.GetPaddingTop() + e.GetHeight()!.Value + e.GetPaddingBottom()
                           + borderBottom - topWidth - bottomWidth;

                rightBorder.SetBoundary(x, y, rightWidth, h);
                rightBorder.SetLineWidth(rightWidth);
                rightBorder.SetAbbreviatedData(new AbbreviatedData().M(rightWidth / 2, 0).L(rightWidth / 2, h));

                int[]? borderColor = e.GetBorderColor();
                if (borderColor != null) rightBorder.SetStrokeColor(CT_Color.Rgb(borderColor));
                if (alpha != null) rightBorder.SetAlpha(alpha);
                if (borderDash != null) SetLineDash(rightBorder, borderDash);

                layer.AddPageBlock(rightBorder);
            }
        }
    }

    /// <summary>设置虚线样式：[偏移量, 虚线长, 空白长, ...]。</summary>
    private static void SetLineDash(CT_Path g, double[]? dash)
    {
        if (dash == null || dash.Length == 0) return;

        if (dash.Length == 1) dash = new[] { 0.0, dash[0], dash[0] };

        // 偏移量（忽略 double 精度误差）
        if (dash[0] >= 0.000001) g.SetDashOffset(dash[0]);

        var dashPattern = new ST_Array();
        for (int i = 1; i < dash.Length; i++)
            dashPattern.Add(STBase.Fmt(dash[i]));

        g.SetDashPattern(dashPattern);
    }

    /// <summary>忽略精度比较一组 double 是否相等。</summary>
    public static bool Eq(params double[]? arr)
    {
        if (arr == null || arr.Length == 0) return true;

        string? last = null;
        foreach (double v in arr)
        {
            string fmt = STBase.Fmt(v);
            if (last == null)
            {
                last = fmt;
                continue;
            }
            if (last != fmt) return false;
        }
        return true;
    }
}
