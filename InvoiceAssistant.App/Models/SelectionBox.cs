using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using InvoiceAssistant.Core.Data;

namespace InvoiceAssistant.App.Models;

// 选择框类
public class SelectionBox
{
    public Rect Bounds { get; private set; }
    public ExtractMetadata Metadata { get; set; }
    private const double HandleSize = 8;

    public SelectionBox(Point start, Point end)
    {
        UpdateBounds(start, end);
    }

    public static SelectionBox NewSelectionBox(ExtractMetadata metadata, double width, double height)
    {
        var ret = metadata.ScaleOrAbsolutePosition
                ? new SelectionBox(
                    new Point(metadata.Left * width, metadata.Top * height),
                    new Point(metadata.Right * width,
                        metadata.Bottom * height))
                : new SelectionBox(new Point(metadata.Left, metadata.Top), new Point(metadata.Right, metadata.Bottom))
            ;
        ret.Metadata = metadata;
        return ret;
    }
    public void SyncPositionToExtractMetadata(double width, double height)
    {
        if (Metadata.ScaleOrAbsolutePosition)
        {
            Metadata.Left = (float)(Bounds.Left / width);
            Metadata.Top = (float)(Bounds.Top / height);
            Metadata.Right = (float)(Bounds.Right / width);
            Metadata.Bottom = (float)(Bounds.Bottom / height);
        }
        else
        {

            Metadata.Left = (float)Bounds.Left;
            Metadata.Top = (float)Bounds.Top;
            Metadata.Right = (float)Bounds.Right;
            Metadata.Bottom = (float)Bounds.Bottom;
        }
    }
    public void UpdateEnd(Point end)
    {
        UpdateBounds(Bounds.TopLeft, end);
    }

    public void Move(Vector delta)
    {
        Bounds = new Rect(Bounds.TopLeft + delta, Bounds.Size);
    }

    public void Resize(ResizeHandle handle, Point point)
    {
        var left = Bounds.Left;
        var top = Bounds.Top;
        var right = Bounds.Right;
        var bottom = Bounds.Bottom;

        switch (handle)
        {
            case ResizeHandle.TopLeft:
                left = point.X;
                top = point.Y;
                break;
            case ResizeHandle.TopRight:
                right = point.X;
                top = point.Y;
                break;
            case ResizeHandle.BottomLeft:
                left = point.X;
                bottom = point.Y;
                break;
            case ResizeHandle.BottomRight:
                right = point.X;
                bottom = point.Y;
                break;
        }

        // 确保框有最小大小
        if (right > left + 10 && bottom > top + 10)
        {
            Bounds = new Rect(left, top, right - left, bottom - top);
        }
    }

    public bool Contains(Point point)
    {
        return Bounds.Contains(point);
    }

    public ResizeHandle HitTest(Point point)
    {
        // 检查四个角的调整手柄
        if (new Rect(Bounds.TopLeft, new Size(HandleSize, HandleSize)).Contains(point))
            return ResizeHandle.TopLeft;
        if (new Rect(new Point(Bounds.TopRight.X - HandleSize, Bounds.TopRight.Y), new Size(HandleSize, HandleSize))
            .Contains(point))
            return ResizeHandle.TopRight;
        if (new Rect(new Point(Bounds.BottomLeft.X, Bounds.BottomLeft.Y - HandleSize), new Size(HandleSize, HandleSize))
            .Contains(point))
            return ResizeHandle.BottomLeft;
        if (new Rect(new Point(Bounds.BottomRight.X - HandleSize, Bounds.BottomRight.Y - HandleSize),
                new Size(HandleSize, HandleSize)).Contains(point))
            return ResizeHandle.BottomRight;
        return ResizeHandle.None;
    }

    public void Draw(Canvas canvas)
    {
        // 绘制框
        var border = new Border
        {
            BorderBrush = Brushes.Red,
            BorderThickness = new Thickness(2),
            Width = Bounds.Width,
            Height = Bounds.Height
        };
        Canvas.SetLeft(border, Bounds.Left);
        Canvas.SetTop(border, Bounds.Top);
        canvas.Children.Add(border);

        // 绘制调整手柄
        DrawHandle(canvas, Bounds.TopLeft);
        DrawHandle(canvas, Bounds.TopRight - new Point(HandleSize, 0));
        DrawHandle(canvas, Bounds.BottomLeft - new Point(0, HandleSize));
        DrawHandle(canvas, Bounds.BottomRight - new Point(HandleSize, HandleSize));
    }

    private void DrawHandle(Canvas canvas, Point position)
    {
        var handle = new Canvas
        {
            Width = HandleSize,
            Height = HandleSize,
            Background = Brushes.Blue
        };
        Canvas.SetLeft(handle, position.X);
        Canvas.SetTop(handle, position.Y);
        canvas.Children.Add(handle);
    }

    private void UpdateBounds(Point start, Point end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);
        Bounds = new Rect(left, top, width, height);
    }
}