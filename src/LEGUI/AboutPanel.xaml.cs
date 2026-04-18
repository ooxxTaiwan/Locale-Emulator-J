#nullable disable

using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace LEGUI;

public partial class AboutPanel : UserControl
{
    public AboutPanel()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        tVersion.Text = version?.ToString(3) ?? "unknown";
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
