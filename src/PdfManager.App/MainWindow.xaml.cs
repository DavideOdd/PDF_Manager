using System.Windows;
using System.Windows.Input;
using PdfManager.App.ViewModels;

namespace PdfManager.App;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private MainViewModel? VM => DataContext as MainViewModel;

    private void Window_DragOver(object sender, DragEventArgs e)
        => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            VM?.HandleFileDrop(files);
    }
}
