using System.Text.RegularExpressions;
using InvoiceAssistant.Core.Data;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Local;

namespace InvoiceAssistant.Core.Service.ExtractUnits;

public class PaddleOCRImageInfoExtractUnit : IImageInfoExtractUnit
{
    readonly PaddleOcrAll paddleOcrAll;
    private readonly ILogger<PaddleOCRImageInfoExtractUnit> logger;

    public PaddleOCRImageInfoExtractUnit(ILogger<PaddleOCRImageInfoExtractUnit> logger)
    {
        FullOcrModel model = LocalFullModels.ChineseV5;
        paddleOcrAll = new(model, PaddleDevice.Mkldnn())
        {
            AllowRotateDetection = true, /* 允许识别有角度的文字 */
            Enable180Classification = false, /* 允许识别旋转角度大于90度的文字 */
        };
        this.logger = logger;
    }

    public void Dispose()
    {
        paddleOcrAll.Dispose();
    }
    private Rect GetRoiRect(Mat mat, ExtractMetadata metadata)
    {
        var rect = metadata.ScaleOrAbsolutePosition ?
        metadata.GetRoiRect(mat.Width, mat.Height) :
        metadata.GetRoiRect();
        var ret = Rect.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return ret;

    }
    private Mat GetSub(Mat mat, ExtractMetadata metadata)
    {
        var roiRect = GetRoiRect(mat, metadata!);
        Mat subImage = new(mat, roiRect);
#if DEBUG
        subImage.SaveImage("sub.png");
#endif
        return subImage;
    }
    public async Task<InvoiceInfo?> ExtractByImg(Mat mat, ProcessConfig processConfig)
    {
        InvoiceInfo? ret = null;

        PaddleOcrResult result = paddleOcrAll.Run(mat);
        using Mat subImage = GetSub(mat, processConfig.Matcher!);
        var txt = paddleOcrAll.Run(subImage).Text;
        logger.LogInformation($"{processConfig.Matcher!.RegexPattern} -> {txt}");
        if (string.IsNullOrWhiteSpace(txt) || !Regex.Match(txt, processConfig.Matcher!.RegexPattern!).Success)
        {
            return ret;
        }
        ret = new InvoiceInfo();
        var props = typeof(InvoiceInfo).GetProperties();
        if (processConfig.Xtractors != null)
        {
            var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
            foreach (var xtractor in xtractors)
            {
                using Mat subImage2 = GetSub(mat, xtractor);
                txt = paddleOcrAll.Run(subImage2).Text;
                logger.LogInformation($"{xtractor.RegexPattern} -> {txt}");
                if (string.IsNullOrWhiteSpace(txt)) continue;
                var match = Regex.Match(txt, xtractor.RegexPattern!);
                if (!match.Success) continue;
                ret.SetValue(props, match, xtractor.RegexMapToProperties!);
            }
        }
        return ret;
    }

    public async Task<InvoiceInfo?> ExtractByImg(string filePath, ProcessConfig processConfig)
    {
        using var mat = Cv2.ImRead(filePath, ImreadModes.Color);
        return await ExtractByImg(mat, processConfig); ;
    }

    public async Task<InvoiceInfo?> ExtractByImgOnlyText(Mat mat, ProcessConfig processConfig)
    {
        PaddleOcrResult result = paddleOcrAll.Run(mat);
        InvoiceInfo? ret = null;
        var txt = result.Text.Replace(" ", "");
        Console.WriteLine("Detected all texts: \n" + txt);
        if (!Regex.Match(txt, processConfig.Matcher!.RegexPattern!).Success)
        {
            return ret;
        }
        ret = new InvoiceInfo();
        var props = typeof(InvoiceInfo).GetProperties();
        if (processConfig.Xtractors != null)
        {
            var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
            foreach (var xtractor in xtractors)
            {
                var match = Regex.Match(txt, xtractor.RegexPattern!);
                if (!match.Success) continue;
                ret.SetValue(props, match, xtractor.RegexMapToProperties!);
            }
        }
        return ret;
    }

    public async Task<InvoiceInfo?> ExtractByImgOnlyText(string filePath, ProcessConfig processConfig)
    {
        using var mat = Cv2.ImRead(filePath, ImreadModes.Color);
        return await ExtractByImgOnlyText(mat, processConfig);
    }

    public async Task OutputTestResult(Mat mat)
    {
        PaddleOcrResult result = paddleOcrAll.Run(mat);
        foreach (PaddleOcrResultRegion region in result.Regions)
        {
            Console.WriteLine($"Text: {region.Text}, Score: {region.Score}, RectCenter: {region.Rect.Center}, RectSize:    {region.Rect.Size}, Angle: {region.Rect.Angle}");
        }
    }
}