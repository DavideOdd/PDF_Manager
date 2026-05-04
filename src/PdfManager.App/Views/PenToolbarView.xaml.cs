using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PdfManager.App.ViewModels;

namespace PdfManager.App.Views;

public partial class PenToolbarView : UserControl
{
    public PenToolbarView() => InitializeComponent();

    private PenToolViewModel? VM => DataContext as PenToolViewModel;

    private void PenButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = VM; if (vm == null) return;
        if (vm.IsPenActive) vm.Deactivate(); else vm.ActivatePen();
    }

    private void EraserButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = VM; if (vm == null) return;
        if (vm.IsEraserActive) vm.Deactivate(); else vm.ActivateEraser();
    }

    private void Swatch_Click(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        if (btn.Tag is Color c && VM != null)
        {
            VM.SelectedColor = c;
            if (!VM.IsPenActive) VM.ActivatePen();
        }
    }
}
