using System.Windows;

namespace VRC_OSC_Handy
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize crash handler (auto-restart enabled, log file path optional)
            CrashHandler.CrashHandler.Initialize(autoRestart: false, logFilePath: "CrashLog.txt");
        }
    }
}
