namespace Ofd.Core.Action;

/// <summary>事件类型（表 52）。</summary>
public enum EventType
{
    /// <summary>文档打开</summary>
    DO,

    /// <summary>页面打开</summary>
    PO,

    /// <summary>单击区域</summary>
    CLICK,
}

public static class EventTypeExtensions
{
    /// <summary>获取事件类型，未知类型抛异常（与 Java 一致）。</summary>
    public static EventType GetInstance(string? eventName)
    {
        eventName = (eventName ?? "").Trim();
        if (eventName.Equals("DO", StringComparison.OrdinalIgnoreCase)) return EventType.DO;
        if (eventName.Equals("PO", StringComparison.OrdinalIgnoreCase)) return EventType.PO;
        if (eventName.Equals("CLICK", StringComparison.OrdinalIgnoreCase)) return EventType.CLICK;
        throw new ArgumentException("未知类型事件： " + eventName);
    }
}
