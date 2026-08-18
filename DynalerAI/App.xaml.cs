// Explicitly alias to avoid ambiguity between
// System.Windows.Forms.Application and System.Windows.Application
// (both are pulled in because UseWindowsForms=true is needed for screen capture)
using WpfApp = System.Windows.Application;

namespace DynalerAI;

public partial class App : WpfApp
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
