namespace Ofd.Layout.Engine;

/// <summary>线程安全自增计数器（Java AtomicInteger 的轻量对应，用于 MaxUnitID）。</summary>
public class AtomicCounter
{
    private int _value;

    public AtomicCounter(int initial = 0) => _value = initial;

    /// <summary>自增并返回新值。</summary>
    public int IncrementAndGet() => Interlocked.Increment(ref _value);

    /// <summary>获取当前值。</summary>
    public int Get() => Volatile.Read(ref _value);

    /// <summary>设置当前值。</summary>
    public void Set(int value) => Volatile.Write(ref _value, value);
}
