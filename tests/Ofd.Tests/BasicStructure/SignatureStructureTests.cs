using System.Text;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Core.Signatures;
using Ofd.Core.Signatures.Appearance;
using Ofd.Core.Signatures.Range;
using Ofd.Core.Signatures.Sig;
using Xunit;
using RegSignature = Ofd.Core.Signatures.Signature;
using SigSignature = Ofd.Core.Signatures.Sig.Signature;

namespace Ofd.Tests.BasicStructure;

/// <summary>签名结构（第 18 章）：签名列表/签名描述文件/签名范围/外观。</summary>
public class SignatureStructureTests
{
    [Fact]
    public void Signatures_registry_round_trip()
    {
        var signatures = new Signatures()
            .SetMaxSignId("s2")
            .AddSignature(new RegSignature()
                .SetId("s1")
                .SetType(SigType.Seal)
                .SetBaseLoc(new ST_Loc("Doc_0/Signs/Sign_0/Signature.xml")))
            .AddSignature(new RegSignature()
                .SetId("s2")
                .SetRelative("s1")
                .SetBaseLoc(new ST_Loc("Doc_0/Signs/Sign_1/Signature.xml")))
            .AddSignature(null);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(signatures.Proxy));
        var parsed = new Signatures(DomParser.Parse(xml));

        Assert.Equal("s2", parsed.GetMaxSignId());
        var list = parsed.GetSignatures();
        Assert.Equal(2, list.Count);
        Assert.Equal("s1", list[0].GetId());
        Assert.Equal(SigType.Seal, list[0].GetType());      // 缺省 Seal
        Assert.Equal("s1", list[1].GetRelative());
        Assert.Equal("Doc_0/Signs/Sign_1/Signature.xml", list[1].GetBaseLoc().ToString());

        // 必选校验
        Assert.Throws<InvalidOperationException>(() => new RegSignature().GetId());
        Assert.Throws<InvalidOperationException>(() => new RegSignature().GetBaseLoc());
        Assert.Throws<ArgumentException>(() => new RegSignature().SetId(" "));
        Assert.Throws<ArgumentException>(() => new RegSignature().SetBaseLoc(null!));
        // MaxSignId 为 null 时删除元素
        parsed.SetMaxSignId(null);
        Assert.Null(parsed.GetMaxSignId());
    }

    [Fact]
    public void Signature_description_full_structure()
    {
        var references = new References()
            .SetCheckMethod(CheckMethod.SM3)
            .AddReference(new Reference(new ST_Loc("Doc_0/Document.xml"), new byte[] { 1, 2, 3 }))
            .AddReference(new Reference(new ST_Loc("Doc_0/Pages/Page_0/Content.xml"), new byte[] { 9, 8 }));

        var signedInfo = new SignedInfo()
            .SetProvider(new Provider()
                .SetProviderName("OFDRW")
                .SetVersion("2.0")
                .SetCompany("某公司")
                .SetProtocolVer("1.0")
                .SetExtendData(new byte[] { 7, 6, 5 }))
            .SetSignatureMethod("1.2.156.10197.1.501")
            .SetSignatureDateTime("2026-10-21 10:00:00")
            .SetParameters(new Parameters().AddParameter("key", "string", "value").AddParameter("key2", "v2"))
            .SetReferences(references)
            .AddStampAnnot(new StampAnnot()
                .SetId("s1")
                .SetPageRef(ST_RefID.GetInstance(101))
                .SetBoundary(new ST_Box(10, 10, 40, 40))
                .SetClip(new ST_Box(0, 0, 40, 40)))
            .SetSeal(new Seal(new ST_Loc("Doc_0/Signs/Seal.esl")).SetImageLoc(new ST_Loc("Doc_0/Signs/seal.png")));

        var sig = new SigSignature()
            .SetSignedInfo(signedInfo)
            .SetSignedValue(new ST_Loc("Doc_0/Signs/Sign_0/SignedValue.dat"));

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(sig.Proxy));
        var parsed = new SigSignature(DomParser.Parse(xml));
        var info = parsed.GetSignedInfo();

        var provider = info.GetProvider()!;
        Assert.Equal("OFDRW", provider.GetProviderName());
        Assert.Equal("2.0", provider.GetVersion());
        Assert.Equal("某公司", provider.GetCompany());
        Assert.Equal("1.0", provider.GetProtocolVer());
        Assert.Equal(new byte[] { 7, 6, 5 }, provider.GetExtendData()); // Base64 往返

        Assert.Equal("1.2.156.10197.1.501", info.GetSignatureMethod());
        Assert.Equal("2026-10-21 10:00:00", info.GetSignatureDateTime());

        var parameters = info.GetParameters()!;
        Assert.Equal("value", parameters.GetParameter("key")!.GetValue());
        Assert.Equal("string", parameters.GetParameter("key")!.GetType());
        Assert.Equal("v2", parameters.GetParameter("key2")!.GetValue());

        var refs = info.GetReferences();
        Assert.Equal("SM3", refs.GetCheckMethod());
        Assert.True(refs.HasFile("Doc_0/Document.xml"));
        Assert.False(refs.HasFile("NotExist.xml"));
        Assert.Equal(new byte[] { 1, 2, 3 }, refs.GetReferences()[0].GetCheckValue());

        var stamp = Assert.Single(info.GetStampAnnots());
        Assert.Equal("s1", stamp.GetId());
        Assert.Equal(101, stamp.GetPageRef().GetRefId()!.Id);
        Assert.Equal("10 10 40 40", stamp.GetBoundary()!.ToString());
        Assert.Equal("0 0 40 40", stamp.GetClip()!.ToString());

        var seal = info.GetSeal()!;
        Assert.Equal("Doc_0/Signs/Seal.esl", seal.GetBaseLoc().ToString());
        Assert.Equal("Doc_0/Signs/seal.png", seal.GetImageLoc()!.ToString());

        Assert.Equal("Doc_0/Signs/Sign_0/SignedValue.dat", parsed.GetSignedValue()!.ToString());
    }

    [Fact]
    public void Parameters_same_name_replaces()
    {
        var ps = new Parameters()
            .AddParameter("a", "1")
            .AddParameter("b", "2")
            .AddParameter("a", "3"); // 同名覆盖
        Assert.Equal(2, ps.GetParameters().Count);
        Assert.Equal("3", ps.GetParameter("a")!.GetValue());

        var removed = ps.RemoveParameter("b")!;
        Assert.Equal("2", removed.GetValue());
        Assert.Null(ps.GetParameter("b"));
        Assert.Null(ps.RemoveParameter(null));
    }

    [Fact]
    public void Required_checks_and_null_removals()
    {
        Assert.Throws<InvalidOperationException>(() => new SignedInfo().GetReferences());
        Assert.Throws<ArgumentException>(() => new SignedInfo().SetReferences(null!));
        Assert.Throws<InvalidOperationException>(() => new SigSignature().GetSignedInfo());
        Assert.Throws<ArgumentException>(() => new SigSignature().SetSignedInfo(null!));

        var provider = new Provider();
        Assert.Throws<InvalidOperationException>(() => provider.GetProviderName());
        Assert.Empty(provider.GetExtendData());

        var seal = new Seal();
        Assert.Throws<InvalidOperationException>(() => seal.GetBaseLoc());
        Assert.Null(seal.GetImageLoc());

        var stamp = new StampAnnot();
        Assert.Throws<InvalidOperationException>(() => stamp.GetId());
        Assert.Throws<InvalidOperationException>(() => stamp.GetPageRef());
        Assert.Throws<ArgumentException>(() => stamp.SetBoundary(null!));

        // Parameter.SetType(null) 删除属性（上游误传变量 bug 已修）
        var p = new Parameter("n", "t", "v");
        p.SetType(null);
        Assert.Null(p.Proxy.Attr("Type"));

        // References 检查值 Base64
        var reference = new Reference(new ST_Loc("a.xml"), new byte[] { 255 });
        Assert.Equal(new byte[] { 255 }, reference.GetCheckValue());
        Assert.Throws<ArgumentException>(() => new Reference().SetCheckValue(Array.Empty<byte>()));
    }
}
