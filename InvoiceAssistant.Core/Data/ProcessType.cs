namespace InvoiceAssistant.Core.Data;

public enum ProcessType
{
    UnKnown,
    PdfOnlyText,
    PdfWithTextPosition,
    PdfToImageWithTextPosition,
    ImageOnlyText,
    ImageWithTextPosition,
}
