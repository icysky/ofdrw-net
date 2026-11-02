# Ofd.Net

[ofdrw](https://gitee.com/ofdrw/ofdrw)（Java）的 **.NET 10** 移植：OFD 文件的读取、编辑、排版生成、数字签名、加密、OFD-A 归档转换与文档合并。

> 与上游的详细差异（来源版本、模块映射、新增 / 修改 / 移除）见
> [PORTING-NOTES.md](PORTING-NOTES.md)；许可与第三方声明见 [LICENSE](LICENSE)、[NOTICE](NOTICE)。

- **类级 1:1 移植**：Java 侧 454 个类全部对应实现（`pkg` 中 3 个 DOM 工具类按等价能力合并）
- **测试**：441 个测试（本地自备仿宋字体时全绿；单元 + 端到端 + 结构签名 golden + Java 对拍）
  > 其中 2 个排版测试（`OpticalKerningTests`、`CanvasFontParsingTests`）需要自备仿宋字体；
  > 公开仓库不包含该字体，未放置字体时这两个测试会失败，见
  > [src/Ofd.Font/Assets/FangSong/README.md](src/Ofd.Font/Assets/FangSong/README.md)。
- **无 Java 运行时依赖**：加解密、签名、字体度量、图像处理均使用 .NET 生态（BouncyCastle / SkiaSharp）

## 模块

| 工程 | 对应 Java 模块 | 职责 |
|---|---|---|
| `Ofd.Core` | `ofdrw-core` | OFD 数据模型（文档结构、页面、图元、字体、签名、注释、动作） |
| `Ofd.Pkg` | `ofdrw-pkg` | 虚拟容器（`OFDDir`/`DocDir`/`PagesDir`/`ResDir`）、打包与解包 |
| `Ofd.Reader` | `ofdrw-reader` | `OFDReader`、资源定位、内容抽取、关键字检索、旧版读取器 |
| `Ofd.Crypto` | `ofdrw-crypto` | 加密/解密（口令、证书、FEK） |
| `Ofd.Sign` | `ofdrw-sign` | 数字签名/签章与验签（GB/T 35275、SES V1/V4/V5） |
| `Ofd.Gm` | `ofdrw-gm` | 国密结构（SM2/SM3、证书、SES 结构） |
| `Ofd.Font` | `ofdrw-font` | 字体度量（SkiaSharp）、宽度映射表 |
| `Ofd.Layout` | `ofdrw-layout` | 排版引擎：Div 树 → 分段 → 虚拟页面 → 页面图元 |
| `Ofd.Archive` | `ofdrw-archive` | OFD-A 合规检查（23 条规则）与转换（23 个处理器） |
| `Ofd.Tool` | `ofdrw-tool` | 文档合并、页面删除、裸文档构造 |

## 快速上手

### 1. 排版生成 OFD

```csharp
using Ofd.Layout;
using Ofd.Layout.Element;

using var doc = new OFDDoc("out.ofd");
doc.Add(new Paragraph("Hello OFD", 5));
doc.Add(new Div(100, 40).SetBackgroundColor(230, 240, 255));
// Dispose / Close 时完成布局并打包
```

### 2. 读取内容

```csharp
using var reader = new OFDReader("out.ofd");
int pages = reader.GetNumberOfPages();
var page  = reader.GetPage(1);          // 页面对象（图层 → 图元）
```

### 3. 关键字检索

```csharp
using Ofd.Reader.Keyword;

var hits = KeywordExtractor.GetKeyWordPositionList(reader, "关键词");
// hits[i].GetPage() / GetBox()（单位 mm，含跨行断字匹配）
```

### 4. OFD-A 检查与转换

```csharp
using Ofd.Archive.Check;
using Ofd.Archive.Convert;

var violations = new OFDArchiveChecker().Check("in.ofd");   // 23 条规则

new OFDArchiveConverter().Convert("in.ofd", "out-ofda.ofd"); // 23 个去技术化处理器
```

加密文档需先配置解密器：

```csharp
new OFDArchiveConverter()
    .AddDecryptor(new Ofd.Crypto.Decryptor.UserPasswordDecryptor("张三", "12345678"))
    .Convert("encrypted.ofd", "out-ofda.ofd");
```

### 5. 签名与验签

```csharp
using Ofd.Sign;
using Ofd.Sign.SignContainer;
using Ofd.Sign.Verify;
using Ofd.Sign.Verify.Container;

using (var reader = new OFDReader("in.ofd"))
using (var signer = new OFDSigner(reader, "signed.ofd"))
    signer.SetSignContainer(new GBT35275DSContainer(cert, privateKey)).ExeSign();

using (var reader = new OFDReader("signed.ofd"))
using (var validator = new OFDValidator(reader))
{
    validator.SetValidator(new GBT35275ValidateContainer());
    validator.ExeValidate();
}
```

### 6. 合并与页面删除

```csharp
using Ofd.Tool.Merge;

using (var merger = new OFDMerger("merged.ofd"))
{
    merger.Add("a.ofd");            // 全部页面
    merger.Add("b.ofd", 2, 3);      // 指定页面
}

using (var deleter = new OFDPageDeleter("merged.ofd", "trimmed.ofd"))
    deleter.Delete(0);              // 索引从 0 起
```

## 构建与测试

```powershell
# 构建（单进程，避免多进程 Roslyn 占用大量内存）
dotnet build tests\Ofd.Tests\Ofd.Tests.csproj -c Release -m:1 -p:UseSharedCompilation=false

# 分批测试（测试已串行化：xunit.runner.json 关闭并行）
dotnet test -c Release --no-build --filter "FullyQualifiedName~Ofd.Tests.Layout"
dotnet test -c Release --no-build --filter "FullyQualifiedName~Ofd.Tests.Archive|FullyQualifiedName~Ofd.Tests.Integration"
```

> **内存提示**：测试集包含 SkiaSharp 图像/字体用例，一次性跑全量在低内存机器上可能吃紧。
> 建议按命名空间分批执行；仓库内的 `tools/Smoke` 提供不启动 testhost 的最小验证（约 60 MB）。

### 结构签名 golden

`tests/Ofd.Tests/Golden/LayoutArchiveGoldenTests.cs` 把产物压成稳定文本与
`tests/Ofd.Tests/TestData/layout-archive-golden.txt` 比对（页面图层、图元边界、路径算子、转换后条目）。

修改产物结构后重建基线：

```powershell
$env:OFDNET_UPDATE_GOLDEN="1"
dotnet test -c Release --no-build --filter "FullyQualifiedName~LayoutArchiveGoldenTests"
```

### 一键 CI（推荐）

```powershell
pwsh tools\ci.ps1              # 单进程构建 + 5 批测试 + 1.5GB 内存看门狗
pwsh tools\ci.ps1 -SkipBuild   # 已构建过，只跑分批测试
```

> 测试集含 SkiaSharp 图像/字体用例，历史上并行跑全量曾导致系统内存耗尽。
> `tools/ci.ps1` 把「单进程构建 + 分批 + 看门狗杀进程」固化下来，避免复现事故。

### 压力测试

```powershell
dotnet run --project tools\Stress -c Release -- 500 --convert
```

实测（含每 3 页一张图片 + OFD-A 转换）：

| 页数 | 峰值内存 | 打包耗时 | 转换结果 |
|---:|---:|---:|---|
| 120 | 82 MB | 295 ms | 120 页，OFD-A |
| 500 | 91 MB | 704 ms | 500 页，OFD-A |

内存基本不随页数增长（排版阶段恒定约 31 MB）。

### 真实样本转换

`TestData/real-*.ofd` 来自 ofdrw 测试资源（`helloworld`/`namespace_no_std`/`signedFile`/`allprotected`），
`RealSampleConversionTests` 验证：转换后页数不变、`DocType=OFD-A`、签名类违规消失、文档可正常读取。

### 发票演示（自定义字体 + 双行水印 + 印章图片 + 转换）

```powershell
dotnet run --project tools\InvoiceDemo -c Release -- --out out\invoice
```

依赖本机资源（可用参数覆盖）：
- 字体：`--font-dir`（默认 `E:\NewProjects\大庆\backend\bin\ManualRun\fonts`）
- 印章图：`--seal`（默认 `...\印章\stamp.png`）

产物（`out\invoice\`）：
- `invoice-plain.ofd`：排版结果（默认仿宋，字体随文嵌入；仿宋需自备，缺失时回退系统宋体）
- `invoice-watermark.ofd`：45° 全屏双行水印
- `invoice-sealed.ofd`：SES V4 电子签章（印章图片来自本机）
- `invoice-ofda.ofd`：转换为 OFD-A（签章被移除）

> 水印文本支持 `\n` 分行：`WatermarkDrawer` 会按行高逐行绘制，平铺点数 × 行数 = TextCode 数。

**电子签章可见的三个必要条件**（缺一不可）：

1. 调用 `signer.AddApPos(new NormalStampPos(页, x, y, w, h))` —— 否则产物只有 `Seal.esl`，
   没有 `<ofd:StampAnnot>`，阅读器不知道画在哪里
2. `Seal.esl` 内图片声明尺寸（mm）与 `StampAnnot.Boundary` 一致（物理印章通常 40×40mm）
3. 印章图片能被阅读器解码：**PNG 带 alpha 可用**（实测数科正常显示），
   若目标阅读器不认 alpha，再用 `--flatten` 转成不透明 RGB 兜底

### 字体策略（内置 Noto Serif SC + 可选自备仿宋）

**库级默认字体**：`Font.GetDefault()` 优先使用 `src/Ofd.Font/Assets/FangSong/FangSong_GB2312.ttf`
（构建时复制到输出 `Assets/FangSong`）。该字体为商业字体，**不随仓库分发**，需自行获取
（见 [该目录 README](src/Ofd.Font/Assets/FangSong/README.md)）；文件缺失时回退系统宋体。
字体随文内嵌：

```csharp
var font = Ofd.Font.BuiltinFonts.FangSong();          // 缓存单例；缺失返回 null
var serif = Ofd.Font.BuiltinFonts.NotoSerif();        // Noto Serif SC Regular
var bold = Ofd.Font.BuiltinFonts.NotoSerif(bold: true);
```

| 字体 | 文件 | 体积 | 字形 | 适用 |
|---|---|---:|---:|---|
| 仿宋 | `Assets/FangSong/FangSong_GB2312.ttf` | 3.8 MB | 7,541 | 中文正文，体积小（需自备，默认优先） |
| **Noto Serif SC** | `Assets/NotoSerif/NotoSerifSC-{Regular,Bold}.ttf` | 14.16 MB ×2 | 30,928 | **数字+字母混排**（税号、编号、金额），拉丁为比例字宽 |

> Noto Serif SC 由 Windows 的 `NotoSerifSC-VF.ttf`（可变字体）固化出的静态实例（wght 400/700），
> 避免阅读器对可变字体兼容性差；仅缺扩展 B 区 `𠮷`。
>
> 仓库仅内置可自由再分发的 Noto Serif SC（SIL OFL 1.1）；仿宋为商业字体，需使用者自备。

**体积与兼容性的取舍（已决策）**：本库默认**内嵌完整字体**，单份文档因此比"只写字体名"的实现大得多
（仿宋 2.4 MB / Noto Serif SC 18 MB）。这是有意的取舍：

| | 只写字体名（如部分真实发票） | 内嵌字体（本库默认） |
|---|---|---|
| 体积 | 几十 KB | 2.4 ~ 18 MB |
| 跨平台显示 | 依赖阅读器所在机器装有该字体，Linux 无字体→方框 | 处处一致 |
| OFD-A 合规 | 检查器报 `FONT_SUBSET`（要求嵌入） | 通过 |
| 排版确定性 | 差（字体版本不同则宽度不同） | 强 |

> 需要极小体积时可改用「不嵌字体」写法（`Font` 不带字体文件），但要自行承担上述代价；
> 不建议用字体子集：实测数科等阅读器渲染异常（字符叠加）。

**光学字距（可选，默认关闭）**：`DrawContext.OpticalKerning = true`（演示：`--optical-kerning`）
让 ASCII 按「字形墨迹宽 + 字号 12% 间隙」推进，修正等宽字体中数字/字母间隙不均：

| 仿宋 4mm | 默认 | 光学字距 |
|---|---|---|
| 数字 `0/1` | 2.000 | 1.61 ~ 1.81 |
| 字母 `M/A/X` | 2.000 | 2.17 ~ 2.40 |
| 字间间隙 | 0.26 ~ 0.77（不均） | **统一 0.48** |

> 目前仅作用于画布文本（`DrawContext.FillText`），段落文本尚未覆盖。
> **实测观感**：Noto Serif SC（比例字宽）> 仿宋默认 > 光学字距；光学字距仅作为可选手段保留。

> **踩坑记录**：字体族含空格时（如 `4mm Noto Serif SC`），旧解析器从右取 token 导致字号回退为 1，
> 表现为「字体非常小」。已改为从左扫描并支持带空格的字体族（`CanvasFontParsingTests` 锁定）。

**为什么发票演示默认用 Noto Serif SC**：仿宋的 ASCII 是等宽 0.5em 设计（`advance=2.000mm`@4mm），
而 `M` 的墨迹宽达 1.922mm → 紧跟数字时只剩 0.26mm 间隙，视觉上「字母挤在数字后面」。
Noto Serif SC 的拉丁为比例字宽（`M`=3.9mm、`1`=1.88mm），间隙分布均匀。

**体积是固定成本，与文字量无关**（实测 4mm 字号、14MB 字体）：

| 文档 | 页数 | 产物体积 |
|---|---:|---:|
| 1 页 | 1 | 8,371 KB |
| 200 页 | 200 | 8,481 KB |

→ 多 199 页仅增加 **110 KB**（约 0.55 KB/页）；字体文件每份文档只嵌入一次。
同一字体被多个 `Font` 定义引用时也只嵌入一次（按文件去重）。

- **跨平台确定性**：Linux 容器通常没装中文字体，内置/自备字体保证生成结果一致（不依赖 `fc-list`）
- **回退链**：`Font.GetDefault()` = 自备仿宋 → 系统宋体；Noto Serif SC 通过 `BuiltinFonts.NotoSerif()` 显式指定
- **生僻字预案**：仿宋覆盖 GB2312（6763 汉字），`¥ ㎡ ℉ 𠮷 龘 犇 垚` 等不在其中；
  遇到时换覆盖更全的**完整**字体（如 Noto Serif SC），**不要用子集**（见下表）

```powershell
# 指定任意字体文件（会整包嵌入产物）
dotnet run --project tools\InvoiceDemo -c Release -- `
  --body-font "E:\...\某字体.ttf" --title-font "E:\...\某字体.ttf"
```

> 字体版权归原厂商所有，随包分发（尤其对外发布 NuGet）前请确认已获授权。

**为什么不用 Noto / 不做字体子集化**（实测记录，避免重复踩坑）：

| 方案 | 体积 | 数科版式渲染 |
|---|---:|---|
| 完整 Noto Sans SC | 19 MB/份 | ❌ 字符叠加 |
| Noto GBK 子集 | 8.6 MB/份 | ❌ 字符叠加 |
| Noto GB2312 子集 | 2.8 MB/份 | ❌ 字符叠加 |
| Noto 子集（`--retain-gids`） | 8.5 MB/份 | ❌ 字符叠加 |
| 仿宋（内置，整包） | 2.4 MB/份 | ✅ 正常 |

排查结论：生成物除了字体文件之外**逐字节一致**（TextObject 数量、X/Y、`DeltaX`、水印 `CTM`/`Alpha` 全同），
子集字体也不缺字、`unitsPerEm`/`hhea`/`OS/2` 与原版一致；差异只在字体文件被阅读器解析的方式上。
因此**当前不使用 Noto、也不做子集化**，统一走系统字体/常见中文字体整包嵌入。

> 若后续需要确定性排版或更小体积：优先换体积较小的**完整**中文字体（如仿宋 3.8 MB），
> 并在目标阅读器（数科/OFD 阅读器等）实测后再上线；`tools/fonts/` 下的 `inspect_font.py`
> （字符覆盖检查）与 `make_subsets.py`（生成子集，**仅供实验**）保留。

字体覆盖检查（选字体时用）：

```powershell
python tools\fonts\inspect_font.py "字体.ttf" [...]
```

### 印章类型（`SES_ESPropertyInfo.type`）取值与用法

`Seal.esl` 中的 `eSealInfo.property.type` 是 ASN.1 INTEGER，**只声明印章用途**：
不参与摘要计算、不影响验签结果，阅读器「印章属性」面板里显示的「类型」就是它。

| 值 | 含义 | 依据 / 实测 |
|---:|---|---|
| 1 | 单位印章（公章） | GM/T 0031 系列定义 |
| 2 | 个人印章（私章） | GM/T 0031 系列定义 |
| 3 | 行业/厂商扩展 | ofdrw 官方样本 `V4RideRight.ofd` 实测为 **3** |
| 4 | 合同专用章（常见约定） | ofdrw 测试印章 `SESealData` 实测为 **4**，名称为「测试单位名称 合同专用章」 |

> 3、4 属行业实践中的扩展值，具体语义由**发章系统**约定，不同厂商可能不一致；
> 1、2 有规范定义，可放心使用。

**C# 设置方式**：

```csharp
var property = new V4.SES_ESPropertyInfo()
    .SetType(new DerInteger(4))                      // ← 印章类型
    .SetName(new DerUtf8String("合同专用章"))         // 名称最好与类型呼应
    .SetCertListType(V4.SES_ESPropertyInfo.CertListType)
    .SetCertList(new V4.SES_CertList(...))
    .SetCreateDate(new DerGeneralizedTime("20260101000000Z"))
    .SetValidStart(new DerGeneralizedTime("20260101000000Z"))
    .SetValidEnd(new DerGeneralizedTime("20270101000000Z"));
```

**命令行（发票演示）**：

```powershell
dotnet run --project tools\InvoiceDemo -c Release -- --seal-type 4   # 合同专用章
dotnet run --project tools\InvoiceDemo -c Release -- --seal-type 1   # 单位公章
```

**与既有系统对齐**：先取一份对方能正常显示的 OFD，用
`dotnet run --project tools\SignProbe -- inspect <文件.ofd>` 查看其
`印章类型`，照该值设置即可；同时保证印章名称与类型语义一致（名称含「合同专用章」就用 4）。

**改这个值的影响范围**：印章自签名、文档签名值、验签结果**都不变**，
唯一变化是阅读器属性面板里的类型展示。因此不必为了让验签通过而调整它。

可选参数：`--seal <图片>`、`--font-dir <目录>`、`--body-font/--title-font <ttf>`、`--no-subset`、
`--out <目录>`、`--seal-alpha <0~1>`（按比例调淡印章 alpha）、
`--flatten`（铺白底输出 RGB）、`--wm-font-size <mm>`、`--wm-interval-x/y <mm>`、
`--seal-type <n>`（印章类型，默认 4）、`--ride`（3 页合同骑缝章）。
默认：印章 alpha 0.70、水印 4.5mm、间距 62×40mm（45 个平铺点/页）。

> OFD 签章本身**没有**透明度字段（`StampAnnot`/`Seal.esl` 均无 alpha 属性），
> 要控制浓淡只能预先把印章 PNG 的 alpha 通道调低；`--seal-alpha 0.70` 是当前默认值。

工具链已内置处理：`tools/InvoiceDemo` 用 `PrepareSealPng` 统一 40mm 并按需缩放 alpha；
`tools/SignProbe inspect` 可查看印章证书、类型、图片格式/尺寸与自签名有效性。

## 与 Java 版本的对拍

> **仓库范围说明**：`tools/javaprobe`、`tools/SignProbe`、`tools/InvoiceDemo`、`tools/fonts`
> 属于开发仓库（与 `ofdrwJava/` 参考源码同仓），**不在 `ofdrwNet` 独立发布包内**；
> 本目录只自带 `tools/Smoke`。独立发布时，Java 对拍基线已随
> `tests/Ofd.Tests/TestData/java-*-golden.txt` 提供，可直接复核。

仓库内置 `tools/javaprobe/Probe.java`：用 Maven 构建的 ofdrw（JDK 21）生成同场景产物，导出结构签名作为基线
（`tests/Ofd.Tests/TestData/java-*-golden.txt`），由 `LayoutArchiveGoldenTests` 中的对拍测试逐字段比对。

| 场景 | 结论 |
|---|---|
| 排版（Times 字体、5mm/4mm 文本） | ✅ 完全一致：边界 `31.7,25.4,24.719,7` / `31.7,32.4,18.105,6`、字号、图层结构 |
| 注释（`addAnnotation`） | ✅ 完全一致：`Annots/Annotations.xml`、分页注释条目、`type/visible/readOnly/Boundary` |
| 水印（`addWatermark`） | ✅ 完全一致：平铺块数 `63`（7×9）、外观边界 `0,0,260,347` |
| SES V4 签章 | ✅ 完全一致：`Seal.esl` + `Signature.xml`/`SignedValue.dat`、`type=Seal`、`references=6`、`ProviderName=ofdrw-sign` |
| **互操作（双向）** | ✅ Java 签 → C# 验（GB/T 35275 与 SES V4，夹具 `TestData/java-signed.ofd`、`java-seal.ofd`）；✅ C# 签 → Java 验（`tools/SignProbe` 生成样本，Java 侧 `Probe validate` 返回 `VALIDATE-OK`） |
| 加密（口令） | ✅ 完全一致：`.dat` 条目命名、`EncryptScope=All`、`Provider=ofdrw-crypto`、`Company=ofdrw.org`、`DecryptSeedLoc`/`EntriesMapLoc` |
| 数字签名（GB/T 35275） | ✅ 完全一致：`Signs/` 三件套、`type=Sign`、`ProviderName=ofdrw-sign`、`SignatureMethod`、`references=5`、`MaxSignId=s001` **← 对拍发现并修正了 `MaxSignId` 被错写成属性** |
| OFD-A 归档转换 | ⚠️ **Java 侧不可用**（两处缺陷）：① 缺 `DocumentRes.xml`/`PublicRes.xml` 时 `ResourcePlacementHandler` 抛 `FileNotFoundException`；② 白名单 DFS 只匹配带引号的属性值，而 `DocRoot` 是元素文本 → 转换产物**只剩 `OFD.xml`（1 个条目，不可读）**。C# 版两者均已修复，此环节以 C# 行为为准 |

一键重建全部 Java 基线（Maven 构建 + 编译探针 + 拆分写回）：

```powershell
pwsh tools\javaprobe\run.ps1
```

手动复现（分步）：

```powershell
mvn -B -ntp -DskipTests -Dmaven.test.skip=true -Dgpg.skip=true -pl ofdrw-layout,ofdrw-archive -am install
mvn -B -ntp -q -pl ofdrw-archive dependency:build-classpath -Dmdep.outputFile=cp.txt
javac -encoding UTF-8 -cp "<cp>;ofdrw-archive/target/classes" -d classes tools\javaprobe\Probe.java
java -cp "<cp>;ofdrw-archive/target/classes;classes" Probe out
```

### 双向互操作验证

```powershell
# 1) C# 生成签名/签章样本
dotnet run --project tools\SignProbe -c Release -- sign tools\javaprobe\out\cs-signed.ofd
dotnet run --project tools\SignProbe -c Release -- seal tools\javaprobe\out\cs-seal.ofd

# 2) Java 侧验签（自动按 Type 选择 GBT35275 / SESV4 验签器）
java -cp "<cp>;tools\javaprobe\classes" Probe validate tools\javaprobe\out\cs-signed.ofd
java -cp "<cp>;tools\javaprobe\classes" Probe validate tools\javaprobe\out\cs-seal.ofd
```

## 已知差异与限制

| 项 | 说明 |
|---|---|
| **JBIG2 解码** | SkiaSharp 不内置 JBIG2 解码器，`ImageUtils.ReadJb2` 抛 `NotSupportedException` |
| **SES 电子签章深度解析** | `DLOFDReader`（旧版、已标注过时）不解析 STAMP 结构，签章注释返回空列表 |
| **字体度量** | 默认优先自备仿宋；缺失时回退系统宋体。Noto Serif SC（OFL）随仓库分发；仿宋需自备 |
| **仿宋的 ASCII 字宽** | 等宽 0.5em，字母（如 `M`）墨迹接近满格 → 数字后紧跟字母时视觉偏挤；可改用 Noto Serif SC，或开 `OpticalKerning`（仅画布文本） |
| **字体子集化** | 数科等阅读器对 Noto 子集字体渲染异常（字符叠加），已回退为系统字体/完整字体；`tools/fonts/make_subsets.py` 仅供实验（开发仓库工具，不在独立发布包内） |
| **归并文档的 DocType** | `OFDMerger` 产出为新建文档（`DocType=OFD`），需再转换一次才符合 OFD-A |
| **示例印章的自签名** | ofdrw 测试资源 `SESealData.DER`（证书 2018~2019、名称「测试单位名称 合同专用章」）**印章自签名无效**，数科版式会提示「签章无效」；该印章仅用于结构对拍。C# 侧 `SignProbe`/`SignTests` 自建印章自签名有效（证书为自签，阅读器可能提示「证书不受信」） |

## 目录结构

```
src/
  Ofd.Core/  Ofd.Pkg/  Ofd.Reader/  Ofd.Crypto/  Ofd.Sign/
  Ofd.Gm/    Ofd.Font/ Ofd.Layout/  Ofd.Archive/ Ofd.Tool/
tests/Ofd.Tests/           单元 / 集成 / golden 测试
tools/Smoke/               最小控制台验证程序（不启动 testhost）
```

## 许可与第三方声明

- 本项目以 **Apache License 2.0** 发布，全文见 [LICENSE](LICENSE)。
- 本项目是 [ofdrw](https://gitee.com/ofdrw/ofdrw)（Apache License 2.0）的 .NET 移植
  （原始仓库：[GitHub](https://github.com/ofdrw/ofdrw)）。
  上游归属声明、修改说明（Apache-2.0 第 4 条）见 [NOTICE](NOTICE)。
- 依赖组件与内置字体的许可证、版权声明见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)：
  - BouncyCastle.Cryptography（MIT）、SkiaSharp（MIT）；
  - Noto Serif SC（SIL OFL 1.1），许可证随字体存放于
    `src/Ofd.Font/Assets/NotoSerif/OFL.txt` 并复制到输出目录。
- **商业字体提示**：`FangSong_GB2312.ttf`（Copyright GreatWall Computer Co. 1994）
  不附带再分发许可，不得提交到公开仓库或随产品分发，仓库已移除该文件。
  需要时请自行获取并放到 `src/Ofd.Font/Assets/FangSong/`，
  说明见 [该目录 README](src/Ofd.Font/Assets/FangSong/README.md)。
- `ofdrw` / `OFDRW` 为上游项目名称，本项目不使用其名称暗示官方认可或背书。
