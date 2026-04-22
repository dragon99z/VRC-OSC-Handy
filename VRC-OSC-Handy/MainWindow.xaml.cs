using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SpotifyAPI.Web;

using VRC_OSC_Handy.Auth;
using VRC_OSC_Handy.Particles;
using VRC_OSC_Handy.Update;
using VRC_OSC_Handy.VoiceMeeter;
using VRC_OSC_Handy.Osc;
using System.Threading;
using VRC_OSC_Handy.Wis;

using Whisper.net;
using Whisper.net.Ggml;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;
using VRC_OSC_Handy.NAudio;
using System.Collections.Generic;
using NAudio.Codecs;
using VRC_OSC_Handy.Config;
using Newtonsoft.Json.Serialization;
using VRC_OSC_Handy.Func;

namespace VRC_OSC_Handy
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static SpotifyClient spotify;
        public static JObject vrcJson;
        public static VRCParameterConfig vrcConfig;
        public static JObject configJson;
        public static InterfaceConfig config;
        public static double songTextWidth = 105;
        public static RemoteControle remoteControle;
        public static Wisper wisper;

        private ParticleSystem ps;
        private Point pMouse = new Point(0, 0);

        static CancellationTokenSource updateToken = new CancellationTokenSource();
        CancellationToken ct;

        static CancellationTokenSource updateVMToken = new CancellationTokenSource();
        CancellationToken vmt;

        static updateSpotify updateSpotify = new updateSpotify();
        static updateName updateName = new updateName();
        static updateProgess updateProgess = new updateProgess();
        static VRCOSC osc = new VRCOSC();

        public static string cfg_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/VRC Handy/";

        public static string modelPath = @"models\Base.bin";

        #region vmValDef

        TextBlock s0header;
        Button s0A1;
        Button s0A2;
        Button s0A3;
        Button s0A4;
        Button s0A5;
        Button s0B1;
        Button s0B2;
        Button s0B3;
        Button s0Mute;
        TextBlock s0GainHeader;
        TextBlock s0GainValue;
        Slider s0Gain;

        TextBlock s1header;
        Button s1A1;
        Button s1A2;
        Button s1A3;
        Button s1A4;
        Button s1A5;
        Button s1B1;
        Button s1B2;
        Button s1B3;
        Button s1Mute;
        TextBlock s1GainHeader;
        TextBlock s1GainValue;
        Slider s1Gain;

        TextBlock s2header;
        Button s2A1;
        Button s2A2;
        Button s2A3;
        Button s2A4;
        Button s2A5;
        Button s2B1;
        Button s2B2;
        Button s2B3;
        Button s2Mute;
        TextBlock s2GainHeader;
        TextBlock s2GainValue;
        Slider s2Gain;

        TextBlock s3header;
        Button s3A1;
        Button s3A2;
        Button s3A3;
        Button s3A4;
        Button s3A5;
        Button s3B1;
        Button s3B2;
        Button s3B3;
        Button s3Mute;
        TextBlock s3GainHeader;
        TextBlock s3GainValue;
        Slider s3Gain;

        TextBlock s4header;
        Button s4A1;
        Button s4A2;
        Button s4A3;
        Button s4A4;
        Button s4A5;
        Button s4B1;
        Button s4B2;
        Button s4B3;
        Button s4Mute;
        TextBlock s4GainHeader;
        TextBlock s4GainValue;
        Slider s4Gain;

        #endregion

        public MainWindow()
        {
            if (!Directory.Exists(cfg_path))
            {
                Directory.CreateDirectory(cfg_path);
            }

            genConfig("vrc_config.json", out vrcJson);
            vrcConfig = vrcJson.ToObject<VRCParameterConfig>();

            genConfig("config.json", out configJson);
            config = configJson.ToObject<InterfaceConfig>();

            remoteControle = new RemoteControle();
            wisper = new Wisper();
            InitializeComponent();
            this.Icon = ByteImageConverter.ByteToImage(Properties.Resources.image);
        }

        public void genConfig(string filename, out JObject json)
        {
            if (File.Exists(cfg_path + filename))
            {
                bool newValues = false;

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetName().Name.Replace("-", "_") + ".resource." + filename;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader recourceReader = new StreamReader(stream))
                using (StreamReader file = File.OpenText(cfg_path + filename))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    string result = recourceReader.ReadToEnd();
                    JObject internCFG = JObject.Parse(result);
                    json = (JObject)JToken.ReadFrom(reader);

                    foreach (var item in internCFG)
                    {
                        foreach (var item1 in (JObject)item.Value)
                        {
                            JObject objetc = (JObject)json[item.Key];
                            if (!objetc.ContainsKey(item1.Key))
                            {
                                objetc.Add(item1.Key, item1.Value);
                                json[item.Key] = objetc;
                                newValues = true;
                            }
                        }
                    }
                }

                if (newValues)
                    File.WriteAllText(cfg_path + filename, json.ToString());
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetName().Name.Replace("-","_") + ".resource." + filename;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    json = JObject.Parse(result);
                    File.WriteAllText(cfg_path + filename, json.ToString());
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ps = new ParticleSystem(5, 25, 5, 100, 75, this.cvs_particleContainer, this.grid_lineContainer);
            //Register frame animation
            CompositionTarget.Rendering += CompositionTarget_Rendering;


            if (config.SpotifyConfig.Enabled && config.SpotifyConfig.ClientID != "your-client-id" && config.SpotifyConfig.ClientSecret != "your-client-secret")
            {
                SpotifyAuth auth = new SpotifyAuth();
                auth.runAuth();
                update();
            }
            
            
            osc.Run(remoteControle, spotify, wisper, modelPath);
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            pMouse = e.GetPosition(this.cvs_particleContainer);
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            ps.ParticleRoamUpdate(pMouse);
            ps.AddOrRemoveParticleLine();
            ps.MoveParticleLine();
        }

        private void update()
        {
            updateSpotify.run(spotify);

            updateName.run(Song, this.Icon);

            updateProgess.run(ProgressBar);
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void WindowClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void PlayNext(object sender, RoutedEventArgs e)
        {
            if (config.SpotifyConfig.Enabled && spotify != null)
                spotify.Player.SkipNext();
        }

        private void PlayPauseToggle(object sender, RoutedEventArgs e)
        {
            if (config.SpotifyConfig.Enabled && spotify != null)
            {
                var PlayBack = updateSpotify.track;
                if (PlayBack != null)
                {
                    if (PlayBack.IsPlaying)
                    {
                        spotify.Player.PausePlayback();
                    }
                    else
                    {
                        spotify.Player.ResumePlayback();
                    }
                }
            }
            
        }

        private void PlayLast(object sender, RoutedEventArgs e)
        {
            if (config.SpotifyConfig.Enabled && spotify != null)
                spotify.Player.SkipPrevious();
        }
        
        private void VM_Controller_Loaded(object sender, RoutedEventArgs e)
        {
            vmt = updateVMToken.Token;
            int type = remoteControle.type;
            LoadVMSettings(type);
            Task.Run(() =>
            {
                while (!vmt.IsCancellationRequested)
                {
                    if(type != remoteControle.type)
                    {
                        type = remoteControle.type;

                        var uiAccess = VM_Controller.Dispatcher.CheckAccess();

                        if (uiAccess)
                        {
                            if (ct.IsCancellationRequested)
                                break;
                            VM_Controller.Children.Clear();
                            LoadVMSettings(type);
                        }
                        else
                        {
                            if (ct.IsCancellationRequested)
                                break;
                            VM_Controller.Dispatcher.Invoke(() => { VM_Controller.Children.Clear(); LoadVMSettings(type); });
                        }
                    }
                    Thread.Sleep(1000);
                }
            }, updateVMToken.Token);
        }

        private void LoadVMSettings(int type)
        {
            switch (type)
            {
                default:
                    s0header = new TextBlock();
                    s0header.Text = "Voicemeeter not found!";
                    s0header.Margin = new Thickness(30, 10, 0, 0);
                    s0header.Background = Brushes.Transparent;
                    s0header.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0header.HorizontalAlignment = HorizontalAlignment.Left;
                    s0header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0header);
                    break;
                case 1:
                    //Standard
                    #region Strip 0
                    s0header = new TextBlock();
                    s0header.Text = "Strip 0";
                    s0header.Width = 65;
                    s0header.Height = 30;
                    s0header.Margin = new Thickness(0, 10, 290, 0);
                    s0header.Background = Brushes.Transparent;
                    s0header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s0header.HorizontalAlignment = HorizontalAlignment.Right;
                    s0header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0header);

                    #region A
                    s0A1 = new Button();
                    s0A1.Margin = new Thickness(0, 35, 310, 0);
                    s0A1.Width = 65;
                    s0A1.Height = 30;
                    s0A1.Content = "A1";
                    s0A1.Uid = "Strip[0].A1";
                    s0A1.Style = Resources["RoundedButton"] as Style;
                    s0A1.Background = Brushes.Transparent;
                    if(remoteControle.getBoolParameter("Strip[0].A1"))
                        s0A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0A1.BorderBrush = null;
                    s0A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s0A1.VerticalAlignment = VerticalAlignment.Top;
                    s0A1.Click += vmToggle;
                    s0A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0A1);
                    #endregion

                    #region B
                    s0B1 = new Button();
                    s0B1.Margin = new Thickness(0, 65, 310, 0);
                    s0B1.Width = 65;
                    s0B1.Height = 30;
                    s0B1.Content = "B1";
                    s0B1.Uid = "Strip[0].B1";
                    s0B1.Style = Resources["RoundedButton"] as Style;
                    s0B1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].B1"))
                        s0B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0B1.BorderBrush = null;
                    s0B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s0B1.VerticalAlignment = VerticalAlignment.Top;
                    s0B1.Click += vmToggle;
                    s0B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0B1);

                    #endregion

                    #region other
                    s0Mute = new Button();
                    s0Mute.Margin = new Thickness(0, 95, 310, 0);
                    s0Mute.Width = 65;
                    s0Mute.Height = 30;
                    s0Mute.Content = "Mute";
                    s0Mute.Uid = "Strip[0].Mute";
                    s0Mute.Style = Resources["RoundedButton"] as Style;
                    s0Mute.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].Mute"))
                        s0Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0Mute.BorderBrush = null;
                    s0Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s0Mute.VerticalAlignment = VerticalAlignment.Top;
                    s0Mute.Click += vmToggle;
                    s0Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0Mute);

                    s0GainHeader = new TextBlock();
                    s0GainHeader.Text = "Gain";
                    s0GainHeader.Width = 65;
                    s0GainHeader.Height = 30;
                    s0GainHeader.Margin = new Thickness(0, 130, 290, 0);
                    s0GainHeader.Background = Brushes.Transparent;
                    s0GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s0GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s0GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0GainHeader);

                    s0GainValue = new TextBlock();
                    s0GainValue.Text = Math.Round(remoteControle.getParameter("Strip[1].Gain"), 2).ToString();
                    s0GainValue.Uid = "Strip[0].Gain_Value";
                    s0GainValue.Width = 65;
                    s0GainValue.Height = 30;
                    s0GainValue.Margin = new Thickness(0, 160, 290, 0);
                    s0GainValue.Background = Brushes.Transparent;
                    s0GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s0GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s0GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0GainValue);

                    s0Gain = new Slider();
                    s0Gain.Margin = new Thickness(0, 190, 310, 0);
                    s0Gain.Width = 65;
                    s0Gain.Height = 30;
                    s0Gain.Uid = "Strip[0].Gain";
                    s0Gain.Maximum = 12;
                    s0Gain.Minimum = -60;
                    s0Gain.Value = remoteControle.getParameter("Strip[0].Gain");
                    s0Gain.LargeChange = 0.5;
                    s0Gain.SmallChange = 0.01;
                    s0Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s0Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s0Gain.VerticalAlignment = VerticalAlignment.Top;
                    s0Gain.ValueChanged += vmValueChange;
                    s0Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0Gain);
                    #endregion

                    #endregion

                    #region Strip 1
                    s1header = new TextBlock();
                    s1header.Text = "Strip 1";
                    s1header.Width = 65;
                    s1header.Height = 30;
                    s1header.Margin = new Thickness(0, 10, 220, 0);
                    s1header.Background = Brushes.Transparent;
                    s1header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1header.HorizontalAlignment = HorizontalAlignment.Right;
                    s1header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s1header);

                    #region A
                    s1A1 = new Button();
                    s1A1.Margin = new Thickness(0, 35, 235, 0);
                    s1A1.Width = 65;
                    s1A1.Height = 30;
                    s1A1.Content = "A1";
                    s1A1.Uid = "Strip[1].A1";
                    s1A1.Style = Resources["RoundedButton"] as Style;
                    s1A1.Background = Brushes.Transparent;
                    s1A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1A1.BorderBrush = null;
                    s1A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s1A1.VerticalAlignment = VerticalAlignment.Top;
                    s1A1.Click += vmToggle;
                    s1A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1A1);

                    #endregion

                    #region B
                    s1B1 = new Button();
                    s1B1.Margin = new Thickness(0, 65, 235, 0);
                    s1B1.Width = 65;
                    s1B1.Height = 30;
                    s1B1.Content = "B1";
                    s1B1.Uid = "Strip[1].B1";
                    s1B1.Style = Resources["RoundedButton"] as Style;
                    s1B1.Background = Brushes.Transparent;
                    s1B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1B1.BorderBrush = null;
                    s1B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s1B1.VerticalAlignment = VerticalAlignment.Top;
                    s1B1.Click += vmToggle;
                    s1B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1B1);

                    #endregion

                    #region other
                    s1Mute = new Button();
                    s1Mute.Margin = new Thickness(0, 95, 235, 0);
                    s1Mute.Width = 65;
                    s1Mute.Height = 30;
                    s1Mute.Content = "Mute";
                    s1Mute.Uid = "Strip[1].Mute";
                    s1Mute.Style = Resources["RoundedButton"] as Style;
                    s1Mute.Background = Brushes.Transparent;
                    s1Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1Mute.BorderBrush = null;
                    s1Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s1Mute.VerticalAlignment = VerticalAlignment.Top;
                    s1Mute.Click += vmToggle;
                    s1Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1Mute);

                    s1GainHeader = new TextBlock();
                    s1GainHeader.Text = "Gain";
                    s1GainHeader.Width = 65;
                    s1GainHeader.Height = 30;
                    s1GainHeader.Margin = new Thickness(0, 130, 215, 0);
                    s1GainHeader.Background = Brushes.Transparent;
                    s1GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s1GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s1GainHeader);

                    s1GainValue = new TextBlock();
                    s1GainValue.Text = Math.Round(remoteControle.getParameter("Strip[1].Gain"), 2).ToString();
                    s1GainValue.Uid = "Strip[1].Gain_Value";
                    s1GainValue.Width = 65;
                    s1GainValue.Height = 30;
                    s1GainValue.Margin = new Thickness(0, 160, 215, 0);
                    s1GainValue.Background = Brushes.Transparent;
                    s1GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s1GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s1GainValue);

                    s1Gain = new Slider();
                    s1Gain.Margin = new Thickness(0, 190, 235, 0);
                    s1Gain.Width = 55;
                    s1Gain.Height = 30;
                    s1Gain.Uid = "Strip[1].Gain";
                    s1Gain.Maximum = 12;
                    s1Gain.Minimum = -60;
                    s1Gain.Value = remoteControle.getParameter("Strip[1].Gain");
                    s1Gain.LargeChange = 0.5;
                    s1Gain.SmallChange = 0.01;
                    s1Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s1Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s1Gain.VerticalAlignment = VerticalAlignment.Top;
                    s1Gain.ValueChanged += vmValueChange;
                    s1Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1Gain);
                    #endregion

                    #endregion
                    break;
                case 2:
                    //Banana
                    #region Strip 0
                    s0header = new TextBlock();
                    s0header.Text = "Strip 0";
                    s0header.Width = 65;
                    s0header.Height = 30;
                    s0header.Margin = new Thickness(0, 10, 290, 0);
                    s0header.Background = Brushes.Transparent;
                    s0header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s0header.HorizontalAlignment = HorizontalAlignment.Right;
                    s0header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0header);

                    #region A
                    s0A1 = new Button();
                    s0A1.Margin = new Thickness(0, 35, 310, 0);
                    s0A1.Width = 65;
                    s0A1.Height = 30;
                    s0A1.Content = "A1";
                    s0A1.Uid = "Strip[0].A1";
                    s0A1.Style = Resources["RoundedButton"] as Style;
                    s0A1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].A1"))
                        s0A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0A1.BorderBrush = null;
                    s0A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s0A1.VerticalAlignment = VerticalAlignment.Top;
                    s0A1.Click += vmToggle;
                    s0A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0A1);

                    s0A2 = new Button();
                    s0A2.Margin = new Thickness(0, 65, 310, 0);
                    s0A2.Width = 65;
                    s0A2.Height = 30;
                    s0A2.Content = "A2";
                    s0A2.Uid = "Strip[0].A2";
                    s0A2.Style = Resources["RoundedButton"] as Style;
                    s0A2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].A2"))
                        s0A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0A2.BorderBrush = null;
                    s0A2.HorizontalAlignment = HorizontalAlignment.Right;
                    s0A2.VerticalAlignment = VerticalAlignment.Top;
                    s0A2.Click += vmToggle;
                    s0A2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0A2);

                    s0A3 = new Button();
                    s0A3.Margin = new Thickness(0, 95, 310, 0);
                    s0A3.Width = 65;
                    s0A3.Height = 30;
                    s0A3.Content = "A3";
                    s0A3.Uid = "Strip[0].A3";
                    s0A3.Style = Resources["RoundedButton"] as Style;
                    s0A3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].A3"))
                        s0A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0A3.BorderBrush = null;
                    s0A3.HorizontalAlignment = HorizontalAlignment.Right;
                    s0A3.VerticalAlignment = VerticalAlignment.Top;
                    s0A3.Click += vmToggle;
                    s0A3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0A3);
                    #endregion

                    #region B
                    s0B1 = new Button();
                    s0B1.Margin = new Thickness(0, 125, 310, 0);
                    s0B1.Width = 65;
                    s0B1.Height = 30;
                    s0B1.Content = "B1";
                    s0B1.Uid = "Strip[0].B1";
                    s0B1.Style = Resources["RoundedButton"] as Style;
                    s0B1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].B1"))
                        s0B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0B1.BorderBrush = null;
                    s0B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s0B1.VerticalAlignment = VerticalAlignment.Top;
                    s0B1.Click += vmToggle;
                    s0B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0B1);

                    s0B2 = new Button();
                    s0B2.Margin = new Thickness(0, 155, 310, 0);
                    s0B2.Width = 65;
                    s0B2.Height = 30;
                    s0B2.Content = "B2";
                    s0B2.Uid = "Strip[0].B2";
                    s0B2.Style = Resources["RoundedButton"] as Style;
                    s0B2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].B2"))
                        s0B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0B2.BorderBrush = null;
                    s0B2.HorizontalAlignment = HorizontalAlignment.Right;
                    s0B2.VerticalAlignment = VerticalAlignment.Top;
                    s0B2.Click += vmToggle;
                    s0B2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0B2);
                    #endregion

                    #region other
                    s0Mute = new Button();
                    s0Mute.Margin = new Thickness(0, 185, 310, 0);
                    s0Mute.Width = 65;
                    s0Mute.Height = 30;
                    s0Mute.Content = "Mute";
                    s0Mute.Uid = "Strip[0].Mute";
                    s0Mute.Style = Resources["RoundedButton"] as Style;
                    s0Mute.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].Mute"))
                        s0Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0Mute.BorderBrush = null;
                    s0Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s0Mute.VerticalAlignment = VerticalAlignment.Top;
                    s0Mute.Click += vmToggle;
                    s0Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0Mute);

                    s0GainHeader = new TextBlock();
                    s0GainHeader.Text = "Gain";
                    s0GainHeader.Width = 65;
                    s0GainHeader.Height = 30;
                    s0GainHeader.Margin = new Thickness(0, 220, 290, 0);
                    s0GainHeader.Background = Brushes.Transparent;
                    s0GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s0GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s0GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0GainHeader);

                    s0GainValue = new TextBlock();
                    s0GainValue.Text = Math.Round(remoteControle.getParameter("Strip[1].Gain"), 2).ToString();
                    s0GainValue.Uid = "Strip[0].Gain_Value";
                    s0GainValue.Width = 65;
                    s0GainValue.Height = 30;
                    s0GainValue.Margin = new Thickness(0, 250, 290, 0);
                    s0GainValue.Background = Brushes.Transparent;
                    s0GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s0GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s0GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0GainValue);

                    s0Gain = new Slider();
                    s0Gain.Margin = new Thickness(0, 275, 310, 0);
                    s0Gain.Width = 65;
                    s0Gain.Height = 30;
                    s0Gain.Uid = "Strip[0].Gain";
                    s0Gain.Maximum = 12;
                    s0Gain.Minimum = -60;
                    s0Gain.Value = remoteControle.getParameter("Strip[0].Gain");
                    s0Gain.LargeChange = 0.5;
                    s0Gain.SmallChange = 0.01;
                    s0Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s0Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s0Gain.VerticalAlignment = VerticalAlignment.Top;
                    s0Gain.ValueChanged += vmValueChange;
                    s0Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0Gain);
                    #endregion

                    #endregion

                    #region Strip 1
                    s1header = new TextBlock();
                    s1header.Text = "Strip 1";
                    s1header.Width = 65;
                    s1header.Height = 30;
                    s1header.Margin = new Thickness(0, 10, 220, 0);
                    s1header.Background = Brushes.Transparent;
                    s1header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1header.HorizontalAlignment = HorizontalAlignment.Right;
                    s1header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s1header);

                    #region A
                    s1A1 = new Button();
                    s1A1.Margin = new Thickness(0, 35, 235, 0);
                    s1A1.Width = 65;
                    s1A1.Height = 30;
                    s1A1.Content = "A1";
                    s1A1.Uid = "Strip[1].A1";
                    s1A1.Style = Resources["RoundedButton"] as Style;
                    s1A1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A1"))
                        s1A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1A1.BorderBrush = null;
                    s1A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s1A1.VerticalAlignment = VerticalAlignment.Top;
                    s1A1.Click += vmToggle;
                    s1A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1A1);

                    s1A2 = new Button();
                    s1A2.Margin = new Thickness(0, 65, 235, 0);
                    s1A2.Width = 65;
                    s1A2.Height = 30;
                    s1A2.Content = "A2";
                    s1A2.Uid = "Strip[1].A2";
                    s1A2.Style = Resources["RoundedButton"] as Style;
                    s1A2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A2"))
                        s1A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1A2.BorderBrush = null;
                    s1A2.HorizontalAlignment = HorizontalAlignment.Right;
                    s1A2.VerticalAlignment = VerticalAlignment.Top;
                    s1A2.Click += vmToggle;
                    s1A2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1A2);

                    s1A3 = new Button();
                    s1A3.Margin = new Thickness(0, 95, 235, 0);
                    s1A3.Width = 65;
                    s1A3.Height = 30;
                    s1A3.Content = "A3";
                    s1A3.Uid = "Strip[1].A3";
                    s1A3.Style = Resources["RoundedButton"] as Style;
                    s1A3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A3"))
                        s1A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1A3.BorderBrush = null;
                    s1A3.HorizontalAlignment = HorizontalAlignment.Right;
                    s1A3.VerticalAlignment = VerticalAlignment.Top;
                    s1A3.Click += vmToggle;
                    s1A3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1A3);
                    #endregion

                    #region B
                    s1B1 = new Button();
                    s1B1.Margin = new Thickness(0, 125, 235, 0);
                    s1B1.Width = 65;
                    s1B1.Height = 30;
                    s1B1.Content = "B1";
                    s1B1.Uid = "Strip[1].B1";
                    s1B1.Style = Resources["RoundedButton"] as Style;
                    s1B1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].B1"))
                        s1B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1B1.BorderBrush = null;
                    s1B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s1B1.VerticalAlignment = VerticalAlignment.Top;
                    s1B1.Click += vmToggle;
                    s1B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1B1);

                    s1B2 = new Button();
                    s1B2.Margin = new Thickness(0, 155, 235, 0);
                    s1B2.Width = 65;
                    s1B2.Height = 30;
                    s1B2.Content = "B2";
                    s1B2.Uid = "Strip[1].B2";
                    s1B2.Style = Resources["RoundedButton"] as Style;
                    s1B2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].B2"))
                        s1B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1B2.BorderBrush = null;
                    s1B2.HorizontalAlignment = HorizontalAlignment.Right;
                    s1B2.VerticalAlignment = VerticalAlignment.Top;
                    s1B2.Click += vmToggle;
                    s1B2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1B2);
                    #endregion

                    #region other
                    s1Mute = new Button();
                    s1Mute.Margin = new Thickness(0, 185, 235, 0);
                    s1Mute.Width = 65;
                    s1Mute.Height = 30;
                    s1Mute.Content = "Mute";
                    s1Mute.Uid = "Strip[1].Mute";
                    s1Mute.Style = Resources["RoundedButton"] as Style;
                    s1Mute.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].Mute"))
                        s1Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1Mute.BorderBrush = null;
                    s1Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s1Mute.VerticalAlignment = VerticalAlignment.Top;
                    s1Mute.Click += vmToggle;
                    s1Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1Mute);

                    s1GainHeader = new TextBlock();
                    s1GainHeader.Text = "Gain";
                    s1GainHeader.Width = 65;
                    s1GainHeader.Height = 30;
                    s1GainHeader.Margin = new Thickness(0, 220, 215, 0);
                    s1GainHeader.Background = Brushes.Transparent;
                    s1GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s1GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s1GainHeader);

                    s1GainValue = new TextBlock();
                    s1GainValue.Text = Math.Round(remoteControle.getParameter("Strip[1].Gain"), 2).ToString();
                    s1GainValue.Uid = "Strip[1].Gain_Value";
                    s1GainValue.Width = 65;
                    s1GainValue.Height = 30;
                    s1GainValue.Margin = new Thickness(0, 250, 215, 0);
                    s1GainValue.Background = Brushes.Transparent;
                    s1GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s1GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s1GainValue);

                    s1Gain = new Slider();
                    s1Gain.Margin = new Thickness(0, 275, 235, 0);
                    s1Gain.Width = 65;
                    s1Gain.Height = 30;
                    s1Gain.Uid = "Strip[1].Gain";
                    s1Gain.Maximum = 12;
                    s1Gain.Minimum = -60;
                    s1Gain.Value = remoteControle.getParameter("Strip[1].Gain");
                    s1Gain.LargeChange = 0.5;
                    s1Gain.SmallChange = 0.01;
                    s1Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s1Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s1Gain.VerticalAlignment = VerticalAlignment.Top;
                    s1Gain.ValueChanged += vmValueChange;
                    s1Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1Gain);
                    #endregion

                    #endregion

                    #region Strip 2
                    s2header = new TextBlock();
                    s2header.Text = "Strip 2";
                    s2header.Width = 65;
                    s2header.Height = 30;
                    s2header.Margin = new Thickness(0, 10, 140, 0);
                    s2header.Background = Brushes.Transparent;
                    s2header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s2header.HorizontalAlignment = HorizontalAlignment.Right;
                    s2header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s2header);

                    #region A
                    s2A1 = new Button();
                    s2A1.Margin = new Thickness(0, 35, 160, 0);
                    s2A1.Width = 65;
                    s2A1.Height = 30;
                    s2A1.Content = "A1";
                    s2A1.Uid = "Strip[2].A1";
                    s2A1.Style = Resources["RoundedButton"] as Style;
                    s2A1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[2].A1"))
                        s2A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2A1.BorderBrush = null;
                    s2A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s2A1.VerticalAlignment = VerticalAlignment.Top;
                    s2A1.Click += vmToggle;
                    s2A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2A1);

                    s2A2 = new Button();
                    s2A2.Margin = new Thickness(0, 65, 160, 0);
                    s2A2.Width = 65;
                    s2A2.Height = 30;
                    s2A2.Content = "A2";
                    s2A2.Uid = "Strip[2].A2";
                    s2A2.Style = Resources["RoundedButton"] as Style;
                    s2A2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[2].A2"))
                        s2A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2A2.BorderBrush = null;
                    s2A2.HorizontalAlignment = HorizontalAlignment.Right;
                    s2A2.VerticalAlignment = VerticalAlignment.Top;
                    s2A2.Click += vmToggle;
                    s2A2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2A2);

                    s2A3 = new Button();
                    s2A3.Margin = new Thickness(0, 95, 160, 0);
                    s2A3.Width = 65;
                    s2A3.Height = 30;
                    s2A3.Content = "A3";
                    s2A3.Uid = "Strip[2].A3";
                    s2A3.Style = Resources["RoundedButton"] as Style;
                    s2A3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[2].A3"))
                        s2A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2A3.BorderBrush = null;
                    s2A3.HorizontalAlignment = HorizontalAlignment.Right;
                    s2A3.VerticalAlignment = VerticalAlignment.Top;
                    s2A3.Click += vmToggle;
                    s2A3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2A3);
                    #endregion

                    #region B
                    s2B1 = new Button();
                    s2B1.Margin = new Thickness(0, 125, 160, 0);
                    s2B1.Width = 65;
                    s2B1.Height = 30;
                    s2B1.Content = "B1";
                    s2B1.Uid = "Strip[2].B1";
                    if (remoteControle.getBoolParameter("Strip[2].B1"))
                        s2B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2B1.Background = Brushes.Transparent;
                    s2B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s2B1.BorderBrush = null;
                    s2B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s2B1.VerticalAlignment = VerticalAlignment.Top;
                    s2B1.Click += vmToggle;
                    s2B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2B1);

                    s2B2 = new Button();
                    s2B2.Margin = new Thickness(0, 155, 160, 0);
                    s2B2.Width = 65;
                    s2B2.Height = 30;
                    s2B2.Content = "B2";
                    s2B2.Uid = "Strip[2].B2";
                    s2B2.Style = Resources["RoundedButton"] as Style;
                    s2B2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[2].B2"))
                        s2B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2B2.BorderBrush = null;
                    s2B2.HorizontalAlignment = HorizontalAlignment.Right;
                    s2B2.VerticalAlignment = VerticalAlignment.Top;
                    s2B2.Click += vmToggle;
                    s2B2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2B2);
                    #endregion

                    #region other
                    s2Mute = new Button();
                    s2Mute.Margin = new Thickness(0, 185, 160, 0);
                    s2Mute.Width = 65;
                    s2Mute.Height = 30;
                    s2Mute.Content = "Mute";
                    s2Mute.Uid = "Strip[2].Mute";
                    s2Mute.Style = Resources["RoundedButton"] as Style;
                    s2Mute.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[2].Mute"))
                        s2Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2Mute.BorderBrush = null;
                    s2Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s2Mute.VerticalAlignment = VerticalAlignment.Top;
                    s2Mute.Click += vmToggle;
                    s2Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2Mute);

                    s2GainHeader = new TextBlock();
                    s2GainHeader.Text = "Gain";
                    s2GainHeader.Width = 65;
                    s2GainHeader.Height = 30;
                    s2GainHeader.Margin = new Thickness(0, 220, 140, 0);
                    s2GainHeader.Background = Brushes.Transparent;
                    s2GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s2GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s2GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s2GainHeader);

                    s2GainValue = new TextBlock();
                    s2GainValue.Text = Math.Round(remoteControle.getParameter("Strip[2].Gain"), 2).ToString();
                    s2GainValue.Uid = "Strip[2].Gain_Value";
                    s2GainValue.Width = 65;
                    s2GainValue.Height = 30;
                    s2GainValue.Margin = new Thickness(0, 250, 140, 0);
                    s2GainValue.Background = Brushes.Transparent;
                    s2GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s2GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s2GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s2GainValue);

                    s2Gain = new Slider();
                    s2Gain.Margin = new Thickness(0, 275, 160, 0);
                    s2Gain.Width = 65;
                    s2Gain.Height = 30;
                    s2Gain.Uid = "Strip[2].Gain";
                    s2Gain.Maximum = 12;
                    s2Gain.Minimum = -60;
                    s2Gain.Value = remoteControle.getParameter("Strip[2].Gain");
                    s2Gain.LargeChange = 0.5;
                    s2Gain.SmallChange = 0.01;
                    s2Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s2Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s2Gain.VerticalAlignment = VerticalAlignment.Top;
                    s2Gain.ValueChanged += vmValueChange;
                    s2Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2Gain);
                    #endregion

                    #endregion
                    break;
                case 3:
                    //Potato
                    #region Strip 0
                    s0header = new TextBlock();
                    s0header.Text = "Strip 0";
                    s0header.Width = 65;
                    s0header.Height = 30;
                    s0header.Margin = new Thickness(0, 10, 290, 0);
                    s0header.Background = Brushes.Transparent;
                    s0header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s0header.HorizontalAlignment = HorizontalAlignment.Right;
                    s0header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0header);

                    #region A
                    s0A1 = new Button();
                    s0A1.Margin = new Thickness(0, 35, 310, 0);
                    s0A1.Width = 65;
                    s0A1.Height = 30;
                    s0A1.Content = "A1";
                    s0A1.Uid = "Strip[0].A1";
                    s0A1.Style = Resources["RoundedButton"] as Style;
                    s0A1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].A1"))
                        s0A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0A1.BorderBrush = null;
                    s0A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s0A1.VerticalAlignment = VerticalAlignment.Top;
                    s0A1.Click += vmToggle;
                    s0A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0A1);

                    s0A2 = new Button();
                    s0A2.Margin = new Thickness(0, 65, 310, 0);
                    s0A2.Width = 65;
                    s0A2.Height = 30;
                    s0A2.Content = "A2";
                    s0A2.Uid = "Strip[0].A2";
                    s0A2.Style = Resources["RoundedButton"] as Style;
                    s0A2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].A2"))
                        s0A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0A2.BorderBrush = null;
                    s0A2.HorizontalAlignment = HorizontalAlignment.Right;
                    s0A2.VerticalAlignment = VerticalAlignment.Top;
                    s0A2.Click += vmToggle;
                    s0A2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0A2);

                    s0A3 = new Button();
                    s0A3.Margin = new Thickness(0, 95, 310, 0);
                    s0A3.Width = 65;
                    s0A3.Height = 30;
                    s0A3.Content = "A3";
                    s0A3.Uid = "Strip[0].A3";
                    s0A3.Style = Resources["RoundedButton"] as Style;
                    s0A3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].A3"))
                        s0A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0A3.BorderBrush = null;
                    s0A3.HorizontalAlignment = HorizontalAlignment.Right;
                    s0A3.VerticalAlignment = VerticalAlignment.Top;
                    s0A3.Click += vmToggle;
                    s0A3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0A3);

                    s0A4 = new Button();
                    s0A4.Margin = new Thickness(0, 125, 310, 0);
                    s0A4.Width = 65;
                    s0A4.Height = 30;
                    s0A4.Content = "A4";
                    s0A4.Uid = "Strip[0].A4";
                    s0A4.Style = Resources["RoundedButton"] as Style;
                    s0A4.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].A4"))
                        s0A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0A4.BorderBrush = null;
                    s0A4.HorizontalAlignment = HorizontalAlignment.Right;
                    s0A4.VerticalAlignment = VerticalAlignment.Top;
                    s0A4.Click += vmToggle;
                    s0A4.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0A4);

                    s0A5 = new Button();
                    s0A5.Margin = new Thickness(0, 155, 310, 0);
                    s0A5.Width = 65;
                    s0A5.Height = 30;
                    s0A5.Content = "A5";
                    s0A5.Uid = "Strip[0].A5";
                    s0A5.Style = Resources["RoundedButton"] as Style;
                    s0A5.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].A5"))
                        s0A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0A5.BorderBrush = null;
                    s0A5.HorizontalAlignment = HorizontalAlignment.Right;
                    s0A5.VerticalAlignment = VerticalAlignment.Top;
                    s0A5.Click += vmToggle;
                    s0A5.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0A5);
                    #endregion

                    #region B
                    s0B1 = new Button();
                    s0B1.Margin = new Thickness(0, 185, 310, 0);
                    s0B1.Width = 65;
                    s0B1.Height = 30;
                    s0B1.Content = "B1";
                    s0B1.Uid = "Strip[0].B1";
                    s0B1.Style = Resources["RoundedButton"] as Style;
                    s0B1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].B1"))
                        s0B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0B1.BorderBrush = null;
                    s0B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s0B1.VerticalAlignment = VerticalAlignment.Top;
                    s0B1.Click += vmToggle;
                    s0B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0B1);

                    s0B2 = new Button();
                    s0B2.Margin = new Thickness(0, 215, 310, 0);
                    s0B2.Width = 65;
                    s0B2.Height = 30;
                    s0B2.Content = "B2";
                    s0B2.Uid = "Strip[0].B2";
                    s0B2.Style = Resources["RoundedButton"] as Style;
                    s0B2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].B2"))
                        s0B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0B2.BorderBrush = null;
                    s0B2.HorizontalAlignment = HorizontalAlignment.Right;
                    s0B2.VerticalAlignment = VerticalAlignment.Top;
                    s0B2.Click += vmToggle;
                    s0B2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0B2);

                    s0B3 = new Button();
                    s0B3.Margin = new Thickness(0, 245, 310, 0);
                    s0B3.Width = 65;
                    s0B3.Height = 30;
                    s0B3.Content = "B3";
                    s0B3.Uid = "Strip[0].B3";
                    s0B3.Style = Resources["RoundedButton"] as Style;
                    s0B3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].B3"))
                        s0B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0B3.BorderBrush = null;
                    s0B3.HorizontalAlignment = HorizontalAlignment.Right;
                    s0B3.VerticalAlignment = VerticalAlignment.Top;
                    s0B3.Click += vmToggle;
                    s0B3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0B3);
                    #endregion

                    #region other
                    s0Mute = new Button();
                    s0Mute.Margin = new Thickness(0, 275, 310, 0);
                    s0Mute.Width = 65;
                    s0Mute.Height = 30;
                    s0Mute.Content = "Mute";
                    s0Mute.Uid = "Strip[0].Mute";
                    s0Mute.Style = Resources["RoundedButton"] as Style;
                    s0Mute.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[0].Mute"))
                        s0Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s0Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s0Mute.BorderBrush = null;
                    s0Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s0Mute.VerticalAlignment = VerticalAlignment.Top;
                    s0Mute.Click += vmToggle;
                    s0Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0Mute);

                    s0GainHeader = new TextBlock();
                    s0GainHeader.Text = "Gain";
                    s0GainHeader.Width = 65;
                    s0GainHeader.Height = 30;
                    s0GainHeader.Margin = new Thickness(0, 305, 290, 0);
                    s0GainHeader.Background = Brushes.Transparent;
                    s0GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s0GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s0GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0GainHeader);

                    s0GainValue = new TextBlock();
                    s0GainValue.Text = Math.Round(remoteControle.getParameter("Strip[1].Gain"), 2).ToString();
                    s0GainValue.Uid = "Strip[0].Gain_Value";
                    s0GainValue.Width = 65;
                    s0GainValue.Height = 30;
                    s0GainValue.Margin = new Thickness(0, 325, 290, 0);
                    s0GainValue.Background = Brushes.Transparent;
                    s0GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s0GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s0GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s0GainValue);

                    s0Gain = new Slider();
                    s0Gain.Margin = new Thickness(0, 350, 310, 0);
                    s0Gain.Width = 65;
                    s0Gain.Height = 30;
                    s0Gain.Uid = "Strip[0].Gain";
                    s0Gain.Maximum = 12;
                    s0Gain.Minimum = -60;
                    s0Gain.Value = remoteControle.getParameter("Strip[0].Gain");
                    s0Gain.LargeChange = 0.5;
                    s0Gain.SmallChange = 0.01;
                    s0Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s0Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s0Gain.VerticalAlignment = VerticalAlignment.Top;
                    s0Gain.ValueChanged += vmValueChange;
                    s0Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s0Gain);
                    #endregion

                    #endregion

                    #region Strip 1
                    s1header = new TextBlock();
                    s1header.Text = "Strip 1";
                    s1header.Width = 65;
                    s1header.Height = 30;
                    s1header.Margin = new Thickness(0, 10, 220, 0);
                    s1header.Background = Brushes.Transparent;
                    s1header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1header.HorizontalAlignment = HorizontalAlignment.Right;
                    s1header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s1header);

                    #region A
                    s1A1 = new Button();
                    s1A1.Margin = new Thickness(0, 35, 235, 0);
                    s1A1.Width = 65;
                    s1A1.Height = 30;
                    s1A1.Content = "A1";
                    s1A1.Uid = "Strip[1].A1";
                    s1A1.Style = Resources["RoundedButton"] as Style;
                    s1A1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A1"))
                        s1A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1A1.BorderBrush = null;
                    s1A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s1A1.VerticalAlignment = VerticalAlignment.Top;
                    s1A1.Click += vmToggle;
                    s1A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1A1);

                    s1A2 = new Button();
                    s1A2.Margin = new Thickness(0, 65, 235, 0);
                    s1A2.Width = 65;
                    s1A2.Height = 30;
                    s1A2.Content = "A2";
                    s1A2.Uid = "Strip[1].A2";
                    s1A2.Style = Resources["RoundedButton"] as Style;
                    s1A2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A2"))
                        s1A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1A2.BorderBrush = null;
                    s1A2.HorizontalAlignment = HorizontalAlignment.Right;
                    s1A2.VerticalAlignment = VerticalAlignment.Top;
                    s1A2.Click += vmToggle;
                    s1A2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1A2);

                    s1A3 = new Button();
                    s1A3.Margin = new Thickness(0, 95, 235, 0);
                    s1A3.Width = 65;
                    s1A3.Height = 30;
                    s1A3.Content = "A3";
                    s1A3.Uid = "Strip[1].A3";
                    s1A3.Style = Resources["RoundedButton"] as Style;
                    s1A3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A3"))
                        s1A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1A3.BorderBrush = null;
                    s1A3.HorizontalAlignment = HorizontalAlignment.Right;
                    s1A3.VerticalAlignment = VerticalAlignment.Top;
                    s1A3.Click += vmToggle;
                    s1A3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1A3);

                    s1A4 = new Button();
                    s1A4.Margin = new Thickness(0, 125, 235, 0);
                    s1A4.Width = 65;
                    s1A4.Height = 30;
                    s1A4.Content = "A4";
                    s1A4.Uid = "Strip[1].A4";
                    s1A4.Style = Resources["RoundedButton"] as Style;
                    s1A4.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A4"))
                        s1A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1A4.BorderBrush = null;
                    s1A4.HorizontalAlignment = HorizontalAlignment.Right;
                    s1A4.VerticalAlignment = VerticalAlignment.Top;
                    s1A4.Click += vmToggle;
                    s1A4.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1A4);

                    s1A5 = new Button();
                    s1A5.Margin = new Thickness(0, 155, 235, 0);
                    s1A5.Width = 65;
                    s1A5.Height = 30;
                    s1A5.Content = "A5";
                    s1A5.Uid = "Strip[1].A5";
                    s1A5.Style = Resources["RoundedButton"] as Style;
                    s1A5.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A5"))
                        s1A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1A5.BorderBrush = null;
                    s1A5.HorizontalAlignment = HorizontalAlignment.Right;
                    s1A5.VerticalAlignment = VerticalAlignment.Top;
                    s1A5.Click += vmToggle;
                    s1A5.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1A5);
                    #endregion

                    #region B
                    s1B1 = new Button();
                    s1B1.Margin = new Thickness(0, 185, 235, 0);
                    s1B1.Width = 65;
                    s1B1.Height = 30;
                    s1B1.Content = "B1";
                    s1B1.Uid = "Strip[1].B1";
                    s1B1.Style = Resources["RoundedButton"] as Style;
                    s1B1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].B1"))
                        s1B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1B1.BorderBrush = null;
                    s1B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s1B1.VerticalAlignment = VerticalAlignment.Top;
                    s1B1.Click += vmToggle;
                    s1B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1B1);

                    s1B2 = new Button();
                    s1B2.Margin = new Thickness(0, 215, 235, 0);
                    s1B2.Width = 65;
                    s1B2.Height = 30;
                    s1B2.Content = "B2";
                    s1B2.Uid = "Strip[1].B2";
                    s1B2.Style = Resources["RoundedButton"] as Style;
                    s1B2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].B2"))
                        s1B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1B2.BorderBrush = null;
                    s1B2.HorizontalAlignment = HorizontalAlignment.Right;
                    s1B2.VerticalAlignment = VerticalAlignment.Top;
                    s1B2.Click += vmToggle;
                    s1B2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1B2);

                    s1B3 = new Button();
                    s1B3.Margin = new Thickness(0, 245, 235, 0);
                    s1B3.Width = 65;
                    s1B3.Height = 30;
                    s1B3.Content = "B3";
                    s1B3.Uid = "Strip[1].B3";
                    s1B3.Style = Resources["RoundedButton"] as Style;
                    s1B3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].B3"))
                        s1B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1B3.BorderBrush = null;
                    s1B3.HorizontalAlignment = HorizontalAlignment.Right;
                    s1B3.VerticalAlignment = VerticalAlignment.Top;
                    s1B3.Click += vmToggle;
                    s1B3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1B3);
                    #endregion

                    #region other
                    s1Mute = new Button();
                    s1Mute.Margin = new Thickness(0, 275, 235, 0);
                    s1Mute.Width = 65;
                    s1Mute.Height = 30;
                    s1Mute.Content = "Mute";
                    s1Mute.Uid = "Strip[1].Mute";
                    s1Mute.Style = Resources["RoundedButton"] as Style;
                    s1Mute.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].Mute"))
                        s1Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s1Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s1Mute.BorderBrush = null;
                    s1Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s1Mute.VerticalAlignment = VerticalAlignment.Top;
                    s1Mute.Click += vmToggle;
                    s1Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1Mute);

                    s1GainHeader = new TextBlock();
                    s1GainHeader.Text = "Gain";
                    s1GainHeader.Width = 65;
                    s1GainHeader.Height = 30;
                    s1GainHeader.Margin = new Thickness(0, 305, 215, 0);
                    s1GainHeader.Background = Brushes.Transparent;
                    s1GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s1GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s1GainHeader);

                    s1GainValue = new TextBlock();
                    s1GainValue.Text = Math.Round(remoteControle.getParameter("Strip[1].Gain"), 2).ToString();
                    s1GainValue.Uid = "Strip[1].Gain_Value";
                    s1GainValue.Width = 65;
                    s1GainValue.Height = 30;
                    s1GainValue.Margin = new Thickness(0, 325, 215, 0);
                    s1GainValue.Background = Brushes.Transparent;
                    s1GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s1GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s1GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s1GainValue);

                    s1Gain = new Slider();
                    s1Gain.Margin = new Thickness(0, 350, 235, 0);
                    s1Gain.Width = 65;
                    s1Gain.Height = 30;
                    s1Gain.Uid = "Strip[1].Gain";
                    s1Gain.Maximum = 12;
                    s1Gain.Minimum = -60;
                    s1Gain.Value = remoteControle.getParameter("Strip[1].Gain");
                    s1Gain.LargeChange = 0.5;
                    s1Gain.SmallChange = 0.01;
                    s1Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s1Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s1Gain.VerticalAlignment = VerticalAlignment.Top;
                    s1Gain.ValueChanged += vmValueChange;
                    s1Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s1Gain);
                    #endregion

                    #endregion

                    #region Strip 2
                    s2header = new TextBlock();
                    s2header.Text = "Strip 2";
                    s2header.Width = 65;
                    s2header.Height = 30;
                    s2header.Margin = new Thickness(0, 10, 140, 0);
                    s2header.Background = Brushes.Transparent;
                    s2header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s2header.HorizontalAlignment = HorizontalAlignment.Right;
                    s2header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s2header);

                    #region A
                    s2A1 = new Button();
                    s2A1.Margin = new Thickness(0, 35, 160, 0);
                    s2A1.Width = 65;
                    s2A1.Height = 30;
                    s2A1.Content = "A1";
                    s2A1.Uid = "Strip[2].A1";
                    s2A1.Style = Resources["RoundedButton"] as Style;
                    s2A1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A1"))
                        s2A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2A1.BorderBrush = null;
                    s2A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s2A1.VerticalAlignment = VerticalAlignment.Top;
                    s2A1.Click += vmToggle;
                    s2A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2A1);

                    s2A2 = new Button();
                    s2A2.Margin = new Thickness(0, 65, 160, 0);
                    s2A2.Width = 65;
                    s2A2.Height = 30;
                    s2A2.Content = "A2";
                    s2A2.Uid = "Strip[2].A2";
                    s2A2.Style = Resources["RoundedButton"] as Style;
                    s2A2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A2"))
                        s2A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2A2.BorderBrush = null;
                    s2A2.HorizontalAlignment = HorizontalAlignment.Right;
                    s2A2.VerticalAlignment = VerticalAlignment.Top;
                    s2A2.Click += vmToggle;
                    s2A2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2A2);

                    s2A3 = new Button();
                    s2A3.Margin = new Thickness(0, 95, 160, 0);
                    s2A3.Width = 65;
                    s2A3.Height = 30;
                    s2A3.Content = "A3";
                    s2A3.Uid = "Strip[2].A3";
                    s2A3.Style = Resources["RoundedButton"] as Style;
                    s2A3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A3"))
                        s2A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2A3.BorderBrush = null;
                    s2A3.HorizontalAlignment = HorizontalAlignment.Right;
                    s2A3.VerticalAlignment = VerticalAlignment.Top;
                    s2A3.Click += vmToggle;
                    s2A3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2A3);

                    s2A4 = new Button();
                    s2A4.Margin = new Thickness(0, 125, 160, 0);
                    s2A4.Width = 65;
                    s2A4.Height = 30;
                    s2A4.Content = "A4";
                    s2A4.Uid = "Strip[2].A4";
                    s2A4.Style = Resources["RoundedButton"] as Style;
                    s2A4.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A4"))
                        s2A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2A4.BorderBrush = null;
                    s2A4.HorizontalAlignment = HorizontalAlignment.Right;
                    s2A4.VerticalAlignment = VerticalAlignment.Top;
                    s2A4.Click += vmToggle;
                    s2A4.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2A4);

                    s2A5 = new Button();
                    s2A5.Margin = new Thickness(0, 155, 160, 0);
                    s2A5.Width = 65;
                    s2A5.Height = 30;
                    s2A5.Content = "A5";
                    s2A5.Uid = "Strip[2].A5";
                    s2A5.Style = Resources["RoundedButton"] as Style;
                    s2A5.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[1].A5"))
                        s2A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2A5.BorderBrush = null;
                    s2A5.HorizontalAlignment = HorizontalAlignment.Right;
                    s2A5.VerticalAlignment = VerticalAlignment.Top;
                    s2A5.Click += vmToggle;
                    s2A5.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2A5);
                    #endregion

                    #region B
                    s2B1 = new Button();
                    s2B1.Margin = new Thickness(0, 185, 160, 0);
                    s2B1.Width = 65;
                    s2B1.Height = 30;
                    s2B1.Content = "B1";
                    s2B1.Uid = "Strip[2].B1";
                    s2B1.Style = Resources["RoundedButton"] as Style;
                    s2B1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[2].B1"))
                        s2B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2B1.BorderBrush = null;
                    s2B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s2B1.VerticalAlignment = VerticalAlignment.Top;
                    s2B1.Click += vmToggle;
                    s2B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2B1);

                    s2B2 = new Button();
                    s2B2.Margin = new Thickness(0, 215, 160, 0);
                    s2B2.Width = 65;
                    s2B2.Height = 30;
                    s2B2.Content = "B2";
                    s2B2.Uid = "Strip[2].B2";
                    s2B2.Style = Resources["RoundedButton"] as Style;
                    s2B2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[2].B2"))
                        s2B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2B2.BorderBrush = null;
                    s2B2.HorizontalAlignment = HorizontalAlignment.Right;
                    s2B2.VerticalAlignment = VerticalAlignment.Top;
                    s2B2.Click += vmToggle;
                    s2B2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2B2);

                    s2B3 = new Button();
                    s2B3.Margin = new Thickness(0, 245, 160, 0);
                    s2B3.Width = 65;
                    s2B3.Height = 30;
                    s2B3.Content = "B3";
                    s2B3.Uid = "Strip[2].B3";
                    s2B3.Style = Resources["RoundedButton"] as Style;
                    s2B3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[2].B3"))
                        s2B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2B3.BorderBrush = null;
                    s2B3.HorizontalAlignment = HorizontalAlignment.Right;
                    s2B3.VerticalAlignment = VerticalAlignment.Top;
                    s2B3.Click += vmToggle;
                    s2B3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2B3);
                    #endregion

                    #region other
                    s2Mute = new Button();
                    s2Mute.Margin = new Thickness(0, 275, 160, 0);
                    s2Mute.Width = 65;
                    s2Mute.Height = 30;
                    s2Mute.Content = "Mute";
                    s2Mute.Uid = "Strip[2].Mute";
                    s2Mute.Style = Resources["RoundedButton"] as Style;
                    s2Mute.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[2].Mute"))
                        s2Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s2Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s2Mute.BorderBrush = null;
                    s2Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s2Mute.VerticalAlignment = VerticalAlignment.Top;
                    s2Mute.Click += vmToggle;
                    s2Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2Mute);

                    s2GainHeader = new TextBlock();
                    s2GainHeader.Text = "Gain";
                    s2GainHeader.Width = 65;
                    s2GainHeader.Height = 30;
                    s2GainHeader.Margin = new Thickness(0, 305, 140, 0);
                    s2GainHeader.Background = Brushes.Transparent;
                    s2GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s2GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s2GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s2GainHeader);

                    s2GainValue = new TextBlock();
                    s2GainValue.Text = Math.Round(remoteControle.getParameter("Strip[2].Gain"), 2).ToString();
                    s2GainValue.Uid = "Strip[2].Gain_Value";
                    s2GainValue.Width = 65;
                    s2GainValue.Height = 30;
                    s2GainValue.Margin = new Thickness(0, 325, 140, 0);
                    s2GainValue.Background = Brushes.Transparent;
                    s2GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s2GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s2GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s2GainValue);

                    s2Gain = new Slider();
                    s2Gain.Margin = new Thickness(0, 350, 160, 0);
                    s2Gain.Width = 65;
                    s2Gain.Height = 30;
                    s2Gain.Uid = "Strip[2].Gain";
                    s2Gain.Maximum = 12;
                    s2Gain.Minimum = -60;
                    s2Gain.Value = remoteControle.getParameter("Strip[2].Gain");
                    s2Gain.LargeChange = 0.5;
                    s2Gain.SmallChange = 0.01;
                    s2Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s2Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s2Gain.VerticalAlignment = VerticalAlignment.Top;
                    s2Gain.ValueChanged += vmValueChange;
                    s2Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s2Gain);
                    #endregion

                    #endregion

                    #region Strip 3
                    s3header = new TextBlock();
                    s3header.Text = "Strip 3";
                    s3header.Width = 65;
                    s3header.Height = 30;
                    s3header.Margin = new Thickness(0, 10, 65, 0);
                    s3header.Background = Brushes.Transparent;
                    s3header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s3header.HorizontalAlignment = HorizontalAlignment.Right;
                    s3header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s3header);

                    #region A
                    s3A1 = new Button();
                    s3A1.Margin = new Thickness(0, 35, 85, 0);
                    s3A1.Width = 65;
                    s3A1.Height = 30;
                    s3A1.Content = "A1";
                    s3A1.Uid = "Strip[3].A1";
                    s3A1.Style = Resources["RoundedButton"] as Style;
                    s3A1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[3].A1"))
                        s3A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s3A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s3A1.BorderBrush = null;
                    s3A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s3A1.VerticalAlignment = VerticalAlignment.Top;
                    s3A1.Click += vmToggle;
                    s3A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3A1);

                    s3A2 = new Button();
                    s3A2.Margin = new Thickness(0, 65, 85, 0);
                    s3A2.Width = 65;
                    s3A2.Height = 30;
                    s3A2.Content = "A2";
                    s3A2.Uid = "Strip[3].A2";
                    s3A2.Style = Resources["RoundedButton"] as Style;
                    s3A2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[3].A2"))
                        s3A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s3A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s3A2.BorderBrush = null;
                    s3A2.HorizontalAlignment = HorizontalAlignment.Right;
                    s3A2.VerticalAlignment = VerticalAlignment.Top;
                    s3A2.Click += vmToggle;
                    s3A2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3A2);

                    s3A3 = new Button();
                    s3A3.Margin = new Thickness(0, 95, 85, 0);
                    s3A3.Width = 65;
                    s3A3.Height = 30;
                    s3A3.Content = "A3";
                    s3A3.Uid = "Strip[3].A3";
                    s3A3.Style = Resources["RoundedButton"] as Style;
                    s3A3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[3].A3"))
                        s3A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s3A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s3A3.BorderBrush = null;
                    s3A3.HorizontalAlignment = HorizontalAlignment.Right;
                    s3A3.VerticalAlignment = VerticalAlignment.Top;
                    s3A3.Click += vmToggle;
                    s3A3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3A3);

                    s3A4 = new Button();
                    s3A4.Margin = new Thickness(0, 125, 85, 0);
                    s3A4.Width = 65;
                    s3A4.Height = 30;
                    s3A4.Content = "A4";
                    s3A4.Uid = "Strip[3].A4";
                    s3A4.Style = Resources["RoundedButton"] as Style;
                    s3A4.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[3].A4"))
                        s3A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s3A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s3A4.BorderBrush = null;
                    s3A4.HorizontalAlignment = HorizontalAlignment.Right;
                    s3A4.VerticalAlignment = VerticalAlignment.Top;
                    s3A4.Click += vmToggle;
                    s3A4.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3A4);

                    s3A5 = new Button();
                    s3A5.Margin = new Thickness(0, 155, 85, 0);
                    s3A5.Width = 65;
                    s3A5.Height = 30;
                    s3A5.Content = "A5";
                    s3A5.Uid = "Strip[3].A5";
                    s3A5.Style = Resources["RoundedButton"] as Style;
                    s3A5.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[3].A5"))
                        s3A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s3A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s3A5.BorderBrush = null;
                    s3A5.HorizontalAlignment = HorizontalAlignment.Right;
                    s3A5.VerticalAlignment = VerticalAlignment.Top;
                    s3A5.Click += vmToggle;
                    s3A5.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3A5);
                    #endregion

                    #region B
                    s3B1 = new Button();
                    s3B1.Margin = new Thickness(0, 185, 85, 0);
                    s3B1.Width = 65;
                    s3B1.Height = 30;
                    s3B1.Content = "B1";
                    s3B1.Uid = "Strip[3].B1";
                    s3B1.Style = Resources["RoundedButton"] as Style;
                    s3B1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[3].B1"))
                        s3B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s3B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s3B1.BorderBrush = null;
                    s3B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s3B1.VerticalAlignment = VerticalAlignment.Top;
                    s3B1.Click += vmToggle;
                    s3B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3B1);

                    s3B2 = new Button();
                    s3B2.Margin = new Thickness(0, 215, 85, 0);
                    s3B2.Width = 65;
                    s3B2.Height = 30;
                    s3B2.Content = "B2";
                    s3B2.Uid = "Strip[3].B2";
                    s3B2.Style = Resources["RoundedButton"] as Style;
                    s3B2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[3].B2"))
                        s3B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s3B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s3B2.BorderBrush = null;
                    s3B2.HorizontalAlignment = HorizontalAlignment.Right;
                    s3B2.VerticalAlignment = VerticalAlignment.Top;
                    s3B2.Click += vmToggle;
                    s3B2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3B2);

                    s3B3 = new Button();
                    s3B3.Margin = new Thickness(0, 245, 85, 0);
                    s3B3.Width = 65;
                    s3B3.Height = 30;
                    s3B3.Content = "B3";
                    s3B3.Uid = "Strip[3].B3";
                    s3B3.Style = Resources["RoundedButton"] as Style;
                    s3B3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[3].B3"))
                        s3B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s3B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s3B3.BorderBrush = null;
                    s3B3.HorizontalAlignment = HorizontalAlignment.Right;
                    s3B3.VerticalAlignment = VerticalAlignment.Top;
                    s3B3.Click += vmToggle;
                    s3B3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3B3);
                    #endregion

                    #region other
                    s3Mute = new Button();
                    s3Mute.Margin = new Thickness(0, 275, 85, 0);
                    s3Mute.Width = 65;
                    s3Mute.Height = 30;
                    s3Mute.Content = "Mute";
                    s3Mute.Uid = "Strip[3].Mute";
                    s3Mute.Style = Resources["RoundedButton"] as Style;
                    s3Mute.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[3].Mute"))
                        s3Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s3Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s3Mute.BorderBrush = null;
                    s3Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s3Mute.VerticalAlignment = VerticalAlignment.Top;
                    s3Mute.Click += vmToggle;
                    s3Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3Mute);

                    s3GainHeader = new TextBlock();
                    s3GainHeader.Text = "Gain";
                    s3GainHeader.Width = 65;
                    s3GainHeader.Height = 30;
                    s3GainHeader.Margin = new Thickness(0, 305, 65, 0);
                    s3GainHeader.Background = Brushes.Transparent;
                    s3GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s3GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s3GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s3GainHeader);

                    s3GainValue = new TextBlock();
                    s3GainValue.Text = Math.Round(remoteControle.getParameter("Strip[3].Gain"), 2).ToString();
                    s3GainValue.Uid = "Strip[3].Gain_Value";
                    s3GainValue.Width = 65;
                    s3GainValue.Height = 30;
                    s3GainValue.Margin = new Thickness(0, 325, 65, 0);
                    s3GainValue.Background = Brushes.Transparent;
                    s3GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s3GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s3GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s3GainValue);

                    s3Gain = new Slider();
                    s3Gain.Margin = new Thickness(0, 350, 85, 0);
                    s3Gain.Width = 65;
                    s3Gain.Height = 30;
                    s3Gain.Uid = "Strip[3].Gain";
                    s3Gain.Maximum = 12;
                    s3Gain.Minimum = -60;
                    s3Gain.Value = remoteControle.getParameter("Strip[3].Gain");
                    s3Gain.LargeChange = 0.5;
                    s3Gain.SmallChange = 0.01;
                    s3Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s3Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s3Gain.VerticalAlignment = VerticalAlignment.Top;
                    s3Gain.ValueChanged += vmValueChange;
                    s3Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s3Gain);
                    #endregion

                    #endregion

                    #region Strip 4
                    s4header = new TextBlock();
                    s4header.Text = "Strip 4";
                    s4header.Width = 65;
                    s4header.Height = 30;
                    s4header.Margin = new Thickness(0, 10, -10, 0);
                    s4header.Background = Brushes.Transparent;
                    s4header.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s4header.HorizontalAlignment = HorizontalAlignment.Right;
                    s4header.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s4header);

                    #region A
                    s4A1 = new Button();
                    s4A1.Margin = new Thickness(0, 35, 10, 0);
                    s4A1.Width = 65;
                    s4A1.Height = 30;
                    s4A1.Content = "A1";
                    s4A1.Uid = "Strip[4].A1";
                    s4A1.Style = Resources["RoundedButton"] as Style;
                    s4A1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[4].A1"))
                        s4A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s4A1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s4A1.BorderBrush = null;
                    s4A1.HorizontalAlignment = HorizontalAlignment.Right;
                    s4A1.VerticalAlignment = VerticalAlignment.Top;
                    s4A1.Click += vmToggle;
                    s4A1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4A1);

                    s4A2 = new Button();
                    s4A2.Margin = new Thickness(0, 65, 10, 0);
                    s4A2.Width = 65;
                    s4A2.Height = 30;
                    s4A2.Content = "A2";
                    s4A2.Uid = "Strip[4].A2";
                    s4A2.Style = Resources["RoundedButton"] as Style;
                    s4A2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[4].A2"))
                        s4A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s4A2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s4A2.BorderBrush = null;
                    s4A2.HorizontalAlignment = HorizontalAlignment.Right;
                    s4A2.VerticalAlignment = VerticalAlignment.Top;
                    s4A2.Click += vmToggle;
                    s4A2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4A2);

                    s4A3 = new Button();
                    s4A3.Margin = new Thickness(0, 95, 10, 0);
                    s4A3.Width = 65;
                    s4A3.Height = 30;
                    s4A3.Content = "A3";
                    s4A3.Uid = "Strip[4].A3";
                    s4A3.Style = Resources["RoundedButton"] as Style;
                    s4A3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[4].A3"))
                        s4A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s4A3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s4A3.BorderBrush = null;
                    s4A3.HorizontalAlignment = HorizontalAlignment.Right;
                    s4A3.VerticalAlignment = VerticalAlignment.Top;
                    s4A3.Click += vmToggle;
                    s4A3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4A3);

                    s4A4 = new Button();
                    s4A4.Margin = new Thickness(0, 125, 10, 0);
                    s4A4.Width = 65;
                    s4A4.Height = 30;
                    s4A4.Content = "A4";
                    s4A4.Uid = "Strip[4].A4";
                    s4A4.Style = Resources["RoundedButton"] as Style;
                    s4A4.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[4].A4"))
                        s4A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s4A4.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s4A4.BorderBrush = null;
                    s4A4.HorizontalAlignment = HorizontalAlignment.Right;
                    s4A4.VerticalAlignment = VerticalAlignment.Top;
                    s4A4.Click += vmToggle;
                    s4A4.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4A4);

                    s4A5 = new Button();
                    s4A5.Margin = new Thickness(0, 155, 10, 0);
                    s4A5.Width = 65;
                    s4A5.Height = 30;
                    s4A5.Content = "A5";
                    s4A5.Uid = "Strip[4].A5";
                    s4A5.Style = Resources["RoundedButton"] as Style;
                    s4A5.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[4].A5"))
                        s4A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s4A5.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s4A5.BorderBrush = null;
                    s4A5.HorizontalAlignment = HorizontalAlignment.Right;
                    s4A5.VerticalAlignment = VerticalAlignment.Top;
                    s4A5.Click += vmToggle;
                    s4A5.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4A5);
                    #endregion

                    #region B
                    s4B1 = new Button();
                    s4B1.Margin = new Thickness(0, 185, 10, 0);
                    s4B1.Width = 65;
                    s4B1.Height = 30;
                    s4B1.Content = "B1";
                    s4B1.Uid = "Strip[4].B1";
                    s4B1.Style = Resources["RoundedButton"] as Style;
                    s4B1.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[4].B1"))
                        s4B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s4B1.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s4B1.BorderBrush = null;
                    s4B1.HorizontalAlignment = HorizontalAlignment.Right;
                    s4B1.VerticalAlignment = VerticalAlignment.Top;
                    s4B1.Click += vmToggle;
                    s4B1.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4B1);

                    s4B2 = new Button();
                    s4B2.Margin = new Thickness(0, 215, 10, 0);
                    s4B2.Width = 65;
                    s4B2.Height = 30;
                    s4B2.Content = "B2";
                    s4B2.Uid = "Strip[4].B2";
                    s4B2.Style = Resources["RoundedButton"] as Style;
                    s4B2.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[4].B2"))
                        s4B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s4B2.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s4B2.BorderBrush = null;
                    s4B2.HorizontalAlignment = HorizontalAlignment.Right;
                    s4B2.VerticalAlignment = VerticalAlignment.Top;
                    s4B2.Click += vmToggle;
                    s4B2.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4B2);

                    s4B3 = new Button();
                    s4B3.Margin = new Thickness(0, 245, 10, 0);
                    s4B3.Width = 65;
                    s4B3.Height = 30;
                    s4B3.Content = "B3";
                    s4B3.Uid = "Strip[4].B3";
                    s4B3.Style = Resources["RoundedButton"] as Style;
                    s4B3.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[4].B3"))
                        s4B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s4B3.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s4B3.BorderBrush = null;
                    s4B3.HorizontalAlignment = HorizontalAlignment.Right;
                    s4B3.VerticalAlignment = VerticalAlignment.Top;
                    s4B3.Click += vmToggle;
                    s4B3.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4B3);
                    #endregion

                    #region other
                    s4Mute = new Button();
                    s4Mute.Margin = new Thickness(0, 275, 10, 0);
                    s4Mute.Width = 65;
                    s4Mute.Height = 30;
                    s4Mute.Content = "Mute";
                    s4Mute.Uid = "Strip[4].Mute";
                    s4Mute.Style = Resources["RoundedButton"] as Style;
                    s4Mute.Background = Brushes.Transparent;
                    if (remoteControle.getBoolParameter("Strip[4].Mute"))
                        s4Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    else
                        s4Mute.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    s4Mute.BorderBrush = null;
                    s4Mute.HorizontalAlignment = HorizontalAlignment.Right;
                    s4Mute.VerticalAlignment = VerticalAlignment.Top;
                    s4Mute.Click += vmToggle;
                    s4Mute.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4Mute);

                    s4GainHeader = new TextBlock();
                    s4GainHeader.Text = "Gain";
                    s4GainHeader.Width = 65;
                    s4GainHeader.Height = 30;
                    s4GainHeader.Margin = new Thickness(0, 305, -10, 0);
                    s4GainHeader.Background = Brushes.Transparent;
                    s4GainHeader.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s4GainHeader.HorizontalAlignment = HorizontalAlignment.Right;
                    s4GainHeader.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s4GainHeader);

                    s4GainValue = new TextBlock();
                    s4GainValue.Text = Math.Round(remoteControle.getParameter("Strip[4].Gain"), 2).ToString();
                    s4GainValue.Uid = "Strip[4].Gain_Value";
                    s4GainValue.Width = 65;
                    s4GainValue.Height = 30;
                    s4GainValue.Margin = new Thickness(0, 325, -10, 0);
                    s4GainValue.Background = Brushes.Transparent;
                    s4GainValue.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    s4GainValue.HorizontalAlignment = HorizontalAlignment.Right;
                    s4GainValue.VerticalAlignment = VerticalAlignment.Top;
                    VM_Controller.Children.Add(s4GainValue);

                    s4Gain = new Slider();
                    s4Gain.Margin = new Thickness(0, 350, 10, 0);
                    s4Gain.Width = 65;
                    s4Gain.Height = 30;
                    s4Gain.Uid = "Strip[4].Gain";
                    s4Gain.Maximum = 12;
                    s4Gain.Minimum = -60;
                    s4Gain.Value = remoteControle.getParameter("Strip[4].Gain");
                    s4Gain.LargeChange = 0.5;
                    s4Gain.SmallChange = 0.01;
                    s4Gain.Style = Resources["Horizontal_Slider"] as Style;
                    s4Gain.HorizontalAlignment = HorizontalAlignment.Right;
                    s4Gain.VerticalAlignment = VerticalAlignment.Top;
                    s4Gain.ValueChanged += vmValueChange;
                    s4Gain.MouseRightButtonDown += setVRCParameterMV;
                    VM_Controller.Children.Add(s4Gain);
                    #endregion

                    #endregion
                    break;
            }
        }

        private void setVRCParameterMV(object sender, MouseButtonEventArgs e)
        {
            UIElement element = sender as UIElement;
            string jsonPath = element.Uid.Replace("[", "").Replace("]", "");
            var token = vrcJson.SelectToken(jsonPath);

            Thickness elementMargin = (element as FrameworkElement).Margin;

            TextBox ParameterBox = new TextBox();
            ParameterBox.Text = token.ToString();
            ParameterBox.Uid = jsonPath;
            ParameterBox.Width = 100;
            ParameterBox.Height = 30;
            ParameterBox.HorizontalAlignment = HorizontalAlignment.Right;
            ParameterBox.VerticalAlignment = VerticalAlignment.Top;
            ParameterBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            ParameterBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 107, 109, 113));
            ParameterBox.Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
            ParameterBox.KeyDown += ParameterInputUpdateMV;
            ParameterBox.ContextMenu = null;

            // Get Other_Controller bounds
            double controllerWidth = Other_Controller.ActualWidth;
            double controllerHeight = Other_Controller.ActualHeight;

            // Box position from Margin
            double boxLeft = elementMargin.Left;
            double boxTop = elementMargin.Top;

            // Calculate right & bottom edges of ParameterBox
            double boxRight = elementMargin.Right;
            double boxBottom = elementMargin.Bottom;


            // Check right overflow
            if (boxLeft + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxLeft = controllerWidth - ParameterBox.Width;
            }

            // Check left overflow
            if (boxRight + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxRight = controllerWidth - ParameterBox.Width;
            }

            if (boxBottom + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxBottom = controllerWidth - ParameterBox.Width;
            }

            // Check bottom overflow
            if (boxTop + ParameterBox.Height > controllerHeight)
            {
                // Move box upward (flip vertically)
                boxTop = controllerHeight - ParameterBox.Height;
            }

            // Ensure it doesn't go negative (top-left overrun)
            boxLeft = Math.Max(0, boxLeft);
            boxTop = Math.Max(0, boxTop);
            boxRight = Math.Max(0, boxRight);
            boxBottom = Math.Max(0, boxBottom);

            // Apply corrected Margin
            ParameterBox.Margin = new Thickness(boxLeft, boxTop, boxRight, boxBottom);

            VM_Controller.Children.Add(ParameterBox);
            ParameterBox.Focus();

        }

        private void setVRCParameterSpotify(object sender, MouseButtonEventArgs e)
        {
            UIElement element = sender as UIElement;
            string jsonPath = element.Uid.Replace("[", "").Replace("]", "");
            var token = vrcJson.SelectToken(jsonPath);

            Thickness elementMargin = (element as FrameworkElement).Margin;

            TextBox ParameterBox = new TextBox();
            ParameterBox.Text = token.ToString();
            ParameterBox.Uid = jsonPath;
            ParameterBox.Width = 100;
            ParameterBox.Height = 30;
            ParameterBox.HorizontalAlignment = HorizontalAlignment.Left;
            ParameterBox.VerticalAlignment = VerticalAlignment.Top;
            ParameterBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            ParameterBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 107, 109, 113));
            ParameterBox.Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
            ParameterBox.KeyDown += ParameterInputUpdateSpotify;
            ParameterBox.ContextMenu = null;

            // Get Other_Controller bounds
            double controllerWidth = Other_Controller.ActualWidth;
            double controllerHeight = Other_Controller.ActualHeight;

            // Box position from Margin
            double boxLeft = elementMargin.Left;
            double boxTop = elementMargin.Top;

            // Calculate right & bottom edges of ParameterBox
            double boxRight = elementMargin.Right;
            double boxBottom = elementMargin.Bottom;


            // Check right overflow
            if (boxLeft + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxLeft = controllerWidth - ParameterBox.Width;
            }

            // Check left overflow
            if (boxRight + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxRight = controllerWidth - ParameterBox.Width;
            }

            if (boxBottom + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxBottom = controllerWidth - ParameterBox.Width;
            }

            // Check bottom overflow
            if (boxTop + ParameterBox.Height > controllerHeight)
            {
                // Move box upward (flip vertically)
                boxTop = controllerHeight - ParameterBox.Height;
            }

            // Ensure it doesn't go negative (top-left overrun)
            boxLeft = Math.Max(0, boxLeft);
            boxTop = Math.Max(0, boxTop);
            boxRight = Math.Max(0, boxRight);
            boxBottom = Math.Max(0, boxBottom);

            // Apply corrected Margin
            ParameterBox.Margin = new Thickness(boxLeft, boxTop, boxRight, boxBottom);

            Spotify_Controller.Children.Add(ParameterBox);
            ParameterBox.Focus();

        }
        private void setVRCParameterOther(object sender, MouseButtonEventArgs e)
        {
            UIElement element = sender as UIElement;
            string jsonPath = element.Uid.Replace("[", "").Replace("]", "");
            var token = vrcJson.SelectToken(jsonPath);

            Thickness elementMargin = (element as FrameworkElement).Margin;

            TextBox ParameterBox = new TextBox();
            ParameterBox.Text = token.ToString();
            ParameterBox.Uid = jsonPath;
            ParameterBox.Width = 100;
            ParameterBox.Height = 30;
            ParameterBox.HorizontalAlignment = HorizontalAlignment.Left;
            ParameterBox.VerticalAlignment = VerticalAlignment.Top;
            ParameterBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            ParameterBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 107, 109, 113));
            ParameterBox.Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
            ParameterBox.KeyDown += ParameterInputUpdateOther;
            ParameterBox.ContextMenu = null;

            // Get Other_Controller bounds
            double controllerWidth = Other_Controller.ActualWidth;
            double controllerHeight = Other_Controller.ActualHeight;

            // Box position from Margin
            double boxLeft = elementMargin.Left;
            double boxTop = elementMargin.Top;

            // Calculate right & bottom edges of ParameterBox
            double boxRight = elementMargin.Right;
            double boxBottom = elementMargin.Bottom;


            // Check right overflow
            if (boxLeft + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxLeft = controllerWidth - ParameterBox.Width;
            }

            // Check left overflow
            if (boxRight + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxRight = controllerWidth - ParameterBox.Width;
            }

            if (boxBottom + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxBottom = controllerWidth - ParameterBox.Width;
            }

            // Check bottom overflow
            if (boxTop + ParameterBox.Height > controllerHeight)
            {
                // Move box upward (flip vertically)
                boxTop = controllerHeight - ParameterBox.Height;
            }

            // Ensure it doesn't go negative (top-left overrun)
            boxLeft = Math.Max(0, boxLeft);
            boxTop = Math.Max(0, boxTop);
            boxRight = Math.Max(0, boxRight);
            boxBottom = Math.Max(0, boxBottom);

            // Apply corrected Margin
            ParameterBox.Margin = new Thickness(boxLeft, boxTop, boxRight, boxBottom);

            Other_Controller.Children.Add(ParameterBox);
            ParameterBox.Focus();

        }

        private void ParameterInputUpdateMV(object sender, KeyEventArgs e)
        {
            TextBox ParameterBox = sender as TextBox;
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                string[] path = ParameterBox.Uid.Split('.');
                vrcJson[path[0]][path[1]] = ParameterBox.Text;
                vrcConfig = vrcJson.ToObject<VRCParameterConfig>();
                VM_Controller.Children.Remove(ParameterBox);
            }
        }

        private void ParameterInputUpdateSpotify(object sender, KeyEventArgs e)
        {
            TextBox ParameterBox = sender as TextBox;
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                string[] path = ParameterBox.Uid.Split('.');
                vrcJson[path[0]][path[1]] = ParameterBox.Text;
                vrcConfig = vrcJson.ToObject<VRCParameterConfig>();
                Spotify_Controller.Children.Remove(ParameterBox);
            }
        }

        private void ParameterInputUpdateOther(object sender, KeyEventArgs e)
        {
            TextBox ParameterBox = sender as TextBox;
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                string[] path = ParameterBox.Uid.Split('.');
                vrcJson[path[0]][path[1]] = ParameterBox.Text;
                vrcConfig = vrcJson.ToObject<VRCParameterConfig>();
                Other_Controller.Children.Remove(ParameterBox);
            }
        }

        public static UIElement GetByUid(DependencyObject rootElement, string uid)
        {
            foreach (UIElement element in LogicalTreeHelper.GetChildren(rootElement).OfType<UIElement>())
            {
                if (element.Uid == uid)
                    return element;
                UIElement resultChildren = GetByUid(element, uid);
                if (resultChildren != null)
                    return resultChildren;
            }
            return null;
        }

        public void vmValueChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider gain = sender as Slider;
            remoteControle.changeParameter(gain.Uid as string,(float)gain.Value);
            TextBlock value = (TextBlock)GetByUid(VM_Controller, gain.Uid + "_Value");
            value.Text = Math.Round(gain.Value,2).ToString();
        }

        private void vmToggle(object sender, RoutedEventArgs e)
        {
            Button toggle = sender as Button;
            remoteControle.toggleParameter(toggle.Uid as string);

            if (remoteControle.getBoolParameter(toggle.Uid))
                toggle.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            else
                toggle.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stopAll();
            saveAll();
        }

        public static void stopAll()
        {
            if (config.SpotifyConfig.Enabled && spotify != null)
            {
                updateName.stop();

                updateProgess.stop();

                updateSpotify.stop();
            }

            osc.stop();

            
            updateToken.Cancel();
            updateVMToken.Cancel();
            remoteControle.LogOut();
            if(wisper.isRunning)
                wisper.stop();
            
        }

        public static void saveAll()
        {
            var vrcJsonString = JsonConvert.SerializeObject(vrcConfig, Formatting.Indented);
            File.WriteAllText(cfg_path + "vrc_config.json", vrcJsonString);

            var configJsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(cfg_path + "config.json", configJsonString);
        }

        private void Time_Loaded(object sender, RoutedEventArgs e)
        {
            ct = updateToken.Token;
            TextBlock text = sender as TextBlock;
            Task.Run(() => { 
                while (!ct.IsCancellationRequested)
                {
                    var uiAccess = Time.Dispatcher.CheckAccess();

                    if (uiAccess)
                    {
                        if (ct.IsCancellationRequested)
                            break;
                        Time.Text = DateTime.Now.ToString("HH:mm:ss tt");
                    }
                    else
                    {
                        if (ct.IsCancellationRequested)
                            break;
                        Time.Dispatcher.Invoke(() => { Time.Text = DateTime.Now.ToString("HH:mm:ss tt"); });
                    }
                    Thread.Sleep(250);
                }
            }, updateToken.Token);
        }

        private void STTModels_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (!Directory.Exists(@"models\"))
                Directory.CreateDirectory(@"models\");

            bool isEmpty = !Directory.EnumerateFiles(@"models\").Any();

            if (!isEmpty)
            {
                var files = Directory
                .GetFiles(@"models\", "*", SearchOption.AllDirectories)
                .Select(f => System.IO.Path.GetFileName(f));

                foreach (string ggmlType in Enum.GetNames(typeof(GgmlType)))
                {
                    ComboBoxItem item = new ComboBoxItem();
                    if (files.Contains(ggmlType+ ".bin"))
                        item.Content = ggmlType;
                    else
                        item.Content = ggmlType + " (downloadable)";
                    item.Tag = ggmlType;
                    comboBox.Items.Add(item);
                }
            }
            else
            {
                foreach (string ggmlType in Enum.GetNames(typeof(GgmlType)))
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = ggmlType + " (downloadable)";
                    item.Tag = ggmlType;
                    comboBox.Items.Add(item);
                }
            }
            comboBox.SelectedIndex = config.STT.Model;

            ComboBoxItem comboBoxItem = comboBox.Items[comboBox.SelectedIndex] as ComboBoxItem;
            modelPath = @"models\" + comboBoxItem.Content;
            if (modelPath.Contains(" (downloadable)"))
                modelPath = modelPath.Replace(" (downloadable)", "");
            modelPath = modelPath + ".bin";
        }

        private void STTModels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox.IsLoaded)
            {
                bool wasRunning = false;
                if (wisper.isRunning)
                {
                    wasRunning = true;
                    wisper.stop();
                }

                ComboBoxItem comboBoxItem = comboBox.Items[comboBox.SelectedIndex] as ComboBoxItem;
                config.STT.Model = comboBox.SelectedIndex;
                if (comboBoxItem.IsLoaded)
                {
                    modelPath = @"models\" + comboBoxItem.Content;
                    if(modelPath.Contains(" (downloadable)"))
                        modelPath = modelPath.Replace(" (downloadable)", "");
                    modelPath = modelPath + ".bin";
                    if (wasRunning)
                        wisper.start(modelPath, MicrophoneCapture.LANGUAGES.Keys.ElementAt(config.STT.Language),config.STT.Translate);
                }
            }
            
        }

        private void STTEnablex_Click(object sender, RoutedEventArgs e)
        {
            if (wisper.isRunning)
            {
                wisper.stop();
                STTRunning.Text = "STT is not active";
                STTRunning.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            }
            else
            {
                wisper.start(modelPath, MicrophoneCapture.LANGUAGES.Keys.ElementAt(config.STT.Language), config.STT.Translate);
                STTRunning.Text = "STT starting...";
                STTRunning.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
            }
        }

        private void STTLanguage_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            foreach (KeyValuePair<string, string> language in MicrophoneCapture.LANGUAGES)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = language.Value;
                item.Tag = language.Key;
                comboBox.Items.Add(item);
            }
            comboBox.SelectedIndex = config.STT.Language;
        }

        private void STTLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox.IsLoaded)
            {
                ComboBoxItem comboBoxItem = comboBox.Items[comboBox.SelectedIndex] as ComboBoxItem;
                config.STT.Language = comboBox.SelectedIndex;
                bool wasRunning = false;
                if (wisper.isRunning)
                {
                    wasRunning = true;
                    wisper.stop();
                }

                if (wasRunning)
                {
                    wisper.start(modelPath, MicrophoneCapture.LANGUAGES.Keys.ElementAt(config.STT.Language), config.STT.Translate);
                }
                    
            }
        }

        private void runSpotify_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if(SotifyClientID.Password != "your-client-id" && SotifyClientSecret.Password != "your-client-secret")
            {
                button.IsEnabled = false;
                button.Foreground = new SolidColorBrush(Colors.Red);
                config.SpotifyConfig.ClientID = SotifyClientID.Password;
                config.SpotifyConfig.ClientSecret = SotifyClientSecret.Password;

                SpotifyAuth auth = new SpotifyAuth();
                auth.runAuth();
                update();

            }
        }

        private void SotifyClientID_Loaded(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            passwordBox.Password = config.SpotifyConfig.ClientID;
        }

        private void SotifyClientSecret_Loaded(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            passwordBox.Password = config.SpotifyConfig.ClientSecret;
        }

        public void hideSpotifyInput()
        {
            runSpotify.Visibility = Visibility.Hidden;
            SotifyClientID.Visibility = Visibility.Hidden;
            SotifyClientIDText.Visibility = Visibility.Hidden;
            SotifyClientSecret.Visibility = Visibility.Hidden;
            SotifyClientSecretText.Visibility = Visibility.Hidden;
        }

        private void STTTranslate_Loaded(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            checkBox.IsChecked = config.STT.Translate;
        }

        private void STTTranslate_Unchecked(object sender, RoutedEventArgs e)
        {
            config.STT.Translate = false;
        }

        private void STTTranslate_Checked(object sender, RoutedEventArgs e)
        {
            config.STT.Translate = true;
        }
    }
}
