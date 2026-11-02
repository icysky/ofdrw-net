namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 字体测量工具（Java TextMeasureTool）：按阅读方向/字符方向计算字符偏移与文本占位。
/// </summary>
public static class TextMeasureTool
{
    /// <summary>
    /// 分析字间距偏移量并计算文字宽度（Java 方法名 measureWithWith）。
    /// </summary>
    public static MeasureBody MeasureWidth(string text, FontSetting fontSetting)
    {
        var body = new MeasureBody();
        if (text.Length == 0) return body;

        int readDirection = fontSetting.GetReadDirection();
        int charDirection = fontSetting.GetCharDirection();

        char firstChar = text[0];
        char lastChar = text[^1];
        double fontSize = fontSetting.GetFontSize() ?? 0;

        if (readDirection == 0)
        {
            if (charDirection == 0)
            {
                body.Offset = Offset(text, fontSetting, 0, "W", 1);
                body.With(fontSetting.CharWidth(lastChar));
            }
            else if (charDirection == 180)
            {
                body.Offset = Offset(text, fontSetting, 1, "W", 1);
                body.With(fontSetting.CharWidth(firstChar));
                body.FirstCharOffsetX = fontSetting.CharWidth(firstChar);
                body.FirstCharOffsetY = -fontSize;
            }
            else // 90 / 270
            {
                body.Offset = Offset(text, fontSetting, 0, "H", 1);
                body.With(fontSize);
                if (charDirection == 90) body.FirstCharOffsetY = -fontSize;
                else body.FirstCharOffsetX = fontSize;
            }
        }
        else if (readDirection == 180)
        {
            if (charDirection == 0)
            {
                body.Offset = Offset(text, fontSetting, 1, "W", -1);
                body.With(fontSetting.CharWidth(firstChar));
                body.FirstCharOffsetX = -fontSetting.CharWidth(firstChar);
            }
            else if (charDirection == 180)
            {
                body.Offset = Offset(text, fontSetting, 0, "W", -1);
                body.With(fontSetting.CharWidth(lastChar));
                body.FirstCharOffsetY = -fontSize;
            }
            else // 90 / 270
            {
                body.Offset = Offset(text, fontSetting, 0, "H", -1);
                body.With(fontSize);
                if (charDirection == 90)
                {
                    body.FirstCharOffsetX = -fontSize;
                    body.FirstCharOffsetY = -fontSize;
                }
            }
        }
        else if (readDirection == 90)
        {
            if (charDirection is 0 or 180)
            {
                body.Offset = Offset(text, fontSetting, 0, "H", 1);
                body.With(fontSize);
                if (charDirection == 0) body.FirstCharOffsetY = fontSize;
                else body.FirstCharOffsetX = fontSize;
            }
            else if (charDirection == 90)
            {
                body.Offset = Offset(text, fontSetting, 0, "W", 1);
                body.With(fontSetting.CharWidth(lastChar));
            }
            else // 270
            {
                body.Offset = Offset(text, fontSetting, 1, "W", 1);
                body.With(fontSetting.CharWidth(firstChar));
                body.FirstCharOffsetX = fontSize;
                body.FirstCharOffsetY = fontSetting.CharWidth(firstChar);
            }
        }
        else if (readDirection == 270)
        {
            if (charDirection is 0 or 180)
            {
                body.Offset = Offset(text, fontSetting, 0, "H", -1);
                body.With(fontSize);
                if (charDirection == 180)
                {
                    body.FirstCharOffsetX = fontSize;
                    body.FirstCharOffsetY = -fontSize;
                }
            }
            else if (charDirection == 90)
            {
                body.Offset = Offset(text, fontSetting, 1, "W", -1);
                body.With(fontSetting.CharWidth(firstChar));
                body.FirstCharOffsetY = -fontSetting.CharWidth(firstChar);
            }
            else // 270
            {
                body.Offset = Offset(text, fontSetting, 0, "W", -1);
                body.With(fontSetting.CharWidth(lastChar));
                body.FirstCharOffsetX = fontSize;
            }
        }

        return body;
    }

    /// <summary>测量文本各字符偏移量数组。</summary>
    public static double[] Measure(string text, FontSetting fontSetting) =>
        MeasureWidth(text, fontSetting).Offset;

    /// <summary>计算文本所占空间大小。</summary>
    public static TextMetricsArea MeasureArea(string text, FontSetting fontSetting)
    {
        var area = new TextMetricsArea
        {
            CharAreas = new List<Ofd.Font.TextBounds>(text.Length),
            Width = 0d,
            Height = 0d,
        };

        int readDirection = fontSetting.GetReadDirection();
        int charDirection = fontSetting.GetCharDirection();

        double width = 0;
        double height = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            var box = fontSetting.Box(c);

            if (readDirection is 0 or 180)
            {
                if (charDirection is 0 or 180)
                {
                    if (i != 0) width += fontSetting.GetLetterSpacing() ?? 0;
                    width += box.Width;
                    height = Math.Max(height, box.Height);
                    area.CharAreas.Add(new Ofd.Font.TextBounds(box.Width, box.Height));
                }
                else if (charDirection is 90 or 270)
                {
                    if (i != 0) width += fontSetting.GetLetterSpacing() ?? 0;
                    width += box.Height;
                    height = Math.Max(height, box.Width);
                    area.CharAreas.Add(new Ofd.Font.TextBounds(box.Height, box.Width));
                }
            }
            else if (readDirection is 90 or 270)
            {
                if (charDirection is 0 or 180)
                {
                    if (i != 0) height += fontSetting.GetLetterSpacing() ?? 0;
                    height += box.Height;
                    width = Math.Max(width, box.Width);
                    area.CharAreas.Add(new Ofd.Font.TextBounds(box.Width, box.Height));
                }
                else if (charDirection is 90 or 270)
                {
                    if (i != 0) height += fontSetting.GetLetterSpacing() ?? 0;
                    height += box.Width;
                    width = Math.Max(width, box.Height);
                    area.CharAreas.Add(new Ofd.Font.TextBounds(box.Height, box.Width));
                }
            }
        }

        area.Height = height;
        area.Width = width;
        area.LetterSpacing = fontSetting.GetLetterSpacing() ?? 0;
        return area;
    }

    /// <summary>
    /// 计算字符偏移量。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="fontSetting">字体设置</param>
    /// <param name="indexOffset">相对偏移（0 或 1）</param>
    /// <param name="wOrH">以宽度 W 或高度 H 作为偏移</param>
    /// <param name="direction">排列方向：1 正序，-1 逆序</param>
    private static double[] Offset(string text, FontSetting fontSetting, int indexOffset, string wOrH, int direction)
    {
        if (text.Length == 1) return Array.Empty<double>();

        double fontSize = fontSetting.GetFontSize() ?? 0;
        double letterSpacing = fontSetting.GetLetterSpacing() ?? 0;
        var offsetArray = new double[text.Length - 1];

        for (int i = indexOffset, len = indexOffset + text.Length - 1; i < len; i++)
        {
            int index = indexOffset == 0 ? i : i - 1;

            offsetArray[index] = "w".Equals(wOrH, StringComparison.OrdinalIgnoreCase)
                ? fontSetting.CharWidth(text[i]) + letterSpacing
                : fontSize + letterSpacing;

            // 阅读方向 180 / 270 → 反向
            offsetArray[index] = direction * offsetArray[index];
        }
        return offsetArray;
    }
}
