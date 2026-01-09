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