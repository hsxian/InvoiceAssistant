using InvoiceAssistant.Core.Data;

namespace InvoiceAssistant.App.Models;

public record EnumOptions(ProcessType Value, string Label)
{
    public record ProcessTypeOption(ProcessType Value, string Label);
    public static ProcessTypeOption[] ProcessTypeOptions { get; } =
    [
        new ProcessTypeOption(ProcessType.ImageOnlyText, "提取图像文本"),
        new ProcessTypeOption(ProcessType.ImageWithTextPosition, "提取图像文本根据位置"),
        new ProcessTypeOption(ProcessType.PdfOnlyText, "提取PDF文本"),
        new ProcessTypeOption(ProcessType.PdfWithTextPosition, "提取PDF文本根据位置"),
        new ProcessTypeOption(ProcessType.PdfToImageWithTextPosition, "将PDF转换为图像并提取文本根据位置"),
        new ProcessTypeOption(ProcessType.UnKnown, "未知"),
    ];
}