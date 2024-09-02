using System.Windows;

namespace Nuart.DeviceSamples
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            
        }

        protected override Window CreateShell()
        {
            return new MainView();
        }
    }
}