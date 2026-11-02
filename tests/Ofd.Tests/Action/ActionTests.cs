using System.Text;
using Ofd.Core.Action;
using Ofd.Core.Action.ActionType;
using Ofd.Core.Action.ActionType.ActionMovie;
using Ofd.Core.Action.ActionType.ActionGoto;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Outlines;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Core.Action.ActionType.ActionGoto;
using Xunit;

namespace Ofd.Tests.Action;

/// <summary>动作体系（第 14 章）：Actions/CT_Action/Goto/GotoA/URI/Sound/Movie。</summary>
public class ActionTests
{
    [Fact]
    public void Actions_container_round_trip_with_all_action_types()
    {
        var actions = new Actions()
            .AddAction(new CT_Action(EventType.CLICK, new Goto(new CT_Dest().SetType(DestType.XYZ).SetPageID(ST_RefID.GetInstance(5)))))
            .AddAction(new CT_Action(EventType.CLICK, new GotoA("attach-1", true)))
            .AddAction(new CT_Action(EventType.CLICK, new URI("https://example.com", "https://example.com/")))
            .AddAction(new CT_Action(EventType.PO, new Sound(ST_RefID.GetInstance(7))))
            .AddAction(new CT_Action(EventType.DO, new Movie(ST_RefID.GetInstance(8))));

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(actions.Proxy));
        var parsed = new Actions(DomParser.Parse(xml));
        var list = parsed.GetActions();
        Assert.Equal(5, list.Count);

        Assert.Equal(EventType.CLICK, list[0].GetEvent());
        Assert.IsType<Goto>(list[0].GetAction());
        Assert.IsType<GotoA>(list[1].GetAction());
        Assert.IsType<URI>(list[2].GetAction());
        Assert.IsType<Sound>(list[3].GetAction());
        Assert.IsType<Movie>(list[4].GetAction());
        Assert.Equal(EventType.PO, list[3].GetEvent());
    }

    [Fact]
    public void Goto_dest_and_bookmark_are_mutually_exclusive()
    {
        var gotoAct = new Goto(new CT_Dest().SetType(DestType.Fit).SetPageID(ST_RefID.GetInstance(3)));
        var target = (CT_Dest)gotoAct.GetTarget();
        Assert.Equal(DestType.Fit, target.GetType());

        // 切换为目标书签：应移除 Dest
        gotoAct.SetBookmark("第一章");
        Assert.Null(gotoAct.GetOFDElement("Dest"));
        var bm = Assert.IsType<GotoBookmark>(gotoAct.GetTarget());
        Assert.Equal("第一章", bm.GetBookmarkName());

        // 切回目标区域：应移除 Bookmark
        gotoAct.SetDest(new CT_Dest().SetType(DestType.FitH));
        Assert.Null(gotoAct.GetOFDElement("Bookmark"));
        Assert.Equal(DestType.FitH, ((CT_Dest)gotoAct.GetTarget()).GetType());
    }

    [Fact]
    public void Goto_target_requires_exactly_one_child()
    {
        var g = new Goto();
        Assert.Throws<InvalidOperationException>(() => g.GetTarget());
        g.Add(new CT_Dest());
        g.Add(new GotoBookmark("b"));
        Assert.Throws<InvalidOperationException>(() => g.GetTarget());
    }

    [Fact]
    public void CT_Action_set_action_replaces_and_empty_throws()
    {
        var act = new CT_Action();
        Assert.Throws<InvalidOperationException>(() => act.GetAction());

        act.SetEvent(EventType.CLICK).SetAction(new URI("http://a"));
        Assert.IsType<URI>(act.GetAction());
        act.SetAction(new GotoA("a1"));
        Assert.Null(act.GetOFDElement("URI")); // 五类动作互斥
        Assert.IsType<GotoA>(act.GetAction());
        Assert.Equal("CLICK", act.Proxy.Attr("Event"));
        Assert.Throws<ArgumentException>(() => EventTypeExtensions.GetInstance("HOVER"));
    }

    [Fact]
    public void GotoA_and_URI_attributes()
    {
        var g = new GotoA("attach-9");
        Assert.Equal("attach-9", g.GetAttachID());
        Assert.Null(g.GetNewWindow()); // 未设置返回 null（Java 同）
        g.SetNewWindow(false);
        Assert.False(g.GetNewWindow());

        var u = new URI("http://x");
        Assert.Null(u.GetBase());
        u.SetBase("http://base/");
        Assert.Equal("http://base/", u.GetBase());
        u.SetBase(null);
        Assert.Null(u.GetBase());
    }

    [Fact]
    public void Sound_volume_quirks_and_flags()
    {
        var s = new Sound(ST_RefID.GetInstance(6));
        Assert.Equal(6, s.GetResourceID()!.GetRefId()!.Id);

        // 上游怪癖：max(v,0) 后 %100 → 150→50，-5→0，100→0
        s.SetVolume(150);
        Assert.Equal(50, s.GetVolume());
        s.SetVolume(-5);
        Assert.Equal(0, s.GetVolume());
        s.SetVolume(100);
        Assert.Equal(0, s.GetVolume());

        Assert.False(s.GetRepeat());
        Assert.False(s.GetSynchronous());
        s.SetRepeat(true).SetSynchronous(true);
        Assert.True(s.GetRepeat());
        Assert.True(s.GetSynchronous());
        s.SetRepeat(null);
        Assert.False(s.GetRepeat());
    }

    [Fact]
    public void Movie_operator_default_and_removal()
    {
        var m = new Movie(ST_RefID.GetInstance(9)!);
        Assert.Equal(9, m.GetResourceID()!.GetRefId()!.Id);
        Assert.Throws<ArgumentException>(() => m.GetOperator()); // 缺失抛异常（Java 同）
        m.SetOperator(PlayType.Pause);
        Assert.Equal(PlayType.Pause, m.GetOperator());
        Assert.Equal("Pause", m.Proxy.Attr("Operator"));
        m.SetOperator(null);
        Assert.Null(m.Proxy.Attr("Operator"));
    }

    [Fact]
    public void Document_and_outline_accept_actions()
    {
        var actions = new Actions().AddAction(new CT_Action(EventType.DO, new URI("http://ofd")));

        var doc = new Document().SetActions(actions);
        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(doc.Proxy));
        var parsedDoc = new Document(DomParser.Parse(xml));
        Assert.IsType<URI>(parsedDoc.GetActions()!.GetActions()[0].GetAction());

        var outline = new CT_OutlineElem("章节").SetActions(new Actions().AddAction(new CT_Action(EventType.CLICK, new Goto("书签A"))));
        string oxml = Encoding.UTF8.GetString(DomSerializer.Dump(outline.Proxy));
        var parsedOutline = new CT_OutlineElem(DomParser.Parse(oxml));
        var gotoAct = Assert.IsType<Goto>(parsedOutline.GetActions()!.GetActions()[0].GetAction());
        Assert.Equal("书签A", Assert.IsType<GotoBookmark>(gotoAct.GetTarget()).GetBookmarkName());
    }
}
