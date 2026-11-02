using Ofd.Core.BasicType;
using Xunit;

namespace Ofd.Tests.BasicType;

/// <summary>ST_* 基础类型直译行为验证（对照 Java 上游语义，含边界与上游怪癖锁定）。</summary>
public class STBasicTypeTests
{
    // ---------------- STBase ----------------

    [Fact]
    public void ToDouble_best_effort_parsing()
    {
        Assert.Equal(12.5, STBase.ToDouble("12.5"));
        Assert.Equal(12, STBase.ToDouble("12px"));      // 尽力解析：过滤掉非数字
        Assert.Equal(0, STBase.ToDouble("abc"));        // 全失败 → 0
        Assert.Equal(0, STBase.ToDouble(""));
        Assert.Equal(8, STBase.ToInt("8x"));            // 尽力解析 → 8（Java 同）
        Assert.Equal(0, STBase.ToInt("abc"));
    }

    [Fact]
    public void JavaDoubleToString_matches_java_Double_toString()
    {
        Assert.Equal("1.0", STBase.JavaDoubleToString(1));
        Assert.Equal("0.0", STBase.JavaDoubleToString(0));
        Assert.Equal("-0.0", STBase.JavaDoubleToString(-0.0));
        Assert.Equal("0.5", STBase.JavaDoubleToString(0.5));
        Assert.Equal("0.001", STBase.JavaDoubleToString(0.001));
        Assert.Equal("1000000.0", STBase.JavaDoubleToString(1000000));
        Assert.Equal("1234567.0", STBase.JavaDoubleToString(1234567));
        Assert.Equal("1.0E7", STBase.JavaDoubleToString(1e7));      // 域外上界 → 科学计数
        Assert.Equal("1.0E-4", STBase.JavaDoubleToString(0.0001));  // 域外下界 → 科学计数
        Assert.Equal("2.5E-5", STBase.JavaDoubleToString(2.5e-5));
        Assert.Equal("NaN", STBase.JavaDoubleToString(double.NaN));
        Assert.Equal("Infinity", STBase.JavaDoubleToString(double.PositiveInfinity));
    }

    // ---------------- ST_ID / ST_RefID ----------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ST_ID_rejects_non_positive(long id)
    {
        Assert.Throws<ArgumentException>(() => new ST_ID(id));
    }

    [Fact]
    public void ST_ID_basic()
    {
        var id = new ST_ID("1000");
        Assert.Equal(1000, id.Id);
        Assert.Equal("1000", id.ToString());
        Assert.Null(ST_ID.GetInstance(null));
        Assert.Null(ST_ID.GetInstance("  "));
        Assert.Equal(5, ST_ID.GetInstance(" 5 ")!.Id);
        Assert.Equal(new ST_ID(7), new ST_ID("7")); // equals/hashCode 按 id 值
    }

    [Fact]
    public void ST_RefID_wraps()
    {
        var r = new ST_ID(10).Ref();
        Assert.Equal(10, r.GetRefId()!.Id);
        Assert.Equal("10", r.ToString());
        Assert.Null(ST_RefID.GetInstance(null));
        Assert.Equal(3, ST_RefID.GetInstance("3")!.GetRefId()!.Id);
    }

    // ---------------- ST_Pos / ST_Box ----------------

    [Fact]
    public void ST_Pos_parse_and_format()
    {
        var p = ST_Pos.GetInstance("0 0")!;
        Assert.Equal(0, p.X);
        Assert.Equal("0 0", p.ToString());
        // fmt 语义：非整数保留 3 位小数（Java %.3f 同）
        Assert.Equal("1.500 2.250", ST_Pos.GetInstance("1.5 2.25")!.ToString());
        // fmt(0.001)="0.00100"：%.8f 后取首个非零位再 +3 位（与 golden 3049 一致）
        Assert.Equal("0.00100 0", ST_Pos.GetInstance("0.0010 0")!.ToString());
        Assert.Null(ST_Pos.GetInstance(null));
        Assert.Null(ST_Pos.GetInstance("1"));          // 1 元素
        Assert.Null(ST_Pos.GetInstance("1  2"));       // 连续空格 → 3 元素（Java 一致）
        Assert.Equal("10 20", new ST_Pos(10, 20).ToString());
    }

    [Fact]
    public void ST_Box_parse_format_clone()
    {
        var b = ST_Box.GetInstance("10 10 50 50")!;
        Assert.Equal(50, b.Width);
        Assert.Equal("10 10 50 50", b.ToString());
        Assert.Equal("10 10", b.GetTopLeftPos().ToString());
        var c = b.Clone().SetWidth(60);
        Assert.Equal(60, c.Width);
        Assert.Equal(50, b.Width); // clone 独立
        Assert.Null(ST_Box.GetInstance("1 2 3"));
        Assert.Null(ST_Box.GetInstance(null));
    }

    // ---------------- ST_Loc ----------------

    [Fact]
    public void ST_Loc_path_semantics()
    {
        var loc = new ST_Loc("/Pages/P1/Content.xml");
        Assert.True(loc.IsRootPath());
        Assert.Equal("Content.xml", loc.GetFileName());
        Assert.Equal("/Pages/P1", loc.Parent());
        Assert.Equal("/Pages", loc.ParentLoc().Parent());

        // Java split 正则语义：尾部空串移除、内部空串保留
        var split = new ST_Loc("/Pages/P1/").Split();
        Assert.Equal(new[] { "", "Pages", "P1" }, split);

        // parts 过滤全部空串
        Assert.Equal(new[] { "Pages", "P1" }, new ST_Loc("/Pages/P1/").Parts());
        Assert.Equal(Array.Empty<string>(), new ST_Loc("").Parts());

        // cat 拼接去重 '/'
        Assert.Equal("/Pages/P1/Res.xml", new ST_Loc("/Pages/P1").Cat("/Res.xml").ToString());
        Assert.Equal("/Pages/P1/Res.xml", new ST_Loc("/Pages/P1/").Cat("Res.xml").ToString());
        Assert.Equal("/Pages/P1", new ST_Loc("/Pages/P1").Cat((string?)null).ToString());

        Assert.True(new ST_Loc("a.ofd").EndWith(".ofd"));
        Assert.Null(ST_Loc.GetInstance("  "));
        Assert.True(new ST_Loc(" ").IsEmpty());

        // equals：支持与 string 比较
        Assert.True(new ST_Loc("a.xml").Equals("a.xml"));
        Assert.True(ST_Loc.Equal(new ST_Loc("a"), "a"));
        Assert.False(ST_Loc.Equal(null, new ST_Loc("a")));
    }

    // ---------------- ST_Array ----------------

    [Fact]
    public void ST_Array_parse_and_format()
    {
        var a = ST_Array.GetInstance("1 2.0 5.0")!;
        Assert.Equal(3, a.Size());
        Assert.Equal("1 2.0 5.0", a.ToString());
        Assert.Equal("1 2.0 5.0", ST_Array.GetInstance("  1\t2.0  5.0 ")!.ToString()); // \s+ 分隔
        Assert.Null(ST_Array.GetInstance(null));
        Assert.Null(ST_Array.GetInstance("null"));   // Java 特判字符串 "null"
        Assert.Null(ST_Array.GetInstance(""));
    }

    [Fact]
    public void ST_Array_double_ctor_uses_java_double_to_string()
    {
        // 上游怪癖：6-double 构造走 Double.toString（整数带 .0），与 params 构造（fmt）不同
        Assert.Equal("1.0 0.0 0.0 1.0 0.0 0.0", new ST_Array(1, 0, 0, 1, 0, 0).ToString());
        // params 构造走 fmt（显式 object[] 以避开 6-double 重载）
        Assert.Equal("1 0 0 1 0 0", new ST_Array(new object?[] { 1d, 0d, 0d, 1d, 0d, 0d }).ToString());
        Assert.Equal("1 2 abc", new ST_Array(1d, "2", "abc").ToString());
    }

    [Fact]
    public void ST_Array_mtx_mul()
    {
        // 单位阵 × 任意 = 任意
        var t = ST_Array.GetInstance("2 0 0 3 5 7")!;
        Assert.Equal(t, ST_Array.UnitCTM().MtxMul(t));

        // translate(10,20) × scale(2,3) = [2 0 0 3 20 60]（行向量约定）
        var tr = ST_Array.GetInstance("1 0 0 1 10 20")!;
        var sc = ST_Array.GetInstance("2 0 0 3 0 0")!;
        Assert.Equal("2 0 0 3 20 60", tr.MtxMul(sc).ToString());

        // 非 6 元素拒绝
        Assert.Throws<ArgumentException>(() => ST_Array.GetInstance("1 2 3")!.MtxMul(t));
    }

    [Fact]
    public void ST_Array_expect_and_get()
    {
        var a = ST_Array.GetInstance("1 2 3")!;
        Assert.Equal(new[] { "1", "2", "3", "", "" }, a.ExpectStrArr(5));
        Assert.Equal(new[] { 1, 2, 3, 0 }, a.ExpectIntArr(4));    // 不足补 0
        Assert.Equal(new double[] { 1, 2, 3 }, a.ExpectArr(3));   // 全部可解析
        Assert.Equal(new double[] { 1, 0 }, ST_Array.GetInstance("1 x")!.ExpectArr(2)); // 解析失败补 0
        Assert.Equal(3, a.GetInt(2));
        Assert.Equal(255, ST_Array.GetInstance("#ff")!.GetInt(0)); // # 十六进制
        Assert.Equal(new[] { 255, 0 }, ST_Array.GetInstance("#ff x")!.ExpectIntArr(2)); // 解析失败补 0
        Assert.Equal(2.0, a.Get(1));
        var c = a.Clone();
        c.Add("9");
        Assert.Equal(4, c.Size());
        Assert.Equal(3, a.Size());
    }
}
