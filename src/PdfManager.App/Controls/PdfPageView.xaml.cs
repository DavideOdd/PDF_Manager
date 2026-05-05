using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Shapes;
using PdfManager.App.ViewModels;
using PdfManager.Core.Models;

namespace PdfManager.App.Controls;

public partial class PdfPageView : UserControl
{
    // ── Dependency Properties ────────────────────────────────────────────

    public static readonly DependencyProperty PenToolProperty =
        DependencyProperty.Register(nameof(PenTool), typeof(PenToolViewModel), typeof(PdfPageView),
            new PropertyMetadata(null, OnPenToolChanged));

    public static readonly DependencyProperty IsTextModeProperty =
        DependencyProperty.Register(nameof(IsTextMode), typeof(bool), typeof(PdfPageView),
            new PropertyMetadata(false, OnIsTextModeChanged));

    public static readonly DependencyProperty IsArrowModeProperty =
        DependencyProperty.Register(nameof(IsArrowMode), typeof(bool), typeof(PdfPageView),
            new PropertyMetadata(false, OnShapeModeChanged));

    public static readonly DependencyProperty IsCircleModeProperty =
        DependencyProperty.Register(nameof(IsCircleMode), typeof(bool), typeof(PdfPageView),
            new PropertyMetadata(false, OnShapeModeChanged));

    public PenToolViewModel? PenTool  { get => (PenToolViewModel?)GetValue(PenToolProperty);  set => SetValue(PenToolProperty, value); }
    public bool IsTextMode   { get => (bool)GetValue(IsTextModeProperty);   set => SetValue(IsTextModeProperty, value); }
    public bool IsArrowMode  { get => (bool)GetValue(IsArrowModeProperty);  set => SetValue(IsArrowModeProperty, value); }
    public bool IsCircleMode { get => (bool)GetValue(IsCircleModeProperty); set => SetValue(IsCircleModeProperty, value); }

    // ── Shape drawing state ───────────────────────────────────────────────

    private bool _drawingShape;
    private Point _shapeStart;
    private UIElement? _shapePreview;

    // ── Init ─────────────────────────────────────────────────────────────

    public PdfPageView()
    {
        InitializeComponent();
        Ink.StrokeCollected += OnStrokeCollected;
        Ink.StrokeErased    += OnStrokeErased;
    }

    // ── Ink ──────────────────────────────────────────────────────────────

    private void OnStrokeCollected(object? sender, InkCanvasStrokeCollectedEventArgs e)
    {
        var tab = GetTab();
        if (tab == null) { MarkDirty(); return; }
        var stroke = e.Stroke;
        tab.PushUndo(() => { Ink.Strokes.Remove(stroke); MarkDirty(); });
        MarkDirty();
    }

    private void OnStrokeErased(object? sender, RoutedEventArgs e) => MarkDirty();

    // ── PenTool ───────────────────────────────────────────────────────────

    private static void OnPenToolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var v = (PdfPageView)d;
        if (e.OldValue is PenToolViewModel old) old.PropertyChanged -= v.OnPenToolPropertyChanged;
        if (e.NewValue is PenToolViewModel nw)  nw.PropertyChanged  += v.OnPenToolPropertyChanged;
        v.SyncInkCanvas(e.NewValue as PenToolViewModel);
    }

    private void OnPenToolPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PenToolViewModel.EditingMode) or nameof(PenToolViewModel.DrawingAttributes))
            SyncInkCanvas(sender as PenToolViewModel);
    }

    private void SyncInkCanvas(PenToolViewModel? vm)
    {
        if (vm == null) { Ink.EditingMode = InkCanvasEditingMode.None; Ink.IsHitTestVisible = false; return; }
        Ink.EditingMode = vm.EditingMode;
        if (vm.EditingMode == InkCanvasEditingMode.Ink)
            Ink.DefaultDrawingAttributes = vm.DrawingAttributes;
        Ink.IsHitTestVisible = vm.EditingMode != InkCanvasEditingMode.None;
    }

    // ── TextMode ──────────────────────────────────────────────────────────

    private static void OnIsTextModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var v = (PdfPageView)d;
        v.TextCanvas.IsHitTestVisible = (bool)e.NewValue;
        v.TextCanvas.Cursor = (bool)e.NewValue ? Cursors.IBeam : Cursors.Arrow;
    }

    private void TextCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsTextMode) return;
        PlaceTextBox(e.GetPosition(TextCanvas).X, e.GetPosition(TextCanvas).Y);
        e.Handled = true;
    }

    // ── ShapeMode ─────────────────────────────────────────────────────────

    private static void OnShapeModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var v = (PdfPageView)d;
        bool any = v.IsArrowMode || v.IsCircleMode;
        v.ShapeCanvas.IsHitTestVisible = any;
        v.ShapeCanvas.Cursor = any ? Cursors.Cross : Cursors.Arrow;
    }

    private void ShapeCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsArrowMode && !IsCircleMode) return;
        _drawingShape = true;
        _shapeStart   = e.GetPosition(ShapeCanvas);
        ShapeCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void ShapeCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_drawingShape) return;
        var pos = e.GetPosition(ShapeCanvas);
        if (_shapePreview != null) ShapeCanvas.Children.Remove(_shapePreview);
        _shapePreview = CreateShapePreview(_shapeStart, pos);
        if (_shapePreview != null) ShapeCanvas.Children.Add(_shapePreview);
        e.Handled = true;
    }

    private void ShapeCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_drawingShape) return;
        _drawingShape = false;
        ShapeCanvas.ReleaseMouseCapture();
        var end = e.GetPosition(ShapeCanvas);
        if (_shapePreview != null) ShapeCanvas.Children.Remove(_shapePreview);
        _shapePreview = null;

        var vm  = DataContext as PageViewModel;
        var tab = GetTab();
        if (vm == null) return;

        var color = PenTool?.SelectedColor ?? Colors.Red;
        double w  = PenTool?.WidthPt ?? 2;
        var kind  = IsArrowMode ? ShapeKind.Arrow : ShapeKind.Circle;

        var annot = new ShapeAnnotation
        {
            PageIndex = vm.PageIndex,
            Kind      = kind,
            Start     = new PointPt(vm.PxToPt(_shapeStart.X), vm.FlipY(_shapeStart.Y)),
            End       = new PointPt(vm.PxToPt(end.X), vm.FlipY(end.Y)),
            ColorR    = color.R, ColorG = color.G, ColorB = color.B,
            WidthPt   = w
        };
        vm.Shapes.Add(annot);
        RenderShape(annot, vm);
        tab?.PushUndo(() => { vm.Shapes.Remove(annot); RedrawShapes(vm); MarkDirty(); });
        MarkDirty();
        e.Handled = true;
    }

    private UIElement? CreateShapePreview(Point a, Point b)
    {
        var brush = new SolidColorBrush(PenTool?.SelectedColor ?? Colors.Red);
        double thickness = PenTool?.WidthPt ?? 2;
        if (IsArrowMode)
        {
            return new Line
            {
                X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y,
                Stroke = brush, StrokeThickness = thickness,
                StrokeEndLineCap = PenLineCap.Triangle
            };
        }
        if (IsCircleMode)
        {
            var el = new Ellipse { Stroke = brush, StrokeThickness = thickness, Fill = Brushes.Transparent };
            double x = Math.Min(a.X, b.X), y = Math.Min(a.Y, b.Y);
            double w = Math.Abs(b.X - a.X), h = Math.Abs(b.Y - a.Y);
            Canvas.SetLeft(el, x); Canvas.SetTop(el, y);
            el.Width = w; el.Height = h;
            return el;
        }
        return null;
    }

    private void RenderShape(ShapeAnnotation annot, PageViewModel vm)
    {
        var brush = new SolidColorBrush(Color.FromRgb(annot.ColorR, annot.ColorG, annot.ColorB));
        double x1 = vm.PtToPx(annot.Start.X), y1 = vm.FlipYFromPdf(annot.Start.Y);
        double x2 = vm.PtToPx(annot.End.X),   y2 = vm.FlipYFromPdf(annot.End.Y);

        UIElement el;
        if (annot.Kind == ShapeKind.Arrow)
        {
            el = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = brush,
                            StrokeThickness = annot.WidthPt, StrokeEndLineCap = PenLineCap.Triangle,
                            Tag = annot.Id };
        }
        else
        {
            var ell = new Ellipse { Stroke = brush, StrokeThickness = annot.WidthPt, Fill = Brushes.Transparent, Tag = annot.Id };
            double lx = Math.Min(x1, x2), ly = Math.Min(y1, y2);
            ell.Width = Math.Abs(x2 - x1); ell.Height = Math.Abs(y2 - y1);
            Canvas.SetLeft(ell, lx); Canvas.SetTop(ell, ly);
            el = ell;
        }
        ShapeCanvas.Children.Add(el);
    }

    public void RedrawShapes(PageViewModel vm)
    {
        ShapeCanvas.Children.Clear();
        foreach (var s in vm.Shapes) RenderShape(s, vm);
    }

    // ── TextBox placement (existing text annotations) ─────────────────────

    private void PlaceTextBox(double x, double y)
    {
        var vm = DataContext as PageViewModel;
        var tb = new TextBox
        {
            MinWidth = 120, MinHeight = 28, FontSize = 14,
            Background = Brushes.White, BorderBrush = Brushes.DodgerBlue,
            BorderThickness = new Thickness(1.5), AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap, Padding = new Thickness(2)
        };
        Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
        TextCanvas.Children.Add(tb);
        tb.Focus();
        tb.LostFocus += (_, _) => CommitTextBox(tb, x, y, vm);
        tb.KeyDown   += (_, ke) => { if (ke.Key == Key.Escape) { TextCanvas.Children.Remove(tb); ke.Handled = true; } };
    }

    private void CommitTextBox(TextBox tb, double x, double y, PageViewModel? vm)
    {
        var text = tb.Text.Trim();
        TextCanvas.Children.Remove(tb);
        if (string.IsNullOrEmpty(text) || vm == null) return;
        var note = new FreeTextNote
        {
            PageIndex  = vm.PageIndex, Text = text,
            Rect       = new RectPt(vm.PxToPt(x), vm.FlipY(y + tb.ActualHeight), vm.PxToPt(x + Math.Max(tb.ActualWidth, 80)), vm.FlipY(y)),
            FontSizePt = 12
        };
        vm.Notes.Add(note);
        MarkDirty();
        AddNoteLabel(note, x, y, vm);
    }

    public void RenderSavedNotes(PageViewModel vm)
    {
        TextCanvas.Children.Clear();
        foreach (var note in vm.Notes)
            AddNoteLabel(note, vm.PtToPx(note.Rect.Left), vm.FlipYFromPdf(note.Rect.Top), vm);
    }

    private void AddNoteLabel(FreeTextNote note, double x, double y, PageViewModel vm)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 200)),
            BorderBrush = Brushes.Goldenrod, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3), Padding = new Thickness(4, 2, 4, 2),
            MaxWidth = 300, Tag = note
        };
        border.Child = new TextBlock { Text = note.Text, FontSize = 13, TextWrapping = TextWrapping.Wrap };
        Canvas.SetLeft(border, x); Canvas.SetTop(border, y);
        border.MouseLeftButtonDown += (_, e) => { if (!IsTextMode) return; EditNoteLabel(border, note, vm); e.Handled = true; };
        TextCanvas.Children.Add(border);
    }

    private void EditNoteLabel(Border border, FreeTextNote note, PageViewModel vm)
    {
        double x = Canvas.GetLeft(border), y = Canvas.GetTop(border);
        TextCanvas.Children.Remove(border);
        vm.Notes.Remove(note);
        PlaceTextBoxWithText(x, y, note.Text, vm);
    }

    private void PlaceTextBoxWithText(double x, double y, string initial, PageViewModel? vm)
    {
        var tb = new TextBox
        {
            Text = initial, MinWidth = 120, MinHeight = 28, FontSize = 14,
            Background = Brushes.White, BorderBrush = Brushes.DodgerBlue,
            BorderThickness = new Thickness(1.5), AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap, Padding = new Thickness(2)
        };
        Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
        TextCanvas.Children.Add(tb);
        tb.Focus(); tb.SelectAll();
        tb.LostFocus += (_, _) => CommitTextBox(tb, x, y, vm);
        tb.KeyDown   += (_, ke) => { if (ke.Key == Key.Escape) { TextCanvas.Children.Remove(tb); ke.Handled = true; } };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void MarkDirty()
    {
        var tab = GetTab();
        if (tab != null) tab.IsDirty = true;
    }

    private PdfTabViewModel? GetTab()
    {
        var view = FindAncestor<Views.PdfTabView>(this);
        return view?.DataContext as PdfTabViewModel;
    }

    private static T? FindAncestor<T>(DependencyObject obj) where T : DependencyObject
    {
        var p = VisualTreeHelper.GetParent(obj);
        while (p != null) { if (p is T t) return t; p = VisualTreeHelper.GetParent(p); }
        return null;
    }
}
