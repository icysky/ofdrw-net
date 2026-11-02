using Ofd.Core.BasicType;

namespace Ofd.Layout.Handler;

/// <summary>
/// 元素渲染完成回调（Java ElementRenderFinishHandler）：获取元素所在页面与生成的 OFD 对象/资源 ID。
/// </summary>
public delegate void ElementRenderFinishHandler(ST_Loc loc, List<ST_ID> contentObjId, List<ST_ID> resIds);
