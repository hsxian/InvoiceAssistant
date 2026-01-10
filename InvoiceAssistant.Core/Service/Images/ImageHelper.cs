using OpenCvSharp;

namespace InvoiceAssistant.Core.Service.Images;

public static class ImageHelper
{
    public static Mat Binarize(Mat src)
    {
        using Mat gray = new();
        // 转换为灰度图
        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
        // 读取图像，直接以灰度图形式加载
        Mat dst = new();

        // 方法1：简单全局阈值法
        Cv2.Threshold(gray, dst, thresh: 127, maxval: 255, ThresholdTypes.Binary);

        // 方法2：Otsu算法（推荐，可自动计算最佳阈值）
        //Cv2.Threshold(gray, dst, thresh: 0, maxval: 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        // 方法3：自适应阈值（适用于光照不均的图像）
        // Cv2.AdaptiveThreshold(gray, dst, maxValue: 255, adaptiveMethod: AdaptiveThresholdTypes.GaussianC,
        //                         thresholdType: ThresholdTypes.Binary, blockSize: 11, c: 2);

        return dst;
    }

    // 高斯滤波 [1,6](@ref)
    public static Mat RemoveNoiseWithGaussianBlur(Mat source, OpenCvSharp.Size kernelSize, double sigmaX)
    {
        Mat result = new();
        Cv2.GaussianBlur(source, result, kernelSize, sigmaX);
        return result;
    }
    // 使用示例：对含高斯噪声的图像，使用5x5的核，sigma设为1.2
    // var denoisedImage = RemoveNoiseWithGaussianBlur(noisyImage, new Size(5, 5), 1.2);

    // 中值滤波 [1,6](@ref)
    public static Mat RemoveNoiseWithMedianBlur(Mat source, int ksize)
    {
        Mat result = new();
        Cv2.MedianBlur(source, result, ksize);
        return result;
    }
    // 使用示例：对含椒盐噪声的图像，使用3x3的核（ksize=3）
    // var denoisedImage = RemoveNoiseWithMedianBlur(noisyImage, 3);

    // 均值滤波 [5,7](@ref)
    public static Mat RemoveNoiseWithMeanBlur(Mat source, OpenCvSharp.Size kernelSize)
    {
        Mat result = new();
        Cv2.Blur(source, result, kernelSize);
        return result;
    }
    // 使用示例：轻度平滑，使用10x10的核
    // var denoisedImage = RemoveNoiseWithMeanBlur(noisyImage, new Size(10, 10));

    // 双边滤波 [2,6](@ref)
    public static Mat RemoveNoiseWithBilateralFilter(Mat source, int d, double sigmaColor, double sigmaSpace)
    {
        Mat result = new();
        Cv2.BilateralFilter(source, result, d, sigmaColor, sigmaSpace);
        return result;
    }
    // 使用示例：人像美颜或边缘保留去噪常用参数
    // var denoisedImage = RemoveNoiseWithBilateralFilter(noisyImage, 9, 75, 75);

    // 非局部均值去噪 (适用于彩色图像) [1,3](@ref)
    public static Mat RemoveNoiseWithNonLocalMeans(Mat source, float h = 3, int templateWindowSize = 7, int searchWindowSize = 21)
    {
        Mat result = new();
        Cv2.FastNlMeansDenoisingColored(source, result, h, h, templateWindowSize, searchWindowSize);
        return result;
    }
}
