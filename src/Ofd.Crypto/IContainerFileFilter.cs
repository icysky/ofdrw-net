namespace Ofd.Crypto;

/// <summary>OFD 容器文件过滤器（Java ContainerFileFilter）。</summary>
public interface IContainerFileFilter
{
    /// <summary>执行过滤：true 保留（加密），false 忽略。</summary>
    bool Filter(string containerPath, string absPath);
}
