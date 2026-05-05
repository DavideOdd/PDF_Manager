using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using PdfManager.App.ViewModels;

namespace PdfManager.App;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private MainViewModel? VM => DataContext as MainViewModel;

    private void Window_DragOver(object sender, DragEventArgs e)
        => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            VM?.HandleFileDrop(files);
    }

    private void PenOptionsBtn_Click(object sender, RoutedEventArgs e)
        => PenPopup.IsOpen = !PenPopup.IsOpen;

    private void SwatchClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is System.Windows.Media.Color c && VM != null)
        {
            VM.PenTool.SelectedColor = c;
            if (!VM.IsPenMode) VM.IsPenMode = true;
        }
    }
}
