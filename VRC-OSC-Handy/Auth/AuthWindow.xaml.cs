using CefSharp.Wpf;
using CefSharp;
using System;
using System.Windows;
using System.Windows.Input;
using System.IO;

namespace VRC_OSC_Handy.Auth
{
    /// <summary>
    /// Interaktionslogik für AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            Init();
            InitializeComponent();
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public static void Init()
        {
            // Specify Global Settings and Command Line Arguments
            var settings = new CefSettings();

            // By default CEF uses an in memory cache, to save cached data e.g. to persist cookies you need to specify a cache path
            // NOTE: The executing user must have sufficient privileges to write to this folder.
            settings.CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache");

            // There are many command line arguments that can either be turned on or off
            settings.CefCommandLineArgs.Add("allow-scripts");

            // Enable WebRTC                            
            settings.CefCommandLineArgs.Add("enable-media-stream");

            //Disable GPU Acceleration
            settings.CefCommandLineArgs.Add("disable-gpu");

            // Don't use a proxy server, always make direct connections. Overrides any other proxy server flags that are passed.
            // Slightly improves Cef initialize time as it won't attempt to resolve a proxy
            settings.CefCommandLineArgs.Add("no-proxy-server");
            Cef.Initialize(settings);
        }

    }
}
