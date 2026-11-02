using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicType;

namespace Ofd.Reader;

/// <summary>
/// 旧版 OFD 解析器（Java DLOFDReader，已过时）。
/// <para>
/// 在构造时汇总文档概要：页面区域、页面 VO、注释实体与签章注释。
/// </para>
/// <para>
/// 适配说明：Java 版会深入解析 GM/T 0031 SES 电子签章数据（取印章图片 type/bytes）；
/// C# 版未内置 SES 结构解析，签章注释返回空列表（其余能力等价）。
/// 新代码请直接使用 <see cref="OFDReader"/>。
/// </para>
/// </summary>
[Obsolete("请使用 OFDReader")]
public class DLOFDReader : OFDReader
{
    private readonly ST_Loc _docRoot;
    private readonly Document _document;
    private ST_Box _documentBox = new(0, 0, 210, 297);

    /// <summary>文档概要。</summary>
    private readonly OFDDocumentVo _ofdDocumentVo;

    public DLOFDReader(string ofdFile) : base(ofdFile) => _ofdDocumentVo = InitReader(out _docRoot, out _document, out _documentBox);

    public DLOFDReader(Stream stream) : base(stream) => _ofdDocumentVo = InitReader(out _docRoot, out _document, out _documentBox);

    public DLOFDReader(string unzippedPathRoot, bool deleteOnClose) : base(unzippedPathRoot, deleteOnClose) =>
        _ofdDocumentVo = InitReader(out _docRoot, out _document, out _documentBox);

    private OFDDocumentVo InitReader(out ST_Loc docRoot, out Document document, out ST_Box documentBox)
    {
        var rl = GetResourceLocator();

        try
        {
            rl.Save();

            var docBody = GetOFDDir().GetOfd().GetDocBody()
                          ?? throw new InvalidOperationException("OFD 缺少 DocBody");
            docRoot = docBody.GetDocRoot() ?? throw new InvalidOperationException("OFD 缺少 DocRoot");
            document = rl.Get(docRoot, e => new Document(e));

            // 页面区域（缺失时回退 A4）
            var area = document.GetCommonData()?.GetPageArea();
            documentBox = area?.GetPhysicalBox() ?? new ST_Box(0, 0, 210, 297); // A4 兜底（Reader 不依赖 Layout）

            var pageVos = GetOFDPageVO(document);

            return new OFDDocumentVo(docRoot.Parent(), documentBox.Width, documentBox.Height,
                pageVos,
                new List<StampAnnotVo>(), // SES 深度解析未实现，见类注释
                GetAnnotationEntities());
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("OFD解析失败，原因:" + e.Message, e);
        }
        finally
        {
            rl.Restore();
        }
    }

    /// <summary>获取文档概要。</summary>
    public OFDDocumentVo GetOFDDocumentVo() => _ofdDocumentVo;

    private List<OfdPageVo> GetOFDPageVO(Document document)
    {
        var list = new List<OfdPageVo>();
        int pageSize = document.GetPages()?.GetSize() ?? 0;

        for (int i = 0; i < pageSize; i++) list.Add(new OfdPageVo(GetPage(i + 1), null));
        return list;
    }
}
