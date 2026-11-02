using System.Globalization;
using System.Text.RegularExpressions;
using Ofd.Core;
using Ofd.Core.Annotation;
using Ofd.Core.Annotation.PageAnnot;
using Ofd.Core.Attachment;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Core.Signatures;
using Ofd.Pkg.Container;
using Ofd.Reader.Model;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Reader;

/// <summary>
/// OFD 解析器（Java OFDReader）：解压 OFD 到临时工作目录，提供页面/资源/附件/注释的只读访问。
/// 使用完毕后 Close/Dispose 会删除工作目录。
/// </summary>
public class OFDReader : IDisposable
{
    /// <summary>Reader 工作目录（解压后的 OFD 容器内容）。</summary>
    private readonly string _workDir;

    /// <summary>OFD 虚拟容器对象。</summary>
    private readonly OFDDir _ofdDir;

    /// <summary>资源定位器。</summary>
    protected readonly ResourceLocator Rl;

    /// <summary>资源加载器。</summary>
    private readonly ResourceManage _resMgt;

    /// <summary>关闭时是否删除工作目录。</summary>
    private readonly bool _deleteOnClose = true;

    /// <summary>是否已经关闭文档。</summary>
    private bool _closed;

    /// <summary>从 OFD 文件创建解析器。</summary>
    public OFDReader(string ofdFile) : this(new FileInfo(ofdFile)) { }

    /// <summary>从 OFD 文件创建解析器（解压到临时工作目录）。</summary>
    public OFDReader(FileInfo ofdFile)
    {
        if (ofdFile == null || !ofdFile.Exists)
            throw new ArgumentException("文件位置(ofdFile)不正确");

        _workDir = NewWorkDir();
        ZipUtil.UnZipFileByApacheCommonCompress(ofdFile, _workDir + Path.DirectorySeparatorChar);
        _ofdDir = new OFDDir(_workDir);
        Rl = new ResourceLocator(_ofdDir);
        _resMgt = new ResourceManage(this);
    }

    /// <summary>从 OFD 输入流创建解析器（流由调用方关闭）。</summary>
    public OFDReader(Stream stream)
    {
        if (stream == null) throw new ArgumentException("文件输入流(stream)不正确");

        _workDir = NewWorkDir();
        ZipUtil.UnZipFileByApacheCommonCompress(stream, _workDir + Path.DirectorySeparatorChar);
        _ofdDir = new OFDDir(_workDir);
        Rl = new ResourceLocator(_ofdDir);
        _resMgt = new ResourceManage(this);
    }

    /// <summary>
    /// 使用已经解压好的 OFD 根目录创建解析器（用于无法自动解压的文档）。
    /// </summary>
    /// <param name="unzippedPathRoot">已解压的 OFD 根目录</param>
    /// <param name="deleteOnClose">退出时是否删除该目录</param>
    public OFDReader(string unzippedPathRoot, bool deleteOnClose)
    {
        if (!Directory.Exists(unzippedPathRoot))
            throw new ArgumentException("文件位置(unzippedPathRoot)不正确");

        _workDir = unzippedPathRoot;
        _ofdDir = new OFDDir(_workDir);
        Rl = new ResourceLocator(_ofdDir);
        // 谁创建的目录谁负责：外部目录默认不删除
        _deleteOnClose = deleteOnClose;
        if (!deleteOnClose) _closed = true;
        _resMgt = new ResourceManage(this);
    }

    private static string NewWorkDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ofd-tmp-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>获取 Reader 工作目录。</summary>
    public string GetWorkDir() => _workDir;

    /// <summary>获取文档虚拟容器。</summary>
    public OFDDir GetOFDDir() => _ofdDir;

    /// <summary>获取资源定位器。</summary>
    public ResourceLocator GetResourceLocator() => Rl;

    /// <summary>获取资源管理器（提供的对象均为只读副本）。</summary>
    public ResourceManage GetResMgt() => _resMgt;

    /// <summary>启用或关闭命名空间严格解析模式（默认关闭）。</summary>
    public static void SetNamespaceStrictMode(bool enable) => OFDElement.NSStrictMode = enable;

    // ==================== 签名列表 ====================

    /// <summary>获取默认文档 Doc_0 中签名列表文件的绝对路径，不存在返回 null。</summary>
    public ST_Loc? GetDefaultDocSignaturesPath()
    {
        try
        {
            Rl.Save();
            Rl.Cd("/");
            var docBody = _ofdDir.GetOfd().GetDocBody() ?? throw new BadOFDException("OFD.xml 中不存在 DocBody");
            var loc = docBody.GetSignatures();
            if (loc == null) return null;
            return new ST_Loc(Rl.ToAbsolutePath(loc));
        }
        catch (FileNotFoundException e)
        {
            throw new BadOFDException("错误OFD结构和文件格式", e);
        }
        finally
        {
            Rl.Restore();
        }
    }

    /// <summary>获取默认的签名列表对象，不存在返回 null。</summary>
    public Signatures? GetDefaultSignatures()
    {
        var signaturesLoc = GetDefaultDocSignaturesPath();
        if (signaturesLoc == null || !Rl.Exist(signaturesLoc.Loc)) return null;

        try
        {
            return Rl.Get(signaturesLoc, e => new Signatures(e));
        }
        catch (FileNotFoundException e)
        {
            throw new BadOFDException("错误OFD结构和文件格式", e);
        }
    }

    /// <summary>文档是否包含数字签名。</summary>
    public bool HasSignature()
    {
        try
        {
            var docBody = _ofdDir.GetOfd().GetDocBody();
            return docBody?.GetSignatures() != null;
        }
        catch (FileNotFoundException e)
        {
            throw new BadOFDException("错误OFD结构和文件格式", e);
        }
    }

    // ==================== 注释 ====================

    /// <summary>获取注释列表文件对象，不存在返回 null。</summary>
    public Annotations? GetAnnotations()
    {
        try
        {
            var document = CdDefaultDoc();
            var annotations = document.GetAnnotations();
            if (annotations == null || !Rl.Exist(annotations.Loc)) return null;
            return Rl.Get(annotations, e => new Annotations(e));
        }
        catch (FileNotFoundException e)
        {
            throw new BadOFDException("OFD解析失败，原因:" + e.Message, e);
        }
        finally
        {
            Rl.Restore();
        }
    }

    /// <summary>获取默认文档中的所有注释实体。</summary>
    public List<AnnotionEntity> GetAnnotationEntities()
    {
        try
        {
            var document = CdDefaultDoc();
            var annInfosLoc = document.GetAnnotations();
            if (annInfosLoc == null || !Rl.Exist(annInfosLoc.Loc)) return new List<AnnotionEntity>();

            var annotations = Rl.Get(annInfosLoc, e => new Annotations(e));
            Rl.Cd(annInfosLoc.Parent());
            try
            {
                var res = new List<AnnotionEntity>();
                foreach (var annPage in annotations.GetPages())
                {
                    try
                    {
                        var fileLoc = annPage.GetFileLoc();
                        if (fileLoc == null) continue;
                        var pageAnnot = Rl.Get(fileLoc, e => new PageAnnot(e));
                        res.Add(new AnnotionEntity(
                            annPage.GetPageID()?.ToString() ?? "",
                            pageAnnot.GetAnnots()));
                    }
                    catch (Exception)
                    {
                        // 忽略无法加载的注释文件，尽力而为
                    }
                }
                return res;
            }
            finally
            {
                Rl.Restore();
            }
        }
        catch (FileNotFoundException e)
        {
            throw new InvalidOperationException("OFD解析失败，原因:" + e.Message, e);
        }
    }

    // ==================== 页面 ====================

    /// <summary>获取 OFD 总页面数量。</summary>
    public int GetNumberOfPages()
    {
        try
        {
            var document = CdDefaultDoc();
            var pages = document.GetPages() ?? throw new BadOFDException("Document.xml 中不存在 Pages");
            return pages.GetSize();
        }
        catch (FileNotFoundException e)
        {
            throw new BadOFDException("OFD解析失败，原因:" + e.Message, e);
        }
        finally
        {
            Rl.Restore();
        }
    }

    /// <summary>获取页面信息（页码从 1 起）。</summary>
    public PageInfo GetPageInfo(int pageNum)
    {
        if (pageNum <= 0) throw new FormatException("页码(pageNum)不能小于0");

        try
        {
            Rl.Save();
            int index = pageNum - 1;
            var document = CdDefaultDoc();
            var pages = document.GetPages() ?? throw new BadOFDException("Document.xml 中不存在 Pages");
            var pageList = pages.GetPages();
            if (index >= pageList.Count)
                throw new FormatException(pageNum + "超过最大页码:" + pageList.Count);

            var pageLoc = pageList[index].GetBaseLoc() ?? throw new BadOFDException("页面 BaseLoc 为空");
            var obj = Rl.Get(pageLoc, e => new PageObj(e));
            pageLoc = Rl.GetAbsTo(pageLoc);
            var pageSize = GetPageSize(obj);

            // 加载模板
            var templatePages = new List<TemplatePageEntity>();
            foreach (var item in obj.GetTemplates())
            {
                var templateId = item.GetTemplateID();
                if (templateId == null) continue;
                var template = GetTemplate(templateId.ToString());
                if (template == null) continue;
                template.SetOrder(TypeExtensions.GetInstance(item.AttributeValue("ZOrder")));
                templatePages.Add(template);
            }

            // Page_N 序号
            int n = index;
            string pageNName = new ST_Loc(pageLoc.Parent()).GetFileName().ToLowerInvariant();
            if (Regex.IsMatch(pageNName, @"page_\d+"))
            {
                if (int.TryParse(pageNName.Replace("page_", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                    n = parsed;
            }

            return new PageInfo()
                .SetIndex(pageNum)
                .SetId(pageList[index].GetID())
                .SetObj(obj)
                .SetSize(pageSize.Clone())
                .SetPageAbsLoc(pageLoc)
                .SetTemplates(templatePages)
                .SetPageN(n);
        }
        catch (FileNotFoundException e)
        {
            throw new InvalidOperationException("OFD解析失败，原因:" + e.Message, e);
        }
        finally
        {
            Rl.Restore();
        }
    }

    /// <summary>获取页面信息集合。</summary>
    public List<PageInfo> GetPageList()
    {
        int numberOfPages = GetNumberOfPages();
        var res = new List<PageInfo>(numberOfPages);
        for (int i = 0; i < numberOfPages; i++)
            res.Add(GetPageInfo(i + 1));
        return res;
    }

    /// <summary>解析页面模板对象，不存在返回 null。</summary>
    public TemplatePageEntity? GetTemplate(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        try
        {
            var document = CdDefaultDoc();
            var commonData = document.GetCommonData();
            if (commonData == null) return null;

            foreach (var item in commonData.GetTemplatePages())
            {
                if (id != item.GetID()?.ToString()) continue;
                var loc = item.GetBaseLoc();
                if (loc == null) break;
                var page = Rl.Get(loc, e => new PageObj(e));
                return new TemplatePageEntity(item, page);
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            Rl.Restore();
        }
    }

    /// <summary>切换目录到指定文档（会改变资源定位器工作目录，需外部 restore）。</summary>
    public Document CdDoc(int numOfDoc)
    {
        Rl.Save();
        Rl.Cd("/");
        var docBody = _ofdDir.GetOfd().GetDocBody(numOfDoc);
        var docRoot = docBody.GetDocRoot() ?? throw new BadOFDException("DocBody 中 DocRoot 为空");
        var document = Rl.Get(docRoot, e => new Document(e));
        Rl.Cd(docRoot.Parent());
        return document;
    }

    /// <summary>获取指定文档对象（不改变工作目录）。</summary>
    public Document GetDoc(int numOfDoc)
    {
        Rl.Save();
        try
        {
            Rl.Cd("/");
            var docBody = _ofdDir.GetOfd().GetDocBody(numOfDoc);
            var docRoot = docBody.GetDocRoot() ?? throw new BadOFDException("DocBody 中 DocRoot 为空");
            return Rl.Get(docRoot, e => new Document(e));
        }
        finally
        {
            Rl.Restore();
        }
    }

    /// <summary>切换目录到默认文档（Doc_0），需外部 restore。</summary>
    public Document CdDefaultDoc() => CdDoc(0);

    /// <summary>获取页面物理大小（页面未定义时用 CommonData 定义，均无则 A4）。</summary>
    public ST_Box GetPageSize(PageObj? page)
    {
        var pageArea = GetPageArea(page);
        return pageArea.GetBox() ?? new ST_Box(0, 0, 210d, 297d);
    }

    /// <summary>获取页面区域（页码从 1 起，越界时返回默认区域）。</summary>
    public CT_PageArea? GetPageArea(int num) => GetPageArea(GetPage(num));

    /// <summary>获取页面物理尺寸（页码从 1 起）。</summary>
    public ST_Box GetPageSize(int num) => GetPageSize(GetPage(num));

    /// <summary>获取页面区域：页面 Area → 模板中 ZOrder 最大且有区域的模板 → CommonData → A4。</summary>
    public CT_PageArea GetPageArea(PageObj? page)
    {
        CT_PageArea? res = null;

        if (page != null)
        {
            var pageArea = page.GetArea();
            if (pageArea != null) return pageArea;

            CT_PageArea? tplArea = null;
            int biggestOrder = -1;
            foreach (var item in page.GetTemplates())
            {
                var templateId = item.GetTemplateID();
                if (templateId == null) continue;
                var template = GetTemplate(templateId.ToString());
                if (template == null) continue;

                var area = template.GetPage().GetArea();
                int order = template.GetZOrder().Order();
                if (area != null && order > biggestOrder && area.GetBox() != null)
                {
                    tplArea = area;
                    biggestOrder = order;
                }
            }
            res = tplArea;
        }

        if (res == null)
        {
            try
            {
                var document = _ofdDir.ObtainDocDefault().GetDocument();
                res = document.GetCommonData()?.GetPageArea();
            }
            catch (Exception e)
            {
                throw new BadOFDException("OFD解析失败，原因:" + e.Message, e);
            }
        }

        // 无法找到区域时使用 A4，防止后续 NPE
        return res ?? new CT_PageArea(0, 0, 210d, 297d);
    }

    /// <summary>通过页码获取页面对象（页码从 1 起）。</summary>
    public PageObj GetPage(int pageNum)
    {
        var pageLoc = GetPageAbsLoc(pageNum);
        try
        {
            return Rl.Get(pageLoc, e => new PageObj(e));
        }
        catch (FileNotFoundException e)
        {
            throw new InvalidOperationException("OFD解析失败，原因:" + e.Message, e);
        }
    }

    /// <summary>获取页码对应页面文件在文档中的绝对路径（以 "/" 开头）。</summary>
    public ST_Loc GetPageAbsLoc(int pageNum)
    {
        if (pageNum <= 0) throw new FormatException("页码(pageNum)不能小于0");

        try
        {
            Rl.Save();
            int index = pageNum - 1;
            var document = CdDefaultDoc();
            var pages = document.GetPages() ?? throw new BadOFDException("Document.xml 中不存在 Pages");
            var pageList = pages.GetPages();
            if (index >= pageList.Count)
                throw new FormatException(pageNum + "超过最大页码:" + pageList.Count);

            var pageLoc = pageList[index].GetBaseLoc() ?? throw new BadOFDException("页面 BaseLoc 为空");
            return new ST_Loc(Rl.ToAbsolutePath(pageLoc));
        }
        catch (FileNotFoundException e)
        {
            throw new InvalidOperationException("OFD解析失败，原因:" + e.Message, e);
        }
        finally
        {
            Rl.Restore();
        }
    }

    /// <summary>获取页面对象 ID（页码从 1 起）。</summary>
    public ST_ID? GetPageObjectId(int pageNum)
    {
        if (pageNum <= 0) throw new FormatException("页码(pageNum)不能小于0");

        try
        {
            Rl.Save();
            int index = pageNum - 1;
            var document = CdDefaultDoc();
            var pages = document.GetPages() ?? throw new BadOFDException("Document.xml 中不存在 Pages");
            var pageList = pages.GetPages();
            if (index >= pageList.Count)
                throw new FormatException(pageNum + "超过最大页码:" + pageList.Count);

            return pageList[index].GetID();
        }
        catch (FileNotFoundException e)
        {
            throw new InvalidOperationException("OFD解析失败，原因:" + e.Message, e);
        }
        finally
        {
            Rl.Restore();
        }
    }

    // ==================== 附件 ====================

    /// <summary>获取所有附件对象（只读；FileLoc 已转为绝对路径），无附件返回空列表。</summary>
    public List<CT_Attachment> GetAttachmentList()
    {
        Rl.Save();
        try
        {
            var docDir = _ofdDir.ObtainDocDefault();
            Rl.Cd(docDir);

            Document document;
            try
            {
                document = docDir.GetDocument();
            }
            catch (Exception e)
            {
                throw new BadOFDException(e);
            }

            var attachmentsLoc = document.GetAttachments();
            if (attachmentsLoc == null || !Rl.Exist(attachmentsLoc.Loc)) return new List<CT_Attachment>();

            Attachments attachments;
            try
            {
                attachments = Rl.Get(attachmentsLoc, e => new Attachments(e));
            }
            catch (Exception)
            {
                Console.Error.WriteLine(">> 无法获取或解析Attachments.xml");
                return new List<CT_Attachment>();
            }

            string parent = attachmentsLoc.Parent();
            if (!string.IsNullOrEmpty(parent)) Rl.Cd(parent);

            var res = attachments.GetAttachments();
            // 设置文件为绝对路径，避免外部读取时工作路径不正确
            foreach (var item in res)
            {
                var fileLoc = item.GetFileLoc();
                if (fileLoc != null) item.SetFileLoc(Rl.GetAbsTo(fileLoc));
            }
            return res;
        }
        finally
        {
            Rl.Restore();
        }
    }

    /// <summary>获取附件文件路径（工作目录在 Close 时删除，请提前复制）。</summary>
    public string? GetAttachmentFile(CT_Attachment? attachment)
    {
        if (attachment == null) return null;
        var fileLoc = attachment.GetFileLoc();
        if (fileLoc == null) return null;
        try
        {
            return Rl.GetFile(fileLoc);
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine(">> 无法根据附件对象的描述获取到附件: " + fileLoc);
            return null;
        }
    }

    /// <summary>按附件名称获取附件对象，不存在返回 null。</summary>
    public CT_Attachment? GetAttachment(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        foreach (var attachment in GetAttachmentList())
            if (attachment.GetAttachmentName() == name)
                return attachment;
        return null;
    }

    /// <summary>按附件名称获取附件文件路径。</summary>
    public string? GetAttachmentFile(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        Rl.Save();
        try
        {
            var attachment = GetAttachmentInternal(name, Rl);
            if (attachment == null) return null;
            var fileLoc = attachment.GetFileLoc();
            if (fileLoc == null) return null;

            try
            {
                return Rl.GetFile(fileLoc);
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine(">> 无法根据附件对象的描述获取到附件: " + fileLoc);
                return null;
            }
        }
        finally
        {
            Rl.Restore();
        }
    }

    private CT_Attachment? GetAttachmentInternal(string name, ResourceLocator rl)
    {
        var docDir = _ofdDir.ObtainDocDefault();
        rl.Cd(docDir);

        Document document;
        try
        {
            document = docDir.GetDocument();
        }
        catch (Exception e)
        {
            throw new BadOFDException(e);
        }

        var attachmentsLoc = document.GetAttachments();
        if (attachmentsLoc == null || !rl.Exist(attachmentsLoc.Loc)) return null;

        Attachments attachments;
        try
        {
            attachments = rl.Get(attachmentsLoc, e => new Attachments(e));
        }
        catch (Exception)
        {
            Console.Error.WriteLine(">> 无法获取或解析Attachments.xml");
            return null;
        }

        string parent = attachmentsLoc.Parent();
        if (!string.IsNullOrEmpty(parent)) rl.Cd(parent);

        foreach (var attachment in attachments.GetAttachments())
            if (attachment.GetAttachmentName() == name)
                return attachment;
        return null;
    }

    // ==================== 签章 ====================

    /// <summary>
    /// 获取默认文档中的签章信息（解析 SignedValue.dat，提取印章图片）。
    /// </summary>
    public List<StampAnnotEntity> GetStampAnnots()
    {
        if (!HasSignature())
        {
            // 没有签名时返回空集合，防止 NPE（Java 同）
            return new List<StampAnnotEntity>();
        }

        try
        {
            Rl.Save();

            var sigFileList = GetDefaultSignatures();
            if (sigFileList == null) return new List<StampAnnotEntity>();

            var signaturesLoc = GetDefaultDocSignaturesPath();
            if (signaturesLoc == null) return new List<StampAnnotEntity>();

            // 切换目录到 Signatures.xml 所在目录
            Rl.Cd(signaturesLoc.Parent());

            var sigInfoList = sigFileList.GetSignatures();
            var res = new List<StampAnnotEntity>(sigInfoList.Count);

            foreach (var sigInfoItem in sigInfoList)
            {
                var signatureBaseLoc = sigInfoItem.GetBaseLoc();
                if (signatureBaseLoc == null) continue;

                Rl.Save();
                try
                {
                    // 签名描述文件
                    var sigDesp = Rl.Get(signatureBaseLoc, e => new Ofd.Core.Signatures.Sig.Signature(e));
                    try
                    {
                        Rl.Cd(signatureBaseLoc.Parent());
                        var signedValueLoc = sigDesp.GetSignedValue();
                        if (signedValueLoc == null) continue;

                        Rl.Cd(signedValueLoc.Parent());
                        string signedValueFile = Rl.GetFile(signedValueLoc);

                        // 解析电子签章（SES）
                        var holder = Ofd.Gm.Ses.Parse.VersionParser
                            .ParseSesSignatureVersion(File.ReadAllBytes(signedValueFile));
                        res.Add(new StampAnnotEntity(holder, sigDesp.GetSignedInfo()));
                    }
                    finally
                    {
                        Rl.Restore();
                    }
                }
                catch (Exception)
                {
                    // 忽略错误：文件不存在、或签名值不是电子印章（普通电子签名值）
                }
            }
            return res;
        }
        finally
        {
            Rl.Restore();
        }
    }

    // ==================== 关闭 ====================

    /// <summary>关闭文档并删除工作区。</summary>
    public void Close()
    {
        if (_closed) return;
        _closed = true;

        if (!_deleteOnClose) return;
        if (!Directory.Exists(_workDir)) return;

        try
        {
            Directory.Delete(_workDir, recursive: true);
        }
        catch (IOException e)
        {
            throw new IOException("无法删除Reader的工作空间，原因：" + e.Message, e);
        }
    }

    public void Dispose() => Close();
}
