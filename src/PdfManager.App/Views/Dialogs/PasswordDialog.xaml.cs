using System.Windows;
using System.Windows.Input;

namespace PdfManager.App.Views.Dialogs;

public partial class PasswordDialog : Window
{
    public string? EnteredPassword { get; private set; }

    public PasswordDialog(string fileName)
    {
        InitializeComponent();
        PromptText.Text = $"Inserisci la password per aprire:\n{fileName}";
        Loaded += (_, _) => PwdBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        EnteredPassword = PwdBox.Password;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void PwdBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) Ok_Click(sender, e);
    }
}
