using Ofd.Core.BasicType;

namespace Ofd.Core.Signatures.Range;

/// <summary>针对一个文件的摘要节点（18.2.2 图 87 表 68）。</summary>
public class Reference : OFDElement
{
    public Reference(Dom.DomElement proxy) : base(proxy) { }

    public Reference() : base("Reference") { }

    public Reference(ST_Loc fileRef, byte[] checkValue) : this()
    {
        SetFileRef(fileRef).SetCheckValue(checkValue);
    }

    /// <summary>【必选属性】设置 指向包内的文件（绝对路径；null 抛异常）。</summary>
    public Reference SetFileRef(ST_Loc fileRef)
    {
        if (fileRef == null) throw new ArgumentException("指向包内的文件（FileRef）为空");
        return (Reference)AddAttribute("FileRef", fileRef.ToString());
    }

    /// <summary>【必选属性】获取 指向包内的文件，不存在 null。</summary>
    public ST_Loc? GetFileRef() => ST_Loc.GetInstance(AttributeValue("FileRef"));

    /// <summary>【必选】设置 摘要计算值的杂凑值（Base64 编码写入；空数组抛异常）。</summary>
    public Reference SetCheckValue(byte[] checkValue)
    {
        if (checkValue == null || checkValue.Length == 0) throw new ArgumentException("摘要计算值（CheckValue）为空");
        SetOFDEntity("CheckValue", Convert.ToBase64String(checkValue));
        return this;
    }

    /// <summary>【必选】获取 摘要计算值的杂凑值（Base64 解码），缺失抛异常。</summary>
    public byte[] GetCheckValue()
    {
        var e = GetOFDElement("CheckValue");
        if (e == null) throw new InvalidOperationException("摘要计算值（CheckValue）为空");
        return Convert.FromBase64String(ElementText(e).Trim());
    }
}
