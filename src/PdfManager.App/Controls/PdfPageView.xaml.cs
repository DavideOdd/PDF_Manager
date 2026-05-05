using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Ink;
using System.Windows.Media;
using PdfManager.App.ViewModels;
using PdfManager.Core.Models;

namespace PdfManager.App.Controls;

public partial class PdfPageView : UserControl
{
    public static readonly DependencyProperty PenToolProperty =
        DependencyProperty.Register(nameof(PenTool), typeof(PenToolViewModel), typeof(PdfPageView),
            new PropertyMetadata(null, OnPenToolChanged));

    public static readonly DependencyProperty IsTextModeProperty =
        DependencyProperty.Register(nameof(IsTextMode), typeof(bool), typeof(PdfPageView),
            new PropertyMetadata(false, OnIsTextModeChanged));

    public PenToolViewModel? PenTool
    {
        get => (PenToolViewModel?)GetValue(PenToolProperty);
        set => SetValue(PenToolProperty, value);
    }

    public bool IsTextMode
    {
        get => (bool)GetValue(IsTextModeProperty);
        set => SetValue(IsTextModeProperty, value);
    }

    public PdfPageView()
    {
        InitializeComponent();
        Ink.StrokeCollected += OnStrokeCollected;
        Ink.StrokeErased    += OnStrokeErased;
    }

    private void OnStrokeCollected(object? sender, InkCanvasStrokeCollectedEventArgs e)
    {
        var tab = GetTab();
        if (tab == null) { MarkDirty(); return; }
        var stroke = e.Stroke;
        tab.PushUndo(() =>
        {
            Ink.Strokes.Remove(stroke);
            MarkDirty();
        });
        MarkDirty();
    }

    private void OnStrokeErased(object? sender, RoutedEventArgs e)
    {
        MarkDirty();
        // Undo for erased strokes: complex — push a snapshot restore
    }

    private PdfTabViewModel? GetTab()
    {
        var view = FindAncestor<Views.PdfTabView>(this);
        return view?.DataContext as PdfTabViewModel;
    }

    // ── PenTool ────────────────────────────────────────────────────────────

    private static void OnPenToolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var v = (PdfPageView)d;
        if (e.OldValue is PenToolViewModel old) old.PropertyChanged -= v.OnPenToolPropertyChanged;
        if (e.NewValue is PenToolViewModel nw)  nw.PropertyChanged  += v.OnPenToolPropertyChanged;
        v.SyncInkCanvas(e.NewValue as PenToolViewModel);
    }

    private void OnPenToolPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PenToolViewModel.EditingMode)
                           or nameof(PenToolViewModel.DrawingAttributes))
            SyncInkCanvas(sender as PenToolViewModel);
    }

    private void SyncInkCanvas(PenToolViewModel? vm)
    {
        if (vm == null) { Ink.EditingMode = InkCanvasEditingMode.None; Ink.IsHitTestVisible = false; return; }
        Ink.EditingMode = vm.EditingMode;
        if (vm.EditingMode == InkCanvasEditingMode.Ink)
            Ink.DefaultDrawingAttributes = vm.DrawingAttributes;
        // pass touch through to ScrollViewer when not inking
        Ink.IsHitTestVisible = vm.EditingMode != InkCanvasEditingMode.None;
    }

    // ── TextMode ───────────────────────────────────────────────────────────

    private static void OnIsTextModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var v = (PdfPageView)d;
        // When leaving text mode, hide hit-test so scroll still works
        v.TextCanvas.IsHitTestVisible = (bool)e.NewValue;
        v.TextCanvas.Cursor = (bool)e.NewValue ? Cursors.IBeam : Cursors.Arrow;
    }

    private void TextCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsTextMode) return;
        var pos = e.GetPosition(TextCanvas);
        PlaceTextBox(pos.X, pos.Y);
        e.Handled = true;
    }

    private void PlaceTextBox(double x, double y)
    {
        var vm = DataContext as PageViewModel;

        var tb = new TextBox
        {
            MinWidth     = 120,
            MinHeight    = 28,
            FontSize     = 14,
            Background   = Brushes.White,
            BorderBrush  = Brushes.DodgerBlue,
            BorderThickness = new Thickness(1.5),
            AcceptsReturn = true,
            TextWrapping  = TextWrapping.Wrap,
            Padding       = new Thickness(2)
        };

        Canvas.SetLeft(tb, x);
        Canvas.SetTop(tb, y);
        TextCanvas.Children.Add(tb);
        tb.Focus();

        tb.LostFocus += (_, _) => CommitTextBox(tb, x, y, vm);
        tb.KeyDown   += (_, ke) =>
        {
            if (ke.Key == Key.Escape)
            {
                TextCanvas.Children.Remove(tb);
                ke.Handled = true;
            }
        };
    }

    private void CommitTextBox(TextBox tb, double x, double y, PageViewModel? vm)
    {
        var text = tb.Text.Trim();
        TextCanvas.Children.Remove(tb);

        if (string.IsNullOrEmpty(text) || vm == null) return;

        // Convert DIP coords to PDF user-space points
        double pdfX  = vm.PxToPt(x);
        double pdfY  = vm.FlipY(y);
        double wPt   = vm.PxToPt(Math.Max(tb.ActualWidth, 80));
        double hPt   = vm.PxToPt(Math.Max(tb.ActualHeight, 20));

        var note = new FreeTextNote
        {
            PageIndex  = vm.PageIndex,
            Text       = text,
            Rect       = new RectPt(pdfX, pdfY - hPt, pdfX + wPt, pdfY),
            FontSizePt = 12
        };

        vm.Notes.Add(note);
        MarkDirty();

        // Show persistent label on canvas
        AddNoteLabel(note, x, y, vm);
    }

    // Called when page loads: render saved FreeTextNotes as labels
    public void RenderSavedNotes(PageViewModel vm)
    {
        TextCanvas.Children.Clear();
        foreach (var note in vm.Notes)
        {
            double dipX = vm.PtToPx(note.Rect.Left);
            double dipY = vm.FlipYFromPdf(note.Rect.Top);
            AddNoteLabel(note, dipX, dipY, vm);
        }
    }

    private void AddNoteLabel(FreeTextNote note, double x, double y, PageViewModel vm)
    {
        var border = new Border
        {
            Background      = new SolidColorBrush(Color.FromArgb(220, 255, 255, 200)),
            BorderBrush     = Brushes.Goldenrod,
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(3),
            Padding         = new Thickness(4, 2, 4, 2),
            MaxWidth        = 300,
            Tag             = note
        };
        var tb = new TextBlock
        {
            Text         = note.Text,
            FontSize     = 13,
            TextWrapping = TextWrapping.Wrap
        };
        border.Child = tb;
        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        border.MouseLeftButtonDown += (_, e) =>
        {
            if (!IsTextMode) return;
            EditNoteLabel(border, note, vm);
            e.Handled = true;
        };
        TextCanvas.Children.Add(border);
    }

    private void EditNoteLabel(Border border, FreeTextNote note, PageViewModel vm)
    {
        double x = Canvas.GetLeft(border);
        double y = Canvas.GetTop(border);
        TextCanvas.Children.Remove(border);
        vm.Notes.Remove(note);

        var tb = new TextBox
        {
            Text         = note.Text,
            MinWidth     = 120,
            MinHeight    = 28,
            FontSize     = 14,
            Background   = Brushes.White,
            BorderBrush  = Brushes.DodgerBlue,
            BorderThickness = new Thickness(1.5),
            AcceptsReturn   = true,
            TextWrapping    = TextWrapping.Wrap,
            Padding         = new Thickness(2)
        };
        Canvas.SetLeft(tb, x);
        Canvas.SetTop(tb, y);
        TextCanvas.Children.Add(tb);
        tb.Focus();
        tb.SelectAll();

        tb.LostFocus += (_, _) => CommitTextBox(tb, x, y, vm);
        tb.KeyDown   += (_, ke) =>
        {
            if (ke.Key == Key.Escape)
            {
                TextCanvas.Children.Remove(tb);
                // Restore the original note
                vm.Notes.Add(note);
                AddNoteLabel(note, x, y, vm);
                ke.Handled = true;
            }
        };
    }

    // ── Dirty tracking ────────────────────────────────────────────────────

    private void MarkDirty()
    {
        var tab = FindAncestor<Views.PdfTabView>(this);
        if (tab?.DataContext is PdfTabViewModel vm) vm.IsDirty = true;
    }

    private static T? FindAncestor<T>(DependencyObject obj) where T : DependencyObject
    {
        var p = VisualTreeHelper.GetParent(obj);
        while (p != null) { if (p is T t) return t; p = VisualTreeHelper.GetParent(p); }
        return null;
    }
}
