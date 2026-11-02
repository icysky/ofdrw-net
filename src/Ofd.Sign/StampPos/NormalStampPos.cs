using Ofd.Core.BasicType;
using Ofd.Core.Signatures.Appearance;
using Ofd.Reader;

namespace Ofd.Sign.StampPos;

/// <summary>普通印章位置（Java NormalStampPos）：指定页码 + 左上角坐标 + 宽高（毫米）。</summary>
public class NormalStampPos : IStampAppearance
{
    private int _page;
    private double _tlx;
    private double _tly;
    private double _width;
    private double _height;

    public NormalStampPos(int page, double tlx, double tly, double width, double height)
    {
        _page = page;
        _tlx = tlx;
        _tly = tly;
        _width = width;
        _height = height;
    }

    public int GetPage() => _page;

    public NormalStampPos SetPage(int page)
    {
        _page = page;
        return this;
    }

    public double GetX() => _tlx;

    public NormalStampPos SetX(double tlx)
    {
        _tlx = tlx;
        return this;
    }

    public double GetY() => _tly;

    public NormalStampPos SetY(double tly)
    {
        _tly = tly;
        return this;
    }

    public double GetWidth() => _width;

    public NormalStampPos SetWidth(double width)
    {
        _width = width;
        return this;
    }

    public double GetHeight() => _height;

    public NormalStampPos SetHeight(double height)
    {
        _height = height;
        return this;
    }

    public List<StampAnnot> GetAppearance(OFDReader ctx, ISignIdProvider idProvider)
    {
        var pageObjId = ctx.GetPageObjectId(_page)
                        ?? throw new SignatureException($"无法获取第 {_page} 页的对象 ID");

        var annot = new StampAnnot()
            .SetId(idProvider.IncrementAndGet())
            .SetBoundary(new ST_Box(_tlx, _tly, _width, _height))
            .SetPageRef(new ST_RefID(pageObjId));

        return new List<StampAnnot>(1) { annot };
    }
}
