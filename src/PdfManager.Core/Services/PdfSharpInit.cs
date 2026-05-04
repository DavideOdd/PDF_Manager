using System.Text;

namespace PdfManager.Core.Services;

public static class PdfSharpInit
{
    private static bool _done;
    private static readonly object _lock = new();

    public static void EnsureRegistered()
    {
        if (_done) return;
        lock (_lock)
        {
            if (_done) return;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _done = true;
        }
    }
}
