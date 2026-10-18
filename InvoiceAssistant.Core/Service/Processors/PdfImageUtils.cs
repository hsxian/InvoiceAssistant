using OpenCvSharp;

namespace InvoiceAssistant.Core.Service.Processors;

/// <summary>页面位图的轻量判定工具。</summary>
public static class PdfImageUtils
{
    /// <summary>
    /// 是否是空白页（几乎全白）。
    ///
    /// 现实中的审批单常带一张空白尾页，页数统计是准的，但 OCR 它纯属浪费；
    /// 更糟的是空白页识别不出任何文字，会触发每个配置各做一次“裁剪重识别”。
    /// 这里缩到 64×64 后统计非白像素比例，成本可忽略。
    /// </summary>
    public static bool IsNearlyBlank(Mat mat, double nonWhiteRatioThreshold = 0.005)
    {
        if (mat == null || mat.Empty()) return true;

        using var small = new Mat();
        Cv2.Resize(mat, small, new Size(64, 64), interpolation: InterpolationFlags.Area);
        using var gray = new Mat();
        if (small.Channels() == 1)
        {
            small.CopyTo(gray);
        }
        else
        {
            Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
        }
        using var binary = new Mat();
        Cv2.Threshold(gray, binary, 240, 255, ThresholdTypes.BinaryInv);
        var nonWhite = Cv2.CountNonZero(binary);
        return nonWhite < 64 * 64 * nonWhiteRatioThreshold;
    }
}
