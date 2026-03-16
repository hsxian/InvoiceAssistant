using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using InvoiceAssistant.App.ViewModels;
using InvoiceAssistant.App.Models;

namespace InvoiceAssistant.App.Views;

public partial class ProcessConfigWindow : Window
{
    private ProcessConfigViewModel? _viewModel;
    private List<SelectionBox> _selectionBoxes = [];
    private SelectionBox? _currentBox = null;
    private Point _startPoint;
    private bool _isDragging = false;
    private bool _isResizing = false;
    private ResizeHandle _currentHandle = ResizeHandle.None;

    public ProcessConfigWindow()
    {
        InitializeComponent();

        // 订阅鼠标事件
        PointerPressed += ImageCanvas_PointerPressed;
        PointerMoved += ImageCanvas_PointerMoved;
        PointerReleased += ImageCanvas_PointerReleased;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ProcessConfigViewModel viewModel)
        {
            viewModel.ViewWindow = this;
            viewModel.SelectionBoxesChanged += ViewModel_SelectionBoxesChanged;
            _viewModel = viewModel;
        }
    }

    private void ViewModel_SelectionBoxesChanged(object? sender, IEnumerable<SelectionBox> e)
    {
        _selectionBoxes = [.. e];
        DrawBoxes();
    }


    private void ImageCanvas_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        Console.WriteLine(nameof(ImageCanvas_PointerPressed));
        _isResizing = _isDragging = false;
        var point = e.GetPosition(DisplayImage);
        _startPoint = point;


        // 检查是否点击了现有框的调整手柄
        foreach (var box in _selectionBoxes)
        {
            var handle = box.HitTest(point);
            if (handle == ResizeHandle.None) continue;
            _currentBox = box;
            _currentHandle = handle;
            _isResizing = true;
            return;
        }

        // 检查是否点击了现有框
        foreach (var box in _selectionBoxes)
        {
            if (!box.Contains(point)) continue;
            _currentBox = box;
            _isDragging = true;
            return;
        }

        // 创建新框
        // _currentBox = new SelectionBox(point, point);
        // _selectionBoxes.Add(_currentBox);
        DrawBoxes();
    }

    private void SyncPositionToExtractMetadata()
    {
        if (_currentBox == null || _viewModel?.DisplayBitmap is null
        || _viewModel.DisplayBitmap.Size.Width == 0 || _viewModel.DisplayBitmap.Size.Height == 0)
        {
            return;
        }
        _currentBox!.SyncPositionToExtractMetadata(_viewModel.DisplayBitmap.Size.Width, _viewModel.DisplayBitmap.Size.Height);
    }

    private void ImageCanvas_PointerMoved(object sender, PointerEventArgs e)
    {
        if (_currentBox == null)
            return;

        Console.WriteLine(nameof(ImageCanvas_PointerMoved));
        var point = e.GetPosition(DisplayImage);

        if (_isDragging)
        {
            // 移动框
            var delta = point - _startPoint;
            _currentBox.Move(delta);
            _startPoint = point;
            SyncPositionToExtractMetadata();
        }
        else if (_isResizing)
        {
            // 调整框大小
            _currentBox.Resize(_currentHandle, point);
            SyncPositionToExtractMetadata();
        }
        // else
        // {
        //     // 绘制新框
        //     _currentBox.UpdateEnd(point);
        // }

        DrawBoxes();
    }

    private void ImageCanvas_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        Console.WriteLine(nameof(ImageCanvas_PointerReleased));
        _isDragging = false;
        _isResizing = false;
        _currentHandle = ResizeHandle.None;
        _currentBox = null;
    }

    private void DrawBoxes()
    {
        // 清除现有框
        ImageCanvas.Children.Clear();

        // 绘制所有框
        foreach (var box in _selectionBoxes)
        {
            box.Draw(ImageCanvas);
        }
    }
}