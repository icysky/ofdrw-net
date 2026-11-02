using Ofd.Core.BasicType;
using Ofd.Core.Signatures.Appearance;
using Ofd.Reader;

namespace Ofd.Sign.StampPos;

/// <summary>
/// 两侧对开骑缝章位置（Java CuttingRideStampPos）：左右（或上下）两半分别落在相邻页。
/// </summary>
public class CuttingRideStampPos : IStampAppearance
{
    private readonly Side _side;
    private readonly double _width;
    private readonly double _height;
    private readonly double? _offset;
    private readonly double _margin;
    private readonly List<CuttingRatio>? _cuttingRatios;

    public CuttingRideStampPos(Side side, double? offset, double width, double height, double margin,
        List<CuttingRatio>? cuttingRatios)
    {
        _side = side;
        _width = width;
        _height = height;
        _offset = offset;
        _margin = margin;
        _cuttingRatios = cuttingRatios;
    }

    public List<StampAnnot> GetAppearance(OFDReader ctx, ISignIdProvider idProvider)
    {
        int numPage = ctx.GetNumberOfPages();
        var res = new List<StampAnnot>(numPage);

        if (_side == Side.Right || _side == Side.Left)
        {
            double itemWith = _width * 0.5;

            for (int i = 0; i < numPage; i++)
            {
                var page = ctx.GetPage(i + 1) ?? throw new SignatureException($"无法获取第 {i + 1} 页");
                var pageSize = ctx.GetPageSize(page);

                double x;
                ST_Box? clip;

                if (numPage - 1 != i)
                {
                    if (_cuttingRatios != null && _cuttingRatios.Count > 0)
                        itemWith = _width * _cuttingRatios[i].Left;

                    if (_side == Side.Right)
                    {
                        x = pageSize.Width - itemWith - _margin;
                        clip = new ST_Box(0, 0, itemWith, _height);
                    }
                    else
                    {
                        x = 0 - (_width - itemWith) + _margin;
                        clip = new ST_Box(_width - itemWith, 0, itemWith, _height);
                    }
                }
                else
                {
                    if (_cuttingRatios != null && _cuttingRatios.Count > 0)
                        itemWith = _width * _cuttingRatios[i - 1].Right;

                    if (_side == Side.Right)
                    {
                        x = 0 - (_width - itemWith) + _margin;
                        clip = new ST_Box(_width - itemWith, 0, itemWith, _height);
                    }
                    else
                    {
                        x = pageSize.Width - itemWith - _margin;
                        clip = new ST_Box(0, 0, itemWith, _height);
                    }
                }

                double y = _offset ?? pageSize.Height / 2 - _height / 2;

                var refId = ctx.GetPageObjectId(i + 1)
                            ?? throw new SignatureException($"无法获取第 {i + 1} 页的对象 ID");
                var pageRef = new ST_RefID(refId);

                res.Add(new StampAnnot()
                    .SetId(idProvider.IncrementAndGet())
                    .SetBoundary(new ST_Box(x, y, _width, _height))
                    .SetPageRef(pageRef)
                    .SetClip(clip));

                // 中间页：同一页上再放另一半
                if (i != 0 && numPage - 1 != i)
                {
                    if (_cuttingRatios != null && _cuttingRatios.Count > 0)
                        itemWith = _width * _cuttingRatios[i - 1].Right;

                    if (_side == Side.Right)
                    {
                        x = 0 - (_width - itemWith) + _margin;
                        clip = new ST_Box(_width - itemWith, 0, itemWith, _height);
                    }
                    else
                    {
                        x = pageSize.Width - itemWith - _margin;
                        clip = new ST_Box(0, 0, itemWith, _height);
                    }

                    res.Add(new StampAnnot()
                        .SetId(idProvider.IncrementAndGet())
                        .SetBoundary(new ST_Box(x, y, _width, _height))
                        .SetPageRef(pageRef)
                        .SetClip(clip));
                }
            }
        }

        return res;
    }
}
