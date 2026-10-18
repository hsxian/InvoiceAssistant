using System.Collections.Generic;

#if STUB_V3
namespace Sdcb.PaddleOCR.Models.LocalV3;
#else
namespace Sdcb.PaddleOCR.Models.LocalV4;
#endif

/// <summary>
/// 与真实包同名同命名空间的占位类型，只为满足类型解析。
/// All 恒为空：本工具只用 V5 模型，不会去读 V3/V4 的模型资源。
/// </summary>
public static class KnownModels
{
    public static readonly HashSet<string> All = new HashSet<string>();
}
