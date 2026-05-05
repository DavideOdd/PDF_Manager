using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using PdfManager.App.ViewModels;

namespace PdfManager.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Disable Ctrl+Z/Y when a TextBox has keyboard focus (avoids conflict while typing)
        PreviewKeyDown += (_, e) =>
        {
            if (e.KeyboardDevice.FocusedElement is System.Windows.Controls.TextBox or
                System.Windows.Controls.PasswordBox)
            {
                if (e.Key == System.Windows.Input.Key.Z && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    e.Handled = false; // let TextBox handle its own undo
                if (e.Key == System.Windows.Input.Key.Y && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    e.Handled = false;
                // Block tool shortcuts
                if (e.KeyboardDevice.Modifiers == ModifierKeys.None &&
                    e.Key is System.Windows.Input.Key.Delete or System.Windows.Input.Key.Escape)
                    e.Handled = false;
            }
        };
    }

    private MainViewModel? VM => DataContext as MainViewModel;

    private void Window_DragOver(object sender, DragEventArgs e)
        => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            VM?.HandleFileDrop(files);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (VM == null) return;
        var dirty = VM.OpenTabs.Where(t => t.IsDirty).ToList();
        if (dirty.Count == 0) return;

        var names = string.Join("\n", dirty.Select(t => $"  • {t.Title.TrimEnd(' ', '*')}"));
        var r = MessageBox.Show(
            $"Ci sono modifiche non salvate in:\n{names}\n\nVuoi salvare prima di uscire?",
            "Modifiche non salvate",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (r == MessageBoxResult.Cancel) { e.Cancel = true; return; }
        if (r == MessageBoxResult.Yes)
            foreach (var tab in dirty)
                tab.SaveCommand.Execute(null);
    }

    private void FileMenuBtn_Click(object sender, RoutedEventArgs e)  => FileMenu.IsOpen = !FileMenu.IsOpen;
    private void OrgMenuBtn_Click(object sender, RoutedEventArgs e)   => OrgMenu.IsOpen  = !OrgMenu.IsOpen;
    private void FileMenuItem_Click(object sender, RoutedEventArgs e) => FileMenu.IsOpen = false;
    private void OrgMenuItem_Click(object sender, RoutedEventArgs e)  => OrgMenu.IsOpen  = false;

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
