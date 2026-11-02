namespace Ofd.Layout.Element;

/// <summary>数组参数工具（Java ArrayParamTool）。</summary>
public static class ArrayParamTool
{
    /// <summary>
    /// 四参数归一化：
    /// 空 → {0,0,0,0}；1 个 → 全同；2 个 → {a,b,a,b}；3 个 → {a,b,c,0}；4 个及以上 → 前四个。
    /// </summary>
    public static double[] Arr4p(params double[]? arr)
    {
        if (arr == null || arr.Length == 0) return new[] { 0d, 0d, 0d, 0d };

        return arr.Length switch
        {
            1 => new[] { arr[0], arr[0], arr[0], arr[0] },
            2 => new[] { arr[0], arr[1], arr[0], arr[1] },
            3 => new[] { arr[0], arr[1], arr[2], 0d },
            _ => new[] { arr[0], arr[1], arr[2], arr[3] },
        };
    }
}
