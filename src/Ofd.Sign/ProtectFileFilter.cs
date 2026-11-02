namespace Ofd.Sign;

/// <summary>待保护文件过滤器（Java ProtectFileFilter）：true 表示纳入保护范围。</summary>
public delegate bool ProtectFileFilter(string filePath);
