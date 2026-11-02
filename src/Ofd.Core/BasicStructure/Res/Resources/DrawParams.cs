using Ofd.Core.PageDescription.DrawParam;

namespace Ofd.Core.BasicStructure.Res.Resources;

/// <summary>包含了一组绘制参数的描述（7.9 图 20 表 18）。</summary>
public class DrawParams : OFDElement, IOFDResource
{
    public DrawParams(Dom.DomElement proxy) : base(proxy) { }

    public DrawParams() : base("DrawParams") { }

    /// <summary>【必选】增加 绘制参数描述（必须有 ID；null 忽略）。</summary>
    public DrawParams AddDrawParam(CT_DrawParam? drawParam)
    {
        if (drawParam == null) return this;
        if (drawParam.GetID() == null)
            throw new ArgumentException("绘制参数描述ID不能为空");
        Add(drawParam);
        return this;
    }

    /// <summary>【必选】获取 绘制参数描述序列。</summary>
    public List<CT_DrawParam> GetDrawParams() => GetOFDElements("DrawParam", e => new CT_DrawParam(e));
}
