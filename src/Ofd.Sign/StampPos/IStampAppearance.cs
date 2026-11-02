using Ofd.Core.Signatures.Appearance;
using Ofd.Reader;

namespace Ofd.Sign.StampPos;

/// <summary>签章外观位置提供者（Java StampAppearance）。</summary>
public interface IStampAppearance
{
    /// <summary>获取签章外观列表。</summary>
    List<StampAnnot> GetAppearance(OFDReader ctx, ISignIdProvider idProvider);
}
