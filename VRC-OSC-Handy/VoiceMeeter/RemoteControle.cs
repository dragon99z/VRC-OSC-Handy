using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AtgDev.Voicemeeter;
using AtgDev.Voicemeeter.Utils;
using VRC_OSC_Handy.Logger;

namespace VRC_OSC_Handy.VoiceMeeter
{
    public class RemoteControle
    {

        RemoteApiWrapper vmrApi;
        public int type;

        /*
         * voicemeeter types
         * 1 = Standard
         * 2 = Banana
         * 3 = Potato
        */

        CancellationTokenSource updateToken = new CancellationTokenSource();
        CancellationToken ct;

        // Guards against double-Logout (harmless but avoids a second native call)
        // and lets multiple exit paths race to clean up safely.
        private int _loggedOut = 0;

        public RemoteControle()
        {
            ct = updateToken.Token;

            vmrApi = new RemoteApiWrapper(PathHelper.GetDllPath());
            vmrApi.Login();
            vmrApi.GetVoicemeeterType(out type);
            Task.Run(() => UpdateParams(vmrApi), updateToken.Token);

            // Best-effort safety net: release the VoiceMeeter Remote API slot on any
            // "normal" process exit path that might not go through MainWindow's
            // Closing handler (Environment.Exit, Windows logoff/shutdown, a
            // console-style termination request). This does NOT help if the process
            // is hard-killed (Task Manager "End Process"/taskkill /F, a native crash,
            // or a power loss) - at that point the OS tears the process down without
            // running any more managed code, so nothing in-process can call Logout().
            // VoiceMeeter itself must be restarted to reclaim a slot leaked that way.
            AppDomain.CurrentDomain.ProcessExit += (s, e) => LogOut();
            Microsoft.Win32.SystemEvents.SessionEnding += (s, e) => LogOut();
        }


        public void UpdateParams(RemoteApiWrapper remoteApi) 
        {
            while (!ct.IsCancellationRequested) {
                remoteApi.IsParametersDirty();
                vmrApi.GetVoicemeeterType(out type);
                if (Application.Current != null)
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if(Application.Current.MainWindow != null)
                        {
                            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                            List<Button> vmButtons = new List<Button>();
                            List<Slider> vmSlider = new List<Slider>();
                            List<TextBlock> vmTextBlock = new List<TextBlock>();
                            foreach (var child in mainWindow.VM_Controller.Children)
                            {
                                if (child is Button)
                                {
                                    vmButtons.Add(child as Button);
                                }
                                else if (child is Slider)
                                {
                                    vmSlider.Add(child as Slider);
                                }
                                else if (child is TextBlock)
                                {
                                    TextBlock textBox = (TextBlock)child;
                                    if (textBox.Uid.Contains(".Gain_Value"))
                                        vmTextBlock.Add(child as TextBlock);
                                }
                            }

                            foreach (Button button in vmButtons)
                            {
                                if (getBoolParameter(button.Uid))
                                    button.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                                else
                                    button.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                            }

                            foreach (Slider slider in vmSlider)
                            {
                                slider.ValueChanged -= mainWindow.vmValueChange;
                                slider.Value = getParameter(slider.Uid);
                                slider.ValueChanged += mainWindow.vmValueChange;
                            }

                            foreach (TextBlock textBlock in vmTextBlock)
                            {
                                textBlock.Text = Math.Round(getParameter(textBlock.Uid.Replace("_Value", "")), 2).ToString();
                            }
                        }
                        

                    });
                Thread.Sleep(50);
            }
            
        }

        public void toggleParameter(string parameter)
        {
            vmrApi.GetParameter(parameter, out float val);
            if(val == 0)
            {
                vmrApi.SetParameter(parameter, 1);
            }
            else if (val == 1)
            {
                vmrApi.SetParameter(parameter, 0);
            }
        }

        public void toggleBoolParameter(string parameter, bool val)
        {
            if (val)
            {
                vmrApi.SetParameter(parameter, 1);
            }
            else
            {
                vmrApi.SetParameter(parameter, 0);
            }
        }

        public void changeParameter(string parameter, float val)
        {
            vmrApi.SetParameter(parameter, val);
        }

        public float getParameter(string parameter)
        {
            vmrApi.GetParameter(parameter,out float val);
            return val;
        }

        public bool getBoolParameter(string parameter)
        {
            bool value;
            vmrApi.GetParameter(parameter, out float val);
            if(val >= 0.5)
            {
                value = true;
            }
            else
            {
                value = false;
            }
            return value;
        }

        public void LogOut()
        {
            // Idempotent: ProcessExit, SessionEnding, and the normal Window_Closing
            // path can all race to call this during shutdown.
            if (Interlocked.Exchange(ref _loggedOut, 1) != 0)
                return;

            updateToken.Cancel();
            try
            {
                vmrApi.Logout();
            }
            catch (Exception ex)
            {
                // Don't let a logout failure block the rest of app shutdown.
                DebugLogger.LogError("VoiceMeeter Logout failed", ex);
            }
        }

    }
}
