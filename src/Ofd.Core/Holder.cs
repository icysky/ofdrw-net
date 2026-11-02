namespace Ofd.Core;

/// <summary>
/// 外壳对象，用于解决 lambda/迭代器作用域内的赋值问题（Java org.ofdrw.core.Holder）。
/// </summary>
public sealed class Holder<T>
{
    public T? Value;

    public Holder() { }

    public Holder(T value)
    {
        Value = value;
    }
}
