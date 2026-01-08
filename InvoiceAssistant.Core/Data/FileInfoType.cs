using OpenCvSharp;

namespace InvoiceAssistant.Core.Data;

public enum FileInfoType
{
    UnKnown,
    /// <summary>
    /// 出差审批单
    /// </summary>
    ApprovalForm,
    TrainTicket,
    PlaneTicket,
    Screenshot,
    ScreenshotApplyPlane,
    ScreenshotBankPayment
}
public class FileInfoTypeBag
{
    public FileInfoType InfoType { get; set; }
    public string? Title { get; set; }

    public void CopyTo(InvoiceInfo? info)
    {
        if (info == null) return;
        info.Title = Title;
        info.InfoType = InfoType;
    }
}
