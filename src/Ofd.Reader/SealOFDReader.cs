using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Core.Text.Font;

namespace Ofd.Reader;

/// <summary>
/// 印章 OFD 解析器（Java SealOFDReader，已过时）。
/// <para>提供公共资源中的字体与绘制参数、页面 VO 列表。</para>
/// </summary>
[Obsolete("请使用 DLOFDReader 或 OFDReader")]
public class SealOFDReader : OFDReader
{
    private readonly ST_Loc _docRoot;
    private readonly Document _document;

    public SealOFDReader(string ofdFile) : base(ofdFile) => (_docRoot, _document) = InitDoc();

    public SealOFDReader(Stream inputStream) : base(inputStream) => (_docRoot, _document) = InitDoc();

    private (ST_Loc, Document) InitDoc()
    {
        var rl = GetResourceLocator();
        try
        {
            rl.Save();
            var docBody = GetOFDDir().GetOfd().GetDocBody()
                          ?? throw new InvalidOperationException("OFD 缺少 DocBody");
            var docRoot = docBody.GetDocRoot()
                          ?? throw new InvalidOperationException("OFD 缺少 DocRoot");

            var document = rl.Get(docRoot, e => new Document(e));
            return (docRoot, document);
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

    /// <summary>获取页面 VO 列表。</summary>
    public List<OfdPageVo> GetOFDPageVO()
    {
        var list = new List<OfdPageVo>();
        var rl = GetResourceLocator();

        try
        {
            int pageSize = _document.GetPages()?.GetSize() ?? 0;
            for (int i = 0; i < pageSize; i++)
                list.Add(new OfdPageVo(GetPage(i + 1), null));
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("OFD解析失败，原因:" + e.Message, e);
        }
        finally
        {
            rl.Restore();
        }

        return list;
    }

    /// <summary>获取公共资源中的字体。</summary>
    public List<CT_Font> GetPublicResFonts()
    {
        var fonts = new List<CT_Font>();
        var rl = GetResourceLocator();

        try
        {
            rl.Save();
            rl.Cd(_docRoot.Parent());

            var publicResLoc = _document.GetCommonData()?.GetPublicRes();
            if (publicResLoc == null) return fonts;

            var res = rl.Get(publicResLoc, e => new Res(e));
            foreach (Fonts group in res.GetFonts()) fonts.AddRange(group.GetFonts());
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("OFD解析失败，原因:" + e.Message, e);
        }
        finally
        {
            rl.Restore();
        }

        return fonts;
    }

    /// <summary>获取公共资源中的绘制参数。</summary>
    public List<CT_DrawParam> GetPublicResDrawParam()
    {
        var list = new List<CT_DrawParam>();
        var rl = GetResourceLocator();

        try
        {
            rl.Save();
            rl.Cd(_docRoot.Parent());

            var publicResLoc = _document.GetCommonData()?.GetPublicRes();
            if (publicResLoc == null) return list;

            var res = rl.Get(publicResLoc, e => new Res(e));
            foreach (var resource in res.GetResources())
            {
                if (resource is DrawParams dp) list.AddRange(dp.GetDrawParams());
            }
        }
        catch (Exception)
        {
            // 与 Java 一致：解析失败返回已收集内容
        }
        finally
        {
            rl.Restore();
        }

        return list;
    }
}
