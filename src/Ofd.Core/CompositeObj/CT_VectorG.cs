using System.Globalization;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicType;

namespace Ofd.Core.CompositeObj;

/// <summary>矢量图像（13 图 72 表 50）：复合对象引用的矢量图像资源。</summary>
public class CT_VectorG : OFDElement
{
    public CT_VectorG(Dom.DomElement proxy) : base(proxy) { }

    public CT_VectorG() : base("VectorG") { }

    protected CT_VectorG(string name) : base(name) { }

    public ST_ID? GetID() => GetObjID();

    public CT_VectorG SetID(ST_ID id) => (CT_VectorG)SetObjID(id);

    /// <summary>【必选属性】设置 矢量图像宽度。</summary>
    public CT_VectorG SetWidth(double width) => (CT_VectorG)AddAttribute("Width", STBase.Fmt(width));

    /// <summary>【必选属性】获取 矢量图像宽度，缺失抛异常（与 Java 一致）。</summary>
    public double GetWidth() => GetRequiredDouble("Width", "格式非法无法获取到矢量图像的宽度（Width）");

    /// <summary>【必选属性】设置 矢量图像高度。</summary>
    public CT_VectorG SetHeight(double height) => (CT_VectorG)AddAttribute("Height", STBase.Fmt(height));

    /// <summary>【必选属性】获取 矢量图像高度，缺失抛异常。</summary>
    public double GetHeight() => GetRequiredDouble("Height", "格式非法无法获取到矢量图像的宽度（Height）");

    /// <summary>【可选】设置 缩略图引用。</summary>
    public CT_VectorG SetThumbnail(ST_RefID thumbnail) => (CT_VectorG)SetOFDEntity("Thumbnail", thumbnail.ToString());

    /// <summary>【可选】获取 缩略图引用，不存在 null。</summary>
    public ST_RefID? GetThumbnail() => ST_RefID.GetInstance(GetOFDElementText("Thumbnail"));

    /// <summary>【可选】设置 替换图像引用（高分辨率输出时替代缩略图）。</summary>
    public CT_VectorG SetSubstitution(ST_RefID substitution) => (CT_VectorG)SetOFDEntity("Substitution", substitution.ToString());

    /// <summary>【可选】获取 替换图像引用，不存在 null。</summary>
    public ST_RefID? GetSubstitution() => ST_RefID.GetInstance(GetOFDElementText("Substitution"));

    /// <summary>【必选】设置 内容的矢量描述（null 抛异常）。</summary>
    public CT_VectorG SetContent(Content content)
    {
        if (content == null) throw new ArgumentException("内容的矢量描述（Content）不能为空");
        Set(content);
        return this;
    }

    /// <summary>【必选】增加 内容的矢量描述（已有 Content 时追加页块）。</summary>
    public CT_VectorG AddContent(IPageBlockType blockType)
    {
        if (blockType == null) return this;
        var e = GetOFDElement("Content");
        var content = e == null ? new Content() : new Content(e);
        content.AddPageBlock(blockType);
        Set(content);
        return this;
    }

    /// <summary>【必选】获取 内容的矢量描述，不存在抛异常（与 Java 一致）。</summary>
    public Content GetContent()
    {
        var e = GetOFDElement("Content");
        if (e == null) throw new InvalidOperationException("没有找到Content元素");
        return new Content(e);
    }

    private double GetRequiredDouble(string name, string errorMsg)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) throw new InvalidOperationException(errorMsg);
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
