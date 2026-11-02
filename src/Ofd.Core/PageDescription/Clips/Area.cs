using Ofd.Core.BasicType;

namespace Ofd.Core.PageDescription.Clips;

/// <summary>裁剪区域（8.4 表 33）：用一个图形或文字对象描述裁剪区的一部分，最终裁剪区为并集。</summary>
public class Area : OFDElement
{
    public Area(Dom.DomElement proxy) : base(proxy) { }

    public Area() : base("Area") { }

    /// <summary>【可选属性】设置 引用资源文件中的绘制参数标识。</summary>
    public Area SetDrawParam(ST_RefID drawParam) => (Area)AddAttribute("DrawParam", drawParam.ToString());

    /// <summary>【可选属性】获取 绘制参数标识，不存在 null。</summary>
    public ST_RefID? GetDrawParam() => ST_RefID.GetInstance(AttributeValue("DrawParam"));

    /// <summary>【可选属性】设置 变换矩阵（对 Area 下 Path/Text 进一步变换）。</summary>
    public Area SetCTM(ST_Array ctm) => (Area)AddAttribute("CTM", ctm.ToString());

    /// <summary>【可选属性】获取 变换矩阵，不存在 null。</summary>
    public ST_Array? GetCTM() => ST_Array.GetInstance(AttributeValue("CTM"));

    /// <summary>【必选】设置 裁剪对象（CT_Text 或 CT_Path）。</summary>
    public Area SetClipObj(IClipAble clipObj)
    {
        Add((OFDElement)clipObj);
        return this;
    }

    /// <summary>【必选】获取 裁剪对象，不存在 null。</summary>
    public IClipAble? GetClipObj()
    {
        var elements = Elements();
        if (elements.Count == 0) return null;
        return ClipAbleFactory.GetInstance(elements[0]);
    }
}
