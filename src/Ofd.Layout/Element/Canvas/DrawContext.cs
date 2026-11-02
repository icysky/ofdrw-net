using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.PageDescription;
using Ofd.Core.PageDescription.Clips;
using Ofd.Core.PageDescription.Color.Color;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Core.Text;
using Ofd.Core.Text.Font;
using Ofd.Core.Text.Text;
using Ofd.Layout.Engine;
using SkiaSharp;
using FontType = Ofd.Font.Font;
using PathArea = Ofd.Core.PageDescription.Clips.Area;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 绘制器绘制上下文（Java DrawContext）：提供 Canvas 风格绘制 API，产出 OFD 图元。
/// <para>一条路径只允许一种描边和填充颜色，重复设置取最后一次。</para>
/// </summary>
public class DrawContext : IDisposable
{
    internal static readonly ST_Array ONE = ST_Array.UnitCTM();

    /// <summary>容纳所有绘制结果的容器。</summary>
    private readonly CT_PageBlock _container;

    /// <summary>对象 ID 提供器。</summary>
    private readonly AtomicCounter _maxUnitId;

    /// <summary>资源管理器。</summary>
    private readonly ResManager _resManager;

    /// <summary>画布位置与大小。</summary>
    private readonly ST_Box _boundary;

    /// <summary>画布状态。</summary>
    private CanvasState _state;

    /// <summary>状态栈（save/restore）。</summary>
    private readonly LinkedList<CanvasState> _stack;

    /// <summary>
    /// 填充颜色：支持 string（#000000 / rgb() / rgba()）、CT_Color、IColorClusterType、
    /// CanvasPattern、CanvasGradient、CanvasRadialGradient。
    /// </summary>
    /// <summary>是否启用光学字距（ASCII 按字形墨迹 + 固定间隙推进）。</summary>
    public bool OpticalKerning { get; set; }

    public object? FillStyle;

    /// <summary>描边颜色（同上）。</summary>
    public object? StrokeStyle;

    /// <summary>字体描述（CSS3 格式：[style] [weight] size family）。</summary>
    public string? Font;

    /// <summary>每毫米像素数（pixel per millimeter）。</summary>
    public double PPM;

    public DrawContext(CT_PageBlock container, ST_Box boundary, AtomicCounter maxUnitId, ResManager resManager)
    {
        _container = container;
        _boundary = boundary;
        _maxUnitId = maxUnitId;
        _resManager = resManager;
        _state = new CanvasState();
        _stack = new LinkedList<CanvasState>();
        PPM = 3.78;

        // 默认黑色
        FillStyle = "#000000";
        StrokeStyle = "#000000";
    }

    /// <summary>开启新路径（清除已存在路径）。</summary>
    public DrawContext BeginPath()
    {
        _state.Path = new AbbreviatedData();
        return this;
    }

    /// <summary>关闭路径。</summary>
    public DrawContext ClosePath()
    {
        _state.Path?.ClosePath();
        return this;
    }

    /// <summary>以当前路径作为裁剪区（受 CTM 影响）。</summary>
    public DrawContext Clip()
    {
        if (_state.Path == null) return this;

        _state.ClipArea = _state.Path.Clone();
        if (_state.Ctm != null && !OneEquals(_state.Ctm))
            Transform(_state.ClipArea, _state.Ctm);

        return this;
    }

    public DrawContext MoveTo(double x, double y)
    {
        _state.Path ??= new AbbreviatedData();
        _state.Path.M(x, y);
        return this;
    }

    public DrawContext LineTo(double x, double y)
    {
        _state.Path?.L(x, y);
        return this;
    }

    public DrawContext QuadraticCurveTo(double cpx, double cpy, double x, double y)
    {
        _state.Path ??= new AbbreviatedData();
        _state.Path.Q(cpx, cpy, x, y);
        return this;
    }

    public DrawContext BezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y)
    {
        _state.Path ??= new AbbreviatedData();
        _state.Path.B(cp1x, cp1y, cp2x, cp2y, x, y);
        return this;
    }

    /// <summary>椭圆弧（large/sweep 为 true 表示 1）。</summary>
    public DrawContext Arc(double a, double b, double angle, bool large, bool sweep, double x, double y)
    {
        _state.Path ??= new AbbreviatedData();
        _state.Path.Arc(a, b, angle % 360, large ? 1 : 0, sweep ? 1 : 0, x, y);
        return this;
    }

    /// <summary>圆心圆弧（角度制，默认逆时针参数为 true 表示逆时针）。</summary>
    public DrawContext Arc(double x, double y, double r, double sAngle, double eAngle, bool counterclockwise)
    {
        _state.Path ??= new AbbreviatedData();

        double x1 = x + r * Math.Cos(sAngle * Math.PI / 180);
        double y1 = y + r * Math.Sin(sAngle * Math.PI / 180);
        MoveTo(x1, y1);

        double angle = eAngle - sAngle;
        if (angle == 360)
        {
            // 整圆分两段
            _state.Path.Arc(r, r, angle, 1, counterclockwise ? 1 : 0, x - r, y)
                 .Arc(r, r, angle, 1, counterclockwise ? 1 : 0, x1, y1);
        }
        else
        {
            double x2 = x + r * Math.Cos(eAngle * Math.PI / 180);
            double y2 = y + r * Math.Sin(eAngle * Math.PI / 180);
            _state.Path.Arc(r, r, angle, angle > 180 ? 1 : 0, counterclockwise ? 1 : 0, x2, y2);
        }

        return this;
    }

    /// <summary>圆心圆弧（默认逆时针）。</summary>
    public DrawContext Arc(double x, double y, double r, double sAngle, double eAngle) =>
        Arc(x, y, r, sAngle, eAngle, true);

    /// <summary>追加矩形路径。</summary>
    public DrawContext Rect(double x, double y, double width, double height)
    {
        _state.Path ??= new AbbreviatedData();
        _state.Path.M(x, y).L(x + width, y).L(x + width, y + height).L(x, y + height).C();
        return this;
    }

    /// <summary>创建并填充矩形（不影响上下文路径）。</summary>
    public DrawContext FillRect(double x, double y, double width, double height)
    {
        var data = new AbbreviatedData().M(x, y).L(x + width, y).L(x + width, y + height).L(x, y + height).C();

        var path = new PathObject(new ST_ID(_maxUnitId.IncrementAndGet()));
        path.SetAbbreviatedData(data);
        path.SetFill(true);
        ApplyDrawParam(path);
        _container.AddPageBlock(path);
        return this;
    }

    /// <summary>创建并描边矩形（不影响上下文路径）。</summary>
    public DrawContext StrokeRect(double x, double y, double width, double height)
    {
        var data = new AbbreviatedData().M(x, y).L(x + width, y).L(x + width, y + height).L(x, y + height).C();

        var path = new PathObject(new ST_ID(_maxUnitId.IncrementAndGet()));
        path.SetAbbreviatedData(data);
        path.SetStroke(true);
        ApplyDrawParam(path);
        _container.AddPageBlock(path);
        return this;
    }

    /// <summary>描边当前路径。</summary>
    public DrawContext Stroke()
    {
        if (_state.Path == null) return this;

        var path = new PathObject(new ST_ID(_maxUnitId.IncrementAndGet()));
        path.SetAbbreviatedData(_state.Path.Clone());
        path.SetStroke(true);
        ApplyDrawParam(path);
        _container.AddPageBlock(path);
        return this;
    }

    /// <summary>填充当前路径。</summary>
    public DrawContext Fill()
    {
        if (_state.Path == null) return this;

        var path = new PathObject(new ST_ID(_maxUnitId.IncrementAndGet()));
        path.SetAbbreviatedData(_state.Path.Clone());
        path.SetFill(true);
        path.SetLineWidth(0d);
        ApplyDrawParam(path);
        _container.AddPageBlock(path);
        return this;
    }

    /// <summary>缩放当前绘图。</summary>
    public DrawContext Scale(double scaleWidth, double scaleHeight)
    {
        _state.Ctm ??= ST_Array.UnitCTM();
        _state.Ctm = new ST_Array(scaleWidth, 0, 0, scaleHeight, 0, 0).MtxMul(_state.Ctm);
        return this;
    }

    /// <summary>旋转当前绘图（角度）。</summary>
    public DrawContext Rotate(double angle)
    {
        _state.Ctm ??= ST_Array.UnitCTM();
        double alpha = angle * Math.PI / 180d;
        _state.Ctm = new ST_Array(Math.Cos(alpha), Math.Sin(alpha), -Math.Sin(alpha), Math.Cos(alpha), 0, 0)
            .MtxMul(_state.Ctm);
        return this;
    }

    /// <summary>平移绘图原点。</summary>
    public DrawContext Translate(double x, double y)
    {
        _state.Ctm ??= ST_Array.UnitCTM();
        _state.Ctm = new ST_Array(1, 0, 0, 1, x, y).MtxMul(_state.Ctm);
        return this;
    }

    /// <summary>在当前变换基础上叠加矩阵。</summary>
    public DrawContext TransformMatrix(double a, double b, double c, double d, double e, double f)
    {
        _state.Ctm ??= ST_Array.UnitCTM();
        _state.Ctm = new ST_Array(a, b, c, d, e, f).MtxMul(_state.Ctm);
        return this;
    }

    /// <summary>重置并设置变换矩阵。</summary>
    public DrawContext SetTransform(double a, double b, double c, double d, double e, double f)
    {
        _state.Ctm = new ST_Array(a, b, c, d, e, f);
        return this;
    }

    /// <summary>裁剪图片并在 OFD 上绘制（源/目标参数单位：mm，按 PPM 转换像素）。</summary>
    public DrawContext DrawImage(string img, double sx, double sy, double sWidth, double sHeight,
        double dx, double dy, double dWidth, double dHeight)
    {
        if (img == null || !File.Exists(img)) throw new ArgumentException("图像不存在");

        using var source = SKBitmap.Decode(img) ?? throw new IOException("图像读取失败: " + img);

        int w = Pixel(sWidth);
        int h = Pixel(sHeight);

        using var cutOut = new SKBitmap(Math.Max(w, 1), Math.Max(h, 1));
        using (var canvas = new SKCanvas(cutOut))
        {
            canvas.DrawBitmap(source, new SKRect(Pixel(sx), Pixel(sy), Pixel(sx) + w, Pixel(sy) + h),
                new SKRect(0, 0, w, h));
        }

        string tmp = Path.Combine(Path.GetTempPath(), "ofd-cut-" + Guid.NewGuid().ToString("N")[..8] + ".png");
        try
        {
            using (var image = SKImage.FromBitmap(cutOut))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var fs = File.Create(tmp))
                data.SaveTo(fs);

            return DrawImage(tmp, dx, dy, dWidth, dHeight);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    /// <summary>在 OFD 上绘制图像（尺寸按 PPM 由像素换算毫米）。</summary>
    public DrawContext DrawImage(string img, double dx, double dy)
    {
        if (img == null || !File.Exists(img)) throw new ArgumentException("图像不存在");

        using var bmp = SKBitmap.Decode(img) ?? throw new IOException("图像读取失败: " + img);
        return DrawImage(img, dx, dy, Mm(bmp.Width), Mm(bmp.Height));
    }

    /// <summary>在 OFD 上绘制图像（指定目标宽高）。</summary>
    public DrawContext DrawImage(string img, double dx, double dy, double dWidth, double dHeight)
    {
        if (img == null || !File.Exists(img)) throw new IOException("图片(img)不存在");

        var resourceId = _resManager.AddImage(img);

        var imgObj = new ImageObject(_maxUnitId.IncrementAndGet());
        imgObj.SetResourceID(new ST_RefID(resourceId!));
        imgObj.SetBoundary(_boundary.Clone());

        // 应用变换矩阵
        var ctm = _state.Ctm ?? ST_Array.UnitCTM();
        ctm = new ST_Array(dWidth, 0, 0, dHeight, dx, dy).MtxMul(ctm);
        imgObj.SetCTM(ctm);

        ApplyDrawParam(imgObj);
        _container.AddPageBlock(imgObj);
        return this;
    }

    /// <summary>保存当前绘图状态。</summary>
    public DrawContext Save()
    {
        _state.StrokeStyle = StrokeStyle;
        _state.FillStyle = FillStyle;
        _state.FontStyle = Font;
        _stack.AddFirst(_state.Clone());
        return this;
    }

    /// <summary>还原绘图状态。</summary>
    public DrawContext Restore()
    {
        if (_stack.Count == 0) return this;

        _state = _stack.First!.Value;
        _stack.RemoveFirst();
        StrokeStyle = _state.StrokeStyle;
        FillStyle = _state.FillStyle;
        Font = _state.FontStyle;
        return this;
    }

    /// <summary>填充文字（x/y 为阅读方向左下角）。</summary>
    public DrawContext FillText(string text, double x, double y)
    {
        if (string.IsNullOrWhiteSpace(text)) return this;

        // 字体样式字符串 → 字体设置
        var existFont = FontStyleToSetting(Font, _state.Font);

        // 光学字距（仅影响 ASCII）：解决等宽字体中「数字后紧跟字母过挤」
        _state.Font.SetOpticalKerning(OpticalKerning);

        var fontId = existFont != null
            ? existFont.GetID()
            : _resManager.AddFont(_state.Font.GetFont());

        var txtObj = new TextObject(new ST_ID(_maxUnitId.IncrementAndGet()));
        txtObj.SetBoundary(_boundary.Clone());
        txtObj.SetFont(new ST_RefID(fontId!));
        txtObj.SetSize(_state.Font.GetFontSize() ?? 0);

        txtObj.SetFill(true);

        int? weight = _state.Font.GetFontWeight();
        if (weight != null && weight != 400) txtObj.SetWeight(WeightExtensions.GetInstance(weight.Value));
        if (_state.Font.IsItalic()) txtObj.SetItalic(true);

        int readDirection = _state.Font.GetReadDirection();
        int charDirection = _state.Font.GetCharDirection();
        if (readDirection != 0) txtObj.SetReadDirection(DirectionExtensions.GetInstance(readDirection));
        if (charDirection != 0) txtObj.SetCharDirection(DirectionExtensions.GetInstance(charDirection));

        // 测量偏移
        var measureBody = TextMeasureTool.MeasureWidth(text, _state.Font);

        double xx = x + measureBody.FirstCharOffsetX;
        double yy = y + measureBody.FirstCharOffsetY;
        switch (readDirection)
        {
            case 0:
            case 180:
                xx += TextFloatFactor(_state.Font.GetTextAlign(), measureBody.Width, readDirection);
                break;
            case 90:
            case 270:
                yy += TextFloatFactor(_state.Font.GetTextAlign(), measureBody.Width, readDirection);
                break;
        }

        var tc = new TextCode().SetContent(text).SetX(xx).SetY(yy);
        if (readDirection is 90 or 270) tc.SetDeltaY(measureBody.Offset);
        else tc.SetDeltaX(measureBody.Offset);

        txtObj.AddTextCode(tc);

        ApplyDrawParam(txtObj);
        _container.AddPageBlock(txtObj);
        return this;
    }

    /// <summary>文本对齐带来的偏移因子。</summary>
    private static double TextFloatFactor(CanvasTextAlign align, double width, int readDirection)
    {
        double factor = align switch
        {
            CanvasTextAlign.Start or CanvasTextAlign.Left => 0,
            CanvasTextAlign.End or CanvasTextAlign.Right => -width,
            CanvasTextAlign.Center => -width / 2,
            _ => 0,
        };

        if (readDirection is 180 or 270) factor = -factor;
        return factor;
    }

    /// <summary>获取文本对齐方式。</summary>
    public CanvasTextAlign GetTextAlign() => _state.Font.GetTextAlign();

    /// <summary>设置文本对齐方式。</summary>
    public DrawContext SetTextAlign(CanvasTextAlign textAlign)
    {
        _state.Font.SetTextAlign(textAlign);
        return this;
    }

    /// <summary>测量文本宽度与偏移。</summary>
    public TextMetrics MeasureText(string text)
    {
        FontStyleToSetting(Font, _state.Font);

        var metrics = new TextMetrics
        {
            ReadDirection = _state.Font.GetReadDirection(),
            FontSize = _state.Font.GetFontSize(),
        };

        var body = TextMeasureTool.MeasureWidth(text, _state.Font);
        metrics.Width = body.Width;
        metrics.Offset = body.Offset;
        return metrics;
    }

    /// <summary>测量文本所占空间。</summary>
    public TextMetricsArea MeasureTextArea(string text)
    {
        FontStyleToSetting(Font, _state.Font);
        return TextMeasureTool.MeasureArea(text, _state.Font);
    }

    /// <summary>获取描边颜色（非颜色值时返回 null）。</summary>
    public int[]? GetStrokeColor() => StrokeStyle is string s ? NamedColor.Rgb(s) : null;

    /// <summary>设置描边颜色。</summary>
    public DrawContext SetStrokeColor(int[] strokeColor)
    {
        StrokeStyle = $"#{strokeColor[0]:X2}{strokeColor[1]:X2}{strokeColor[2]:X2}";
        return this;
    }

    public DrawContext SetStrokeColor(int r, int g, int b) => SetStrokeColor(new[] { r, g, b });

    /// <summary>获取填充颜色（非颜色值时返回 null）。</summary>
    public int[]? GetFillColor() => FillStyle is string s ? NamedColor.Rgb(s) : null;

    /// <summary>设置填充颜色。</summary>
    public DrawContext SetFillColor(int[] fillColor)
    {
        FillStyle = $"#{fillColor[0]:X2}{fillColor[1]:X2}{fillColor[2]:X2}";
        return this;
    }

    public DrawContext SetFillColor(int r, int g, int b) => SetFillColor(new[] { r, g, b });

    /// <summary>获取线宽（mm）。</summary>
    public double GetLineWidth() => _state.DrawParam.GetLineWidth();

    /// <summary>设置线宽（mm，负数取默认 0.353）。</summary>
    public DrawContext SetLineWidth(double lineWidth)
    {
        if (lineWidth < 0) lineWidth = 0.353;
        _state.DrawParam.SetLineWidth(lineWidth);
        return this;
    }

    /// <summary>获取绘制文字设置。</summary>
    public FontSetting GetFont() => _state.Font;

    /// <summary>设置绘制文字设置。</summary>
    public DrawContext SetFont(FontSetting font)
    {
        _state.Font = font;
        Font = ""; // 清空字体样式字符串
        return this;
    }

    /// <summary>设置默认字体（宋体，指定字号）。</summary>
    public DrawContext SetDefaultFont(double fontSize)
    {
        _state.Font = FontSetting.GetInstance(fontSize);
        return this;
    }

    /// <summary>获取透明度（0.0~1.0）。</summary>
    public double? GetGlobalAlpha() => _state.GlobalAlpha;

    /// <summary>设置透明度（越界钳制，null 视为 1）。</summary>
    public DrawContext SetGlobalAlpha(double? globalAlpha)
    {
        if (globalAlpha == null || globalAlpha > 1) globalAlpha = 1.0;
        else if (globalAlpha < 0) globalAlpha = 0d;

        _state.GlobalAlpha = globalAlpha;
        return this;
    }

    /// <summary>设置端点样式。</summary>
    public DrawContext SetLineCap(LineCapType cap)
    {
        _state.DrawParam.SetCap(cap);
        return this;
    }

    public LineCapType GetLineCap() => _state.DrawParam.GetCap();

    /// <summary>设置线条连接样式。</summary>
    public DrawContext SetLineJoin(LineJoinType join)
    {
        _state.DrawParam.SetJoin(join);
        return this;
    }

    public LineJoinType GetLineJoin() => _state.DrawParam.GetJoin();

    /// <summary>设置最大斜接长度。</summary>
    public DrawContext SetMiterLimit(double? miterLimit)
    {
        _state.DrawParam.SetMiterLimit(miterLimit);
        return this;
    }

    public double? GetMiterLimit() => _state.DrawParam.GetMiterLimit();

    /// <summary>设置虚线样式（两者均为 null 时清除）。</summary>
    public DrawContext SetLineDash(double? dashOffset, params double[]? pattern)
    {
        if (dashOffset == null && pattern == null)
        {
            _state.DrawParam.SetDashPattern(null);
            _state.DrawParam.SetDashOffset(null);
            return this;
        }

        if (pattern == null || pattern.Length < 2)
            throw new ArgumentException("虚线的线段长度和间隔长度(pattern)，不能为空并且需要大于两个以上的值");

        _state.DrawParam.SetDashPattern(new ST_Array(pattern.Select(v => (object?)v).ToArray()));
        _state.DrawParam.SetDashOffset(dashOffset);
        return this;
    }

    /// <summary>设置虚线样式（无偏移）。</summary>
    public DrawContext SetLineDash(params double[] pattern) => SetLineDash(null, pattern);

    public ST_Array? GetDashPattern() => _state.DrawParam.GetDashPattern();

    public double? GetDashOffset() => _state.DrawParam.GetDashOffset();

    /// <summary>创建线性渐变。</summary>
    public CanvasGradient CreateLinearGradient(double x0, double y0, double x1, double y1) =>
        new(x0, y0, x1, y1);

    /// <summary>创建重复底纹。</summary>
    public CanvasPattern CreatePattern(string img, string repetition)
    {
        string abs = Path.GetFullPath(img);
        if (!File.Exists(abs)) throw new ArgumentException("底纹图片不存在：" + abs);

        var resourceId = _resManager.AddImage(abs);

        using var bmp = SKBitmap.Decode(abs) ?? throw new IOException("底纹图片读取失败: " + abs);
        double w = Mm(bmp.Width);
        double h = Mm(bmp.Height);

        var imgObj = new ImageObject(_maxUnitId.IncrementAndGet());
        imgObj.SetResourceID(new ST_RefID(resourceId!));
        imgObj.SetBoundary(new ST_Box(0, 0, w, h));
        imgObj.SetCTM(new ST_Array(w, 0, 0, h, 0, 0));

        return new CanvasPattern(abs, repetition, imgObj);
    }

    /// <summary>创建径向渐变。</summary>
    public CanvasRadialGradient CreateRadialGradient(double x0, double y0, double r0, double x1, double y1, double r1) =>
        new(x0, y0, r0, x1, y1, r1);

    /// <summary>把当前上下文绘制参数应用到图元。</summary>
    private void ApplyDrawParam(IGraphicUnit p)
    {
        // 边界
        p.ApplyBoundary(_boundary.Clone());

        // 透明度
        if (_state.GlobalAlpha != null) p.ApplyAlpha((int)(255 * _state.GlobalAlpha.Value));

        // 变换矩阵（忽略已设置变换矩阵的图元）
        if (_state.Ctm != null && p.GetCtmArray() == null) p.ApplyCtm((ST_Array)_state.Ctm.Clone());

        // 填充/描边颜色
        var fillColor = DetectColor(FillStyle);
        if (fillColor != null) _state.DrawParam.SetFillColor(fillColor);

        var strokeColor = DetectColor(StrokeStyle);
        if (strokeColor != null) _state.DrawParam.SetStrokeColor(strokeColor);

        // 绘制参数
        var paramId = _resManager.AddDrawParam(_state.DrawParam);
        if (paramId != null) p.ApplyDrawParam(new ST_RefID(paramId));

        // 裁剪区
        if (_state.ClipArea != null)
        {
            var clips = new Clips();
            var area = new PathArea();
            var clipObj = new CT_Path().SetAbbreviatedData(_state.ClipArea.Clone());
            clipObj.SetFill(true);
            clipObj.SetBoundary(new ST_Box(0, 0, _boundary.Width, _boundary.Height));

            if (_state.Ctm != null && !OneEquals(_state.Ctm))
            {
                var inverse = Inverse(_state.Ctm);
                if (inverse == null) return; // 不可逆则放弃裁剪区
                clipObj.SetCTM(inverse);
            }

            area.SetClipObj(clipObj);
            clips.AddClip(new CT_Clip().AddArea(area));
            p.ApplyClips(clips);
        }
    }

    /// <summary>计算可逆矩阵（不可逆返回 null）。</summary>
    private static ST_Array? Inverse(ST_Array ctm)
    {
        if (ctm.Size() < 6) return null;

        double a = ctm.Get(0), b = ctm.Get(1), c = ctm.Get(2), d = ctm.Get(3), e = ctm.Get(4), f = ctm.Get(5);
        double det = a * d - b * c;
        if (Math.Abs(det) < 1e-12) return null;

        double ia = d / det;
        double ib = -b / det;
        double ic = -c / det;
        double id = a / det;
        double ie = (c * f - d * e) / det;
        double iff = (b * e - a * f) / det;

        return new ST_Array(ia, ib, ic, id, ie, iff);
    }

    /// <summary>对路径应用变换矩阵（就地修改）。</summary>
    public static void Transform(AbbreviatedData data, ST_Array ctm)
    {
        (double a, double b, double c, double d, double e, double f) = ReadCtm(ctm);

        foreach (var optVal in data.GetRawOptVal())
        {
            switch (optVal.GetOpt())
            {
                case "S":
                case "M":
                case "L":
                {
                    var arr = optVal.GetValues();
                    optVal.SetValues(new[] { a * arr[0] + c * arr[1] + e, b * arr[0] + d * arr[1] + f });
                    continue;
                }
                case "Q":
                {
                    var arr = optVal.GetValues();
                    optVal.SetValues(new[]
                    {
                        a * arr[0] + c * arr[1] + e, b * arr[0] + d * arr[1] + f,
                        a * arr[2] + c * arr[3] + e, b * arr[2] + d * arr[3] + f,
                    });
                    continue;
                }
                case "B":
                {
                    var arr = optVal.GetValues();
                    optVal.SetValues(new[]
                    {
                        a * arr[0] + c * arr[1] + e, b * arr[0] + d * arr[1] + f,
                        a * arr[2] + c * arr[3] + e, b * arr[2] + d * arr[3] + f,
                        a * arr[4] + c * arr[5] + e, b * arr[4] + d * arr[5] + f,
                    });
                    continue;
                }
                case "A":
                {
                    // [0]rx [1]ry [2]angle [3]large [4]sweep [5]x [6]y
                    var arr = optVal.GetValues();
                    double rx = arr[0] * a;
                    double ry = arr[1] * d;
                    double px = a * arr[5] + c * arr[6] + e;
                    double py = b * arr[5] + d * arr[6] + f;
                    optVal.SetValues(new[] { rx, ry, arr[2], arr[3], arr[4], px, py });
                    continue;
                }
                case "C":
                default:
                    continue;
            }
        }
    }

    private static (double, double, double, double, double, double) ReadCtm(ST_Array ctm) =>
        (ctm.Get(0), ctm.Get(1), ctm.Get(2), ctm.Get(3), ctm.Get(4), ctm.Get(5));

    private static bool OneEquals(ST_Array ctm) => ctm.ToString() == ONE.ToString();

    /// <summary>根据颜色类型推断 OFD 颜色对象。</summary>
    private static CT_Color? DetectColor(object? color)
    {
        switch (color)
        {
            case null:
                return null;

            case string s:
            {
                int[]? rgb = NamedColor.Rgb(s);
                if (rgb == null) return null;
                var c = CT_Color.Rgb(rgb[0], rgb[1], rgb[2]);
                if (rgb.Length > 3) c.SetAlpha(rgb[3]);
                return c;
            }

            case IColorClusterType cluster:
            {
                var res = new CT_Color();
                res.SetColor(cluster);
                return res;
            }

            case CT_Color ctColor:
                return ctColor;

            case CanvasGradient gradient:
            {
                var res = new CT_Color();
                res.SetColor(gradient.AxialShd);
                return res;
            }

            case CanvasPattern pattern:
            {
                var res = new CT_Color();
                res.SetColor(pattern.Pattern);
                return res;
            }

            case CanvasRadialGradient radial:
            {
                var res = new CT_Color();
                res.SetColor(radial.RadialShd);
                return res;
            }

            default:
                return null;
        }
    }

    /// <summary>
    /// 解析 CSS 字体字符串为字体设置；文档已存在该字体时返回其 CT_Font。
    /// </summary>
    private CT_Font? FontStyleToSetting(string? fontSettingStr, FontSetting fs)
    {
        if (string.IsNullOrEmpty(fontSettingStr) || fs == null) return null;

        // 解析顺序改为「从左到右」：字体族允许包含空格（如 Noto Serif SC）。
        // 语法：[italic] [weight] <size>(mm|px) <family...>  其中 family 可带引号。
        // 注：原实现从右取 token，导致 "4mm Noto Serif SC" 被拆成 family="SC"、字号解析失败（回退 1）。
        var tokens = Tokenize(fontSettingStr);
        if (tokens.Count == 0) return null;

        int cursor = 0;
        bool italic = false;
        int fontWeight = 400;
        double? fontSize = null;

        while (cursor < tokens.Count)
        {
            string token = tokens[cursor];
            string lower = token.ToLowerInvariant();

            if (lower is "italic" or "oblique")
            {
                italic = true;
                cursor++;
                continue;
            }

            if (TryParseWeight(lower, out int weight))
            {
                fontWeight = weight;
                cursor++;
                continue;
            }

            if (TryParseSize(token, out double size))
            {
                fontSize = size;
                cursor++;
                continue;
            }

            break; // 其余视为字体族
        }

        if (cursor >= tokens.Count) return null;

        string fontFamily = string.Join(" ", tokens.Skip(cursor)).Trim();
        if (fontFamily.Length >= 2 && fontFamily[0] == '"' && fontFamily[^1] == '"')
            fontFamily = fontFamily[1..^1];
        if (fontFamily.Length == 0) return null;

        var nameFont = new FontType(fontFamily, fontFamily);

        CT_Font? ctFont = null;
        var existCtFont = _resManager.GetFont(fontFamily);
        if (existCtFont != null)
        {
            ctFont = existCtFont.Font;
            nameFont = existCtFont.AbsPath != null
                ? new FontType(ctFont.GetFontName() ?? fontFamily, ctFont.GetFamilyName(), existCtFont.AbsPath)
                : new FontType(ctFont.GetFontName() ?? fontFamily, ctFont.GetFamilyName());
        }

        fs.SetFont(nameFont);
        fs.SetFontSize(fontSize ?? 1);
        fs.SetFontWeight(fontWeight);
        fs.SetItalic(italic);

        return ctFont;
    }

    /// <summary>按空格切分，但保留双引号内的整体（用于带空格的字体名）。</summary>
    private static List<string> Tokenize(string text)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool quoted = false;

        foreach (char c in text.Trim())
        {
            if (c == '"') { quoted = !quoted; current.Append(c); continue; }
            if (char.IsWhiteSpace(c) && !quoted)
            {
                if (current.Length > 0) { result.Add(current.ToString()); current.Clear(); }
                continue;
            }
            current.Append(c);
        }

        if (current.Length > 0) result.Add(current.ToString());
        return result;
    }

    /// <summary>识别 CSS 字号：支持 mm / px / 无单位。</summary>
    private bool TryParseSize(string token, out double size)
    {
        size = 0;
        double value;

        if (token.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
        {
            if (!double.TryParse(token[..^2], out value)) return false;
            size = value;
            return true;
        }

        if (token.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            if (!double.TryParse(token[..^2], out value)) return false;
            size = value / PPM;
            return true;
        }

        if (!double.TryParse(token, out value)) return false;
        size = value;
        return true;
    }

    /// <summary>识别字重关键字。</summary>
    private static bool TryParseWeight(string token, out int weight)
    {
        switch (token)
        {
            case "lighter": weight = 100; return true;
            case "100": weight = 100; return true;
            case "200": weight = 200; return true;
            case "300": weight = 300; return true;
            case "normal":
            case "400": weight = 400; return true;
            case "500": weight = 500; return true;
            case "600": weight = 600; return true;
            case "bold":
            case "700": weight = 700; return true;
            case "800": weight = 800; return true;
            case "bolder":
            case "900": weight = 900; return true;
            default: weight = 400; return false;
        }
    }

    /// <summary>像素 → 毫米。</summary>
    public double Mm(int pixel) => pixel / PPM;

    /// <summary>毫米 → 像素。</summary>
    public int Pixel(double mm) => (int)(mm * PPM);

    /// <summary>加入字体资源。</summary>
    public DrawContext AddFont(string name, string fontPath)
    {
        _resManager.AddFont(FontType.FromFile(name, fontPath));
        return this;
    }

    public void Close()
    {
    }

    public void Dispose() => Close();
}
