using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Reader;

/// <summary>DeltaX/DeltaY 工具（Java DeltaTool）：展开 "g" 压缩语法并按文本长度补齐末尾值。</summary>
public static class DeltaTool
{
    /// <summary>
    /// 获取 Delta 数据：支持 <c>n g count value</c> 压缩语法；
    /// 数量不足时以最后一个值补齐到 <paramref name="contentLength"/>。
    /// </summary>
    public static List<float> GetDelta(ST_Array? delta, int contentLength)
    {
        var list = new List<float>();
        if (delta != null)
        {
            var array = delta.GetArray();
            for (int i = 0, len = array.Count; i < len; i++)
            {
                if ("g".Equals(array[i], StringComparison.Ordinal))
                {
                    int repeat = int.Parse(array[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture);
                    float value = float.Parse(array[i + 2], NumberStyles.Float, CultureInfo.InvariantCulture);
                    for (int j = 0; j < repeat; j++) list.Add(value);
                    i += 2;
                }
                else
                {
                    list.Add(float.Parse(array[i], NumberStyles.Float, CultureInfo.InvariantCulture));
                }
            }
        }

        int deltaSize = list.Count;
        if (deltaSize < contentLength && deltaSize > 0)
        {
            float lastDelta = list[^1];
            for (int i = 0; i < contentLength - deltaSize; i++)
                list.Add(lastDelta);
        }
        return list;
    }
}
