using Ofd.Core.BasicType;
using Ofd.Core.Signatures.Appearance;
using Ofd.Reader;

namespace Ofd.Sign.StampPos;

/// <summary>
/// 骑缝章位置（Java RidingStampPos）：默认放在边的正中央，按页数平分印章图片。
/// </summary>
public class RidingStampPos : IStampAppearance
{
    /// <summary>默认以右侧边作为骑缝位置。</summary>
    private Side _side;

    /// <summary>图章整章宽度（毫米）。</summary>
    private double _width;

    /// <summary>图章整章高度（毫米）。</summary>
    private double _height;

    /// <summary>图章在边上距最近顶点的偏移（毫米），null 表示居中。</summary>
    private double? _offset;

    /// <summary>图章在边上的 margin（毫米）。</summary>
    private double _margin;

    /// <summary>图章指定切割等份数量（0 表示按页数）。</summary>
    private int _clipNumber;

    /// <summary>右侧边居中骑缝章。</summary>
    public RidingStampPos(double width, double height)
    {
        _width = width;
        _height = height;
        _side = Side.Right;
    }

    /// <summary>居中骑缝章。</summary>
    public RidingStampPos(Side side, double width, double height) : this(width, height) => _side = side;

    /// <summary>指定图章在边上的相对位置。</summary>
    public RidingStampPos(Side side, double? offset, double width, double height)
        : this(side, width, height) => _offset = offset;

    /// <summary>指定图章在边上的相对位置与页边距。</summary>
    public RidingStampPos(Side side, double? offset, double width, double height, double margin)
        : this(side, offset, width, height) => _margin = margin;

    /// <summary>指定图章在边上的相对位置、切割份数与页边距。</summary>
    public RidingStampPos(Side side, double? offset, int clipNumber, double width, double height, double margin)
        : this(side, offset, width, height, margin) => _clipNumber = clipNumber;

    public Side GetSide() => _side;

    public RidingStampPos SetSide(Side side)
    {
        _side = side;
        return this;
    }

    public double GetWidth() => _width;

    public RidingStampPos SetWidth(double width)
    {
        _width = width;
        return this;
    }

    public double GetHeight() => _height;

    public RidingStampPos SetHeight(double height)
    {
        _height = height;
        return this;
    }

    public double? GetOffset() => _offset;

    public RidingStampPos SetOffset(double? offset)
    {
        _offset = offset;
        return this;
    }

    public double GetMargin() => _margin;

    public RidingStampPos SetMargin(double margin)
    {
        _margin = margin;
        return this;
    }

    public int GetClipNumber() => _clipNumber;

    public RidingStampPos SetClipNumber(int clipNumber)
    {
        _clipNumber = clipNumber;
        return this;
    }

    /// <summary>Java Math.floorMod（保证非负余数）。</summary>
    private static int FloorMod(int a, int b) => ((a % b) + b) % b;

    public List<StampAnnot> GetAppearance(OFDReader ctx, ISignIdProvider idProvider)
    {
        int numPage = ctx.GetNumberOfPages();
        var res = new List<StampAnnot>(numPage);

        bool isClipNumber = _clipNumber > 0 && _clipNumber < numPage;
        int leftClipNumber = 0;

        if (_side == Side.Right || _side == Side.Left)
        {
            // 按页码平分印章图片
            double itemWith = _width / numPage;
            if (isClipNumber)
            {
                itemWith = _width / _clipNumber;
                leftClipNumber = numPage % _clipNumber;
                if (leftClipNumber == 1) leftClipNumber = _clipNumber + 1;
            }

            for (int i = 0; i < numPage; i++)
            {
                int index = leftClipNumber == 0 ? i : (leftClipNumber + i - numPage);
                if (numPage - i <= leftClipNumber)
                {
                    _clipNumber = leftClipNumber;
                    itemWith = _width / _clipNumber;
                }

                var page = ctx.GetPage(i + 1) ?? throw new SignatureException($"无法获取第 {i + 1} 页");
                var pageSize = ctx.GetPageSize(page);

                double x;
                ST_Box? clip;
                if (_side == Side.Right)
                {
                    x = pageSize.Width - itemWith * (i + 1) - _margin;
                    clip = new ST_Box(i * itemWith, 0, itemWith, _height);
                    if (isClipNumber)
                    {
                        x = pageSize.Width - (FloorMod(index, _clipNumber) + 1) * itemWith - _margin;
                        clip = new ST_Box(FloorMod(index, _clipNumber) * itemWith, 0, itemWith, _height);
                    }
                }
                else
                {
                    x = 0 - itemWith * (numPage - 1 - i) + _margin;
                    clip = new ST_Box((numPage - 1 - i) * itemWith, 0, itemWith, _height);
                    if (isClipNumber)
                    {
                        x = 0 - itemWith * FloorMod(index, _clipNumber) + _margin;
                        clip = new ST_Box(FloorMod(index, _clipNumber) * itemWith, 0, itemWith, _height);
                    }
                }

                double y = _offset ?? pageSize.Height / 2 - _height / 2;

                var refId = ctx.GetPageObjectId(i + 1)
                            ?? throw new SignatureException($"无法获取第 {i + 1} 页的对象 ID");
                res.Add(new StampAnnot()
                    .SetId(idProvider.IncrementAndGet())
                    .SetBoundary(new ST_Box(x, y, _width, _height))
                    .SetPageRef(new ST_RefID(refId))
                    .SetClip(clip));
            }
        }
        else
        {
            double itemHeight = _height / numPage;
            if (isClipNumber)
            {
                itemHeight = _height / _clipNumber;
                leftClipNumber = numPage % _clipNumber;
                if (leftClipNumber == 1) leftClipNumber = _clipNumber + 1;
            }

            for (int i = 0; i < numPage; i++)
            {
                int index = leftClipNumber == 0 ? i : (leftClipNumber + i - numPage);
                if (numPage - i <= leftClipNumber)
                {
                    _clipNumber = leftClipNumber;
                    // 注：上游此处用 width 计算高度（保持原样）
                    itemHeight = _width / _clipNumber;
                }

                var page = ctx.GetPage(i + 1) ?? throw new SignatureException($"无法获取第 {i + 1} 页");
                var pageSize = ctx.GetPageSize(page);

                double x = _offset ?? pageSize.Width / 2 - _width / 2;

                double y;
                ST_Box? clip;
                if (_side == Side.Bottom)
                {
                    y = pageSize.Height - itemHeight * (i + 1) - _margin;
                    clip = new ST_Box(0, itemHeight * i, _width, itemHeight);
                    if (isClipNumber)
                    {
                        y = pageSize.Height - (FloorMod(index, _clipNumber) + 1) * itemHeight - _margin;
                        clip = new ST_Box(0, itemHeight * FloorMod(index, _clipNumber), _width, itemHeight);
                    }
                }
                else
                {
                    y = 0 - itemHeight * (numPage - 1 - i) + _margin;
                    clip = new ST_Box(0, (numPage - 1 - i) * itemHeight, _width, itemHeight);
                    if (isClipNumber)
                    {
                        y = 0 - itemHeight * FloorMod(index, _clipNumber) + _margin;
                        clip = new ST_Box(0, FloorMod(index, _clipNumber) * itemHeight, _width, itemHeight);
                    }
                }

                var refId = ctx.GetPageObjectId(i + 1)
                            ?? throw new SignatureException($"无法获取第 {i + 1} 页的对象 ID");
                res.Add(new StampAnnot()
                    .SetId(idProvider.IncrementAndGet())
                    .SetBoundary(new ST_Box(x, y, _width, _height))
                    .SetPageRef(new ST_RefID(refId))
                    .SetClip(clip));
            }
        }

        return res;
    }
}
