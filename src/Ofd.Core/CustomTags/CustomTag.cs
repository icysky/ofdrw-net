using Ofd.Core.BasicType;

namespace Ofd.Core.CustomTags;

/// <summary>自定义标引入口（第 16 章 图 82 表 63）。</summary>
public class CustomTag : OFDElement
{
    public CustomTag(Dom.DomElement proxy) : base(proxy) { }

    public CustomTag() : base("CustomTag") { }

    /// <summary>【必选】设置 自定义标引内容节点使用的类型标识（空白抛异常）。</summary>
    public CustomTag SetTypeID(string typeId)
    {
        if (typeId == null || typeId.Trim().Length == 0)
            throw new ArgumentException("自定义标引内容节点使用的类型标识（TypeID）为空");
        return (CustomTag)AddAttribute("TypeID", typeId);
    }

    /// <summary>【必选】获取 类型标识，缺失抛异常。</summary>
    public string GetTypeID()
    {
        var str = AttributeValue("TypeID");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("自定义标引内容节点使用的类型标识（TypeID）为空");
        return str;
    }

    /// <summary>设置 命名空间（空白忽略，与 Java 一致）。</summary>
    public CustomTag SetNameSpace(string? nameSpace)
    {
        if (nameSpace == null || nameSpace.Trim().Length == 0) return this;
        return (CustomTag)AddAttribute("NameSpace", nameSpace);
    }

    /// <summary>获取 命名空间，不存在 null。</summary>
    public string? GetNameSpace() => AttributeValue("NameSpace");

    /// <summary>【可选】设置 指向自定义标引内容节点适用的 Schema 文件（null 删除元素）。</summary>
    public CustomTag SetSchemaLoc(ST_Loc? schemaLoc)
    {
        if (schemaLoc == null)
        {
            RemoveOFDElemByNames("SchemaLoc");
            return this;
        }
        Set(new OFDSimpleTypeElement("SchemaLoc", schemaLoc));
        return this;
    }

    /// <summary>【可选】获取 Schema 文件路径，不存在 null。</summary>
    public ST_Loc? GetSchemaLoc()
    {
        var e = GetOFDElement("SchemaLoc");
        return e == null ? null : ST_Loc.GetInstance(ElementText(e).Trim());
    }

    /// <summary>【必选】设置 指向自定义标引文件（null 抛异常）。</summary>
    public CustomTag SetFileLoc(ST_Loc fileLoc)
    {
        if (fileLoc == null) throw new ArgumentException("指向自定义标引文件（FileLoc）为空");
        Set(new OFDSimpleTypeElement("FileLoc", fileLoc));
        return this;
    }

    /// <summary>【必选】获取 指向自定义标引文件，缺失抛异常。</summary>
    public ST_Loc GetFileLoc()
    {
        var e = GetOFDElement("FileLoc");
        if (e == null) throw new InvalidOperationException("指向自定义标引文件（FileLoc）为空");
        return new ST_Loc(ElementText(e).Trim());
    }
}
