using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service;

/// <summary>
/// 预加载程序目录 lib/ 下的原生共享库（Linux）。
///
/// 背景：发布包里的 libOpenCvSharpExtern.so 动态依赖 libswscale.so.7（ffmpeg6），
/// Ubuntu 26.04 系统里没有，靠启动脚本 run.sh/run-gui.sh 注入 LD_LIBRARY_PATH 兜底。
/// 一旦不经脚本、直接启动二进制（如文件管理器双击 App），OCR 一初始化 OpenCV 就会抛
/// "The type initializer for 'OpenCvSharp.Internal.NativeMethods' threw an exception"。
/// 这里在首次抽取前把 lib/ 下所有 .so 显式 Load 进进程，glibc 的 dlopen 解析依赖时
/// 会优先命中已加载的同 SONAME 对象，从而摆脱对 LD_LIBRARY_PATH 的依赖。
/// </summary>
public static class NativeLibPreloader
{
    private static int _done;

    public static void Preload(ILogger? logger = null)
    {
        if (Interlocked.Exchange(ref _done, 1) != 0)
        {
            return;
        }
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return;
        }

        var dir = Path.Combine(AppContext.BaseDirectory, "lib");
        if (!Directory.Exists(dir))
        {
            return;
        }

        foreach (var so in Directory.EnumerateFiles(dir, "*.so*"))
        {
            try
            {
                // 依赖缺失时 TryLoad 返回 false 而不是抛异常；此时仍走系统路径解析，
                // 与旧行为一致（由 LD_LIBRARY_PATH 或系统库兜底）。
                if (!NativeLibrary.TryLoad(so, out _))
                {
                    logger?.LogDebug("预加载原生库失败（依赖缺失或已加载）: {Lib}", so);
                }
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "预加载原生库异常: {Lib}", so);
            }
        }
    }
}
