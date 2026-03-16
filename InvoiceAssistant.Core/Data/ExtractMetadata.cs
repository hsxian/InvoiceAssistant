using System.Drawing;
using System.ComponentModel;

namespace InvoiceAssistant.Core.Data;

public class RegexMapToPropertyMetadata(int groupsIndex, string? propertyName) : INotifyPropertyChanged
{
    public int GroupsIndex { get; set; } = groupsIndex;
    public string? PropertyName { get; set; } = propertyName;
    public string? Format { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}
public class ExtractMetadata : INotifyPropertyChanged
{
    public string? RegexPattern { get; set; }
    public float Left { get; set; }
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public bool? Enable { get; set; }
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Offset(float x, float y)
    {
        Left += x;
        Right += x;
        Top += y;
        Bottom += y;
    }
    public Rectangle GetRoiRect(int w, int h)
    {
        var l = (int)(w * Left);
        var r = (int)(w * Right);
        var t = (int)(h * Top);
        var b = (int)(h * Bottom);
        return new Rectangle(l, t, r - l, b - t);
    }
    public Rectangle GetRoiRect()
    {
        return new Rectangle((int)Left, (int)Top,
                    (int)(Right - Left), (int)(Bottom - Top));
    }
}
