namespace InvoiceAssistant.Core.Data;

public class RegexMapToPropertyMetadata(int groupsIndex, string? propertyName)
{
    public int GroupsIndex { get; set; } = groupsIndex;
    public string? PropertyName { get; set; } = propertyName;
}
public class ExtractMetadata
{
    public string? RegexPattern { get; set; }
    public float Left { get; set; }
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public bool ScaleOrAbsolutePosition { get; set; } = true;
    public List<RegexMapToPropertyMetadata>? RegexMapToProperties { get; set; }

    public ExtractMetadata()
    {

    }

    public ExtractMetadata(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public void Offset(float x, float y)
    {
        Left += x;
        Right += x;
        Top += y;
        Bottom += y;
    }

}
