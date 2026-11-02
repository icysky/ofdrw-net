using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;

namespace Ofd.Core.PageDescription.Color.Pattern;

/// <summary>
/// 底纹单元（8.3.3）：独立坐标空间（左上角 0,0，X 右 Y 下，单位毫米）。
/// </summary>
public class CellContent : CT_PageBlock
{
    public CellContent(Dom.DomElement proxy) : base(proxy) { }

    public CellContent() : base("CellContent") { }

    /// <summary>【可选属性】设置 引用资源文件中缩略图图像的标识符（null 删除属性）。</summary>
    public CellContent SetThumbnail(ST_RefID? thumbnail)
    {
        if (thumbnail == null)
        {
            RemoveAttr("Thumbnail");
            return this;
        }
        return (CellContent)AddAttribute("Thumbnail", thumbnail.ToString());
    }

    /// <summary>【可选属性】获取 缩略图标识符引用，不存在 null。</summary>
    public ST_RefID? GetThumbnail() => ST_RefID.GetInstance(AttributeValue("Thumbnail"));

    /// <summary>【可选】增加 页块（返回 CellContent 支持链式）。</summary>
    public new CellContent AddPageBlock(IPageBlockType pageBlock)
    {
        Add((OFDElement)pageBlock);
        return this;
    }
}
