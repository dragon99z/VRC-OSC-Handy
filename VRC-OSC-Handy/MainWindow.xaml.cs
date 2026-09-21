using Newtonsoft.Json.Linq;
using SpotifyAPI.Web;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using VRC_OSC_Handy.Auth;
using VRC_OSC_Handy.Config;
using VRC_OSC_Handy.Func;
using VRC_OSC_Handy.Logger;
using VRC_OSC_Handy.Osc;
using VRC_OSC_Handy.Particles;
using VRC_OSC_Handy.Update;
using VRC_OSC_Handy.VoiceMeeter;
using VRC_OSC_Handy.Wis;

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

        private const string SpotifyClientIDPlaceholder = "your-client-id";
        private const string SpotifyClientSecretPlaceholder = "your-client-secret";

        // The per-strip UI used to be ~65 hand-declared fields (s0header, s0A1..s0B3,
        // s0Mute, s0GainHeader/Value/Gain, repeated for s1..s4). Nothing outside
        // LoadVMSettings ever read them by name - every button/slider is looked up at
        // runtime by its Uid (see vmToggle/vmValueChange/GetByUid) - so they were pure
        // write-once scratch variables and are now just locals inside BuildVmStrip.

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

            try
            {
                remoteControle = new RemoteControle();
            }
            catch (Exception ex)
            {
                // VoiceMeeter is documented as optional (see README). The native wrapper
                // throws when it isn't installed (registry/DLL lookup in
                // VoiceMeeterPathHelper) or when the Remote API login fails, so treat
                // that as "not available" instead of letting it take the whole app down.
                // Every call site that reads remoteControle is null-guarded to degrade
                // to "Voicemeeter not found!" UI / a no-op instead.
                remoteControle = null;
                DebugLogger.LogWarning($"VoiceMeeter unavailable, continuing without it: {ex.Message}");
            }
            wisper = new Wisper();
            InitializeComponent();
            this.Icon = ByteImageConverter.ByteToImage(Properties.Resources.image);
        }

        // genConfig, stopAll, and saveAll now live in MainWindow.ConfigIO.cs.

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ps = new ParticleSystem(5, 25, 5, 100, 75, this.cvs_particleContainer, this.grid_lineContainer);
            //Register frame animation
            CompositionTarget.Rendering += CompositionTarget_Rendering;


            if (config.SpotifyConfig.Enabled && config.SpotifyConfig.ClientID != SpotifyClientIDPlaceholder && config.SpotifyConfig.ClientSecret != SpotifyClientSecretPlaceholder)
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

        // PlayNext, PlayPauseToggle, PlayLast, and the Spotify client ID/secret
        // settings UI now live in MainWindow.SpotifySettings.cs.

        // VM_Controller_Loaded, GetByUid, vmValueChange, and vmToggle now live in
        // MainWindow.VoiceMeeterPanel.cs, next to the strip-panel builder they operate on.

        // The VoiceMeeter strip-panel builder (LoadVMSettings, BuildVmStrip, and
        // friends) now lives in MainWindow.VoiceMeeterPanel.cs.

        // ShowParameterEditor, setVRCParameterMV/Spotify/Other, HideParameterEditor, and
        // ParameterInputUpdateMV/Spotify/Other now live in MainWindow.ParameterEditor.cs.

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stopAll();
            saveAll();
        }

        // Time_Loaded, STTModels_*, STTEnablex_Click, STTLanguage_*, and
        // STTTranslate_* now live in MainWindow.SttSettings.cs.
    }
}
