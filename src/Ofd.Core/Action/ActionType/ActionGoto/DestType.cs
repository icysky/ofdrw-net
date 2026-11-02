namespace Ofd.Core.Action.ActionType.ActionGoto;

/// <summary>目标区域描述方法（表 54 目标区域属性）。</summary>
public enum DestType
{
    /// <summary>由左上角位置（Left，Top）及缩放比例（Zoom）确定</summary>
    XYZ,

    /// <summary>适合整个窗口区域</summary>
    Fit,

    /// <summary>适合窗口宽度，目标区域由 Top 确定</summary>
    FitH,

    /// <summary>适合窗口高度，目标区域由 Left 确定</summary>
    FitV,

    /// <summary>适合窗口内的目标区域（Left，Top，Right，Bottom 矩形）</summary>
    FitR,
}

public static class DestTypeExtensions
{
    /// <summary>获取目标区域类型，未知类型抛异常（与 Java 一致）。</summary>
    public static DestType GetInstance(string? type)
    {
        type = (type ?? "").Trim();
        if (type.Equals("XYZ", StringComparison.OrdinalIgnoreCase)) return DestType.XYZ;
        if (type.Equals("Fit", StringComparison.OrdinalIgnoreCase)) return DestType.Fit;
        if (type.Equals("FitH", StringComparison.OrdinalIgnoreCase)) return DestType.FitH;
        if (type.Equals("FitV", StringComparison.OrdinalIgnoreCase)) return DestType.FitV;
        if (type.Equals("FitR", StringComparison.OrdinalIgnoreCase)) return DestType.FitR;
        throw new ArgumentException("未知目标区域类型：" + type);
    }
}
