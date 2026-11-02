namespace Ofd.Sign;

/// <summary>时间戳 hook（Java TimeStampHook）：输入签名值，返回时间戳数据（null 表示不附加）。</summary>
public delegate byte[]? TimeStampHook(byte[] signature);
