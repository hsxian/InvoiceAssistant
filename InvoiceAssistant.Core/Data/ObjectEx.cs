using System.Text.Json;

namespace InvoiceAssistant.Core.Data;

public static class ObjectEx
{
    public static T CloneByJson<T>(this object obj)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj))!;
    }
}
