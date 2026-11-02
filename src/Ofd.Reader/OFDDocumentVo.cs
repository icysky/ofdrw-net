using Ofd.Reader.Model;

namespace Ofd.Reader;

/// <summary>文档概要信息（已过时，改用 <see cref="DLOFDReader"/>）。</summary>
[Obsolete("请使用 DLOFDReader")]
public class OFDDocumentVo
{
    private readonly string _docPath;
    private readonly double _pageWidth;
    private readonly double _pageHeight;
    private readonly List<OfdPageVo> _ofdPageVoList;
    private readonly List<StampAnnotVo> _stampAnnotVos;
    private readonly List<AnnotionEntity> _annotations;

    public OFDDocumentVo(string docPath, double pageWidth, double pageHeight,
        List<OfdPageVo> ofdPageVoList, List<StampAnnotVo> stampAnnotVoList, List<AnnotionEntity> annotations)
    {
        _docPath = docPath;
        _pageWidth = pageWidth;
        _pageHeight = pageHeight;
        _ofdPageVoList = ofdPageVoList;
        _stampAnnotVos = stampAnnotVoList;
        _annotations = annotations;
    }

    public string GetDocPath() => _docPath;

    public List<StampAnnotVo> GetStampAnnotVos() => _stampAnnotVos;

    public double GetPageWidth() => _pageWidth;

    public double GetPageHeight() => _pageHeight;

    public List<AnnotionEntity> GetAnnotations() => _annotations;

    public List<OfdPageVo> GetOfdPageVoList() => _ofdPageVoList;
}
