namespace Ofd.Core.Action.ActionType.ActionMovie;

/// <summary>放映参数属性（表 59）。</summary>
public enum PlayType
{
    /// <summary>播放</summary>
    Play,

    /// <summary>停止</summary>
    Stop,

    /// <summary>暂停</summary>
    Pause,

    /// <summary>继续</summary>
    Resume,
}

public static class PlayTypeExtensions
{
    /// <summary>获取放映参数，未知类型抛异常（与 Java 一致）。</summary>
    public static PlayType GetInstance(string? type)
    {
        type = (type ?? "").Trim();
        if (type.Equals("Play", StringComparison.OrdinalIgnoreCase)) return PlayType.Play;
        if (type.Equals("Stop", StringComparison.OrdinalIgnoreCase)) return PlayType.Stop;
        if (type.Equals("Pause", StringComparison.OrdinalIgnoreCase)) return PlayType.Pause;
        if (type.Equals("Resume", StringComparison.OrdinalIgnoreCase)) return PlayType.Resume;
        throw new ArgumentException("未知的放映参数： " + type);
    }
}
