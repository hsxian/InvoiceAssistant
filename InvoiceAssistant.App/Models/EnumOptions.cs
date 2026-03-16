using InvoiceAssistant.Core.Data;

namespace InvoiceAssistant.App.Models;

public record EnumOptions(ProcessType Value, string Label)
{
    public record ProcessTypeOption(ProcessType Value, string Label);
    public record InvoiceInfoPeopertyOption(string Value, string Label);
    public static ProcessTypeOption[] ProcessTypeOptions { get; } =
    [
        new ProcessTypeOption(ProcessType.ImageOnlyText, "提取图像文本"),
        new ProcessTypeOption(ProcessType.ImageWithTextPosition, "提取图像文本根据位置"),
        new ProcessTypeOption(ProcessType.PdfOnlyText, "提取PDF文本"),
        new ProcessTypeOption(ProcessType.PdfWithTextPosition, "提取PDF文本根据位置"),
        new ProcessTypeOption(ProcessType.PdfToImageWithTextPosition, "将PDF转换为图像并提取文本根据位置"),
        new ProcessTypeOption(ProcessType.UnKnown, "未知"),
    ];
    public static InvoiceInfoPeopertyOption[] InvoiceInfoPeopertyOptions { get; } =
    [
        new InvoiceInfoPeopertyOption(nameof(InvoiceInfo.Destination), "目的地"),
        new InvoiceInfoPeopertyOption(nameof(InvoiceInfo.EndTime), "结束时间"),
        new InvoiceInfoPeopertyOption(nameof(InvoiceInfo.Origin), "出发地"),
        new InvoiceInfoPeopertyOption(nameof(InvoiceInfo.PersonName), "乘客姓名"),
        new InvoiceInfoPeopertyOption(nameof(InvoiceInfo.StartTime), "开始时间"),
        new InvoiceInfoPeopertyOption(nameof(InvoiceInfo.TicketPrice), "金额"),
        new InvoiceInfoPeopertyOption(nameof(InvoiceInfo.Title), "标题"),
    ];
}