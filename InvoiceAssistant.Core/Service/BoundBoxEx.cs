using Docnet.Core.Models;

namespace InvoiceAssistant.Core.Service;

public static class BoundBoxEx
{
    public static BoundBox Plus(this BoundBox @this, BoundBox box)
    {
        return new BoundBox(@this.Left + box.Left,
        @this.Top + box.Top,
        @this.Right + box.Right,
        @this.Bottom + box.Bottom);
    }
}
