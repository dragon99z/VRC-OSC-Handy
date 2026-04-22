using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace VRC_OSC_Handy.CrashHandler
{
    public static class CrashHandler
    {
        private static string _logFilePath = "CrashLog.txt";
        private static bool _autoRestart = false;

        public static void Initialize(bool autoRestart = false, string logFilePath = "CrashLog.txt")
        {
            _autoRestart = autoRestart;
            _logFilePath = logFilePath;

            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException("UI Thread Exception", e.Exception);
            MessageBox.Show($"An unexpected error occurred:\n\n{e.Exception.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;

            MainWindow.stopAll();
            MainWindow.saveAll();

            if (_autoRestart)
                RestartApplication();
        }

        private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogException("Background Thread Exception", ex);

            MessageBox.Show($"A critical error occurred:\n\n{ex?.Message}", "Application Crash", MessageBoxButton.OK, MessageBoxImage.Error);

            MainWindow.stopAll();
            MainWindow.saveAll();

            if (_autoRestart)
                RestartApplication();
        }

        private static void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException("Task Unobserved Exception", e.Exception);
            e.SetObserved();

            MessageBox.Show($"An unobserved task error occurred:\n\n{e.Exception.Message}", "Task Error", MessageBoxButton.OK, MessageBoxImage.Warning);

            MainWindow.stopAll();
            MainWindow.saveAll();

            if (_autoRestart)
                RestartApplication();
        }

        private static void LogException(string title, Exception ex)
        {
            try
            {
                var log = $"[{DateTime.Now}] {title}\n{ex}\n\n";
                File.AppendAllText(_logFilePath, log);
            }
            catch
            {
                // Failsafe: avoid recursive crash if file write fails.
            }
        }

        private static void RestartApplication()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(exePath);
            }
            catch
            {
                // Ignore restart failure
            }
        }
    }
}
