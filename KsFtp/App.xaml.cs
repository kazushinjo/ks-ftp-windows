using System.Text;
using System.Windows;

namespace KsFtp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Shift-JIS 等のコードページエンコーディングを .NET で使用可能にする
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        base.OnStartup(e);
    }
}
