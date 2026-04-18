#nullable disable

using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using LECommonLibrary;

namespace LEGUI;

public partial class AboutPanel : UserControl
{
    public AboutPanel()
    {
        InitializeComponent();
        tVersion.Text = GlobalHelper.GetVersionString();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
