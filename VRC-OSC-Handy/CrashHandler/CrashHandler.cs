using System;
using System.Diagnostics;
using System.IO;
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
            HandleFatalException("UI Thread Exception", e.Exception, "Application Error",
                $"An unexpected error occurred:\n\n{e.Exception.Message}", MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            HandleFatalException("Background Thread Exception", ex, "Application Crash",
                $"A critical error occurred:\n\n{ex?.Message}", MessageBoxImage.Error);
        }

        private static void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleFatalException("Task Unobserved Exception", e.Exception, "Task Error",
                $"An unobserved task error occurred:\n\n{e.Exception.Message}", MessageBoxImage.Warning);
            e.SetObserved();
        }

        // Shared by all three unhandled-exception sources: log it, tell the user, stop
        // and save application state, then optionally restart.
        private static void HandleFatalException(string logTitle, Exception ex, string messageBoxTitle, string userMessage, MessageBoxImage icon)
        {
            LogException(logTitle, ex);
            MessageBox.Show(userMessage, messageBoxTitle, MessageBoxButton.OK, icon);

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
