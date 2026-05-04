using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PdfManager.App.ViewModels;

namespace PdfManager.App.Views;

public partial class PdfTabView : UserControl
{
    private const double ZoomStep = 0.15;
    private const double ZoomMin  = 0.20;
    private const double ZoomMax  = 4.00;

    private bool   _panning;
    private Point  _panOrigin;
    private double _hOffset;
    private double _vOffset;

    public PdfTabView()
    {
        InitializeComponent();
        PreviewMouseWheel += OnPreviewMouseWheel;
        MainScroll.PreviewMouseDown += OnScrollMouseDown;
        MainScroll.PreviewMouseMove += OnScrollMouseMove;
        MainScroll.PreviewMouseUp   += OnScrollMouseUp;

        DataContextChanged += (_, _) => WireMainVm();
    }

    private MainViewModel? _mainVm;

    private void WireMainVm()
    {
        if (_mainVm != null) _mainVm.PropertyChanged -= OnMainVmPropertyChanged;
        _mainVm = FindMainVm();
        if (_mainVm != null) _mainVm.PropertyChanged += OnMainVmPropertyChanged;
    }

    private void OnMainVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsPanMode))
            UpdateCursor();
    }

    private void UpdateCursor() =>
        MainScroll.Cursor = _mainVm?.IsPanMode == true ? Cursors.Hand : Cursors.Arrow;

    // ── Zoom ────────────────────────────────────────────────────────────────

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
        if (DataContext is not PdfTabViewModel vm) return;
        double delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
        vm.Zoom = Math.Clamp(Math.Round(vm.Zoom + delta, 2), ZoomMin, ZoomMax);
        e.Handled = true;
    }

    // ── Pan ─────────────────────────────────────────────────────────────────

    private bool ShouldPan(MouseButtonEventArgs e) =>
        e.ChangedButton == MouseButton.Middle ||
        (e.ChangedButton == MouseButton.Left && _mainVm?.IsPanMode == true);

    private void OnScrollMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ShouldPan(e)) return;
        _panning   = true;
        _panOrigin = e.GetPosition(MainScroll);
        _hOffset   = MainScroll.HorizontalOffset;
        _vOffset   = MainScroll.VerticalOffset;
        MainScroll.CaptureMouse();
        MainScroll.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void OnScrollMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning) return;
        var pos = e.GetPosition(MainScroll);
        MainScroll.ScrollToHorizontalOffset(_hOffset + (_panOrigin.X - pos.X));
        MainScroll.ScrollToVerticalOffset  (_vOffset + (_panOrigin.Y - pos.Y));
        e.Handled = true;
    }

    private void OnScrollMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_panning) return;
        _panning = false;
        MainScroll.ReleaseMouseCapture();
        UpdateCursor();
        e.Handled = true;
    }

    private MainViewModel? FindMainVm()
    {
        var p = System.Windows.Media.VisualTreeHelper.GetParent(this);
        while (p != null)
        {
            if (p is TabControl tc && tc.DataContext is MainViewModel mv) return mv;
            p = System.Windows.Media.VisualTreeHelper.GetParent(p);
        }
        return null;
    }
}
