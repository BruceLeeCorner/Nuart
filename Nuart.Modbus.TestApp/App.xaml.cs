using System.Windows;

namespace Nuart.Modbus.TestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Xceed.Wpf.Toolkit.Licenser.LicenseKey = "WTK46-P1SP9-RR9GS-0RHA";
            base.OnStartup(e);
        }
        protected override Window CreateShell()
        {
            return new MainView();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<MainViewModel>();
        }
    }
}