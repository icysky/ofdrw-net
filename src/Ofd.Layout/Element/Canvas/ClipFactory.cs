using Ofd.Core.BasicType;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.PageDescription.Clips;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 裁剪区域构造器（Java ClipFactory）。
/// </summary>
[Obsolete("随 Canvas 绘制层一同保留，等价上游标记")]
public class ClipFactory
{
    /// <summary>裁剪路径数据。</summary>
    private AbbreviatedData? _data;

    /// <summary>裁剪路径变换矩阵。</summary>
    private ST_Array? _ctm;

    public ClipFactory(AbbreviatedData data, ST_Array ctm)
    {
        _data = data;
        _ctm = ctm;
    }

    public ClipFactory(AbbreviatedData data) => _data = data;

    public ClipFactory() { }

    public AbbreviatedData? GetData() => _data;

    public ClipFactory SetData(AbbreviatedData? data)
    {
        _data = data;
        return this;
    }

    public ST_Array? GetCtm() => _ctm;

    public ClipFactory SetCtm(ST_Array? ctm)
    {
        _ctm = ctm;
        return this;
    }

    /// <summary>以 A4 页面为边界构造裁剪区。</summary>
    public Clips Clips() => Clips(new ST_Box(0, 0, 210d, 297d));

    /// <summary>按指定边界构造裁剪区。</summary>
    public Clips Clips(ST_Box boundary)
    {
        var clips = new Clips();
        var area = new Area();
        if (_ctm != null) area.SetCTM((ST_Array)_ctm.Clone());

        var clipObj = new CT_Path().SetAbbreviatedData(_data?.Clone());
        clipObj.SetBoundary(boundary);
        area.SetClipObj(clipObj);

        return clips.AddClip(new CT_Clip().AddArea(area));
    }

    public ClipFactory Clone()
    {
        var that = new ClipFactory();
        if (_ctm != null) that._ctm = (ST_Array)_ctm.Clone();
        if (_data != null) that._data = _data.Clone();
        return that;
    }
}
