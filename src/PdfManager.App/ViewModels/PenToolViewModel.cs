using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfManager.App.ViewModels;

public sealed partial class PenToolViewModel : ObservableObject
{
    [ObservableProperty] private Color _selectedColor = Colors.Black;
    [ObservableProperty] private double _widthPt = 2.0;
    [ObservableProperty] private bool _isEraserMode;
    [ObservableProperty] private InkCanvasEditingMode _editingMode = InkCanvasEditingMode.None;

    public DrawingAttributes DrawingAttributes => new()
    {
        Color = SelectedColor,
        Width = WidthPt,
        Height = WidthPt,
        FitToCurve = true,
        IsHighlighter = false
    };

    public static IReadOnlyList<Color> Palette { get; } = new[]
    {
        Colors.Black, Colors.DimGray, Colors.Red, Colors.OrangeRed,
        Colors.Blue, Colors.DarkGreen, Colors.Purple, Colors.Brown
    };

    partial void OnSelectedColorChanged(Color value) { if (_editingMode == InkCanvasEditingMode.Ink) OnPropertyChanged(nameof(DrawingAttributes)); }
    partial void OnWidthPtChanged(double value)      { if (_editingMode == InkCanvasEditingMode.Ink) OnPropertyChanged(nameof(DrawingAttributes)); }

    public void ActivatePen()
    {
        IsEraserMode = false;
        EditingMode = InkCanvasEditingMode.Ink;
    }

    public void ActivateEraser()
    {
        IsEraserMode = true;
        EditingMode = InkCanvasEditingMode.EraseByStroke;
    }

    public void Deactivate() => EditingMode = InkCanvasEditingMode.None;

    public bool IsPenActive => EditingMode == InkCanvasEditingMode.Ink;
    public bool IsEraserActive => EditingMode == InkCanvasEditingMode.EraseByStroke;
    public bool IsAnyActive => EditingMode != InkCanvasEditingMode.None;
}
