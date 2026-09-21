using BuildSoft.VRChat.Osc;
using BuildSoft.VRChat.Osc.Avatar;
using BuildSoft.VRChat.Osc.Chatbox;
using SpotifyAPI.Web;
using Swan;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using VRC_OSC_Handy.Config;
using VRC_OSC_Handy.Logger;
using VRC_OSC_Handy.NAudio;
using VRC_OSC_Handy.Update;
using VRC_OSC_Handy.VoiceMeeter;
using VRC_OSC_Handy.Wis;

namespace VRC_OSC_Handy.Osc
{
    internal class VRCOSC
    {
        OscAvatarConfig avatarConfig = null;

        CancellationTokenSource ChatToken = new CancellationTokenSource();
        CancellationToken chat_ct;

        int sleep = 5000;

        bool song;
        bool progress;
        bool time;
        bool stt;

        public static string msgSst;

        public async void Run(RemoteControle remoteControle, SpotifyClient spotify, Wisper wisper, string modelPath)
        {
            chat_ct = ChatToken.Token;
            Task.Run(() => UpdateChat(), ChatToken.Token);

            avatarConfig = OscAvatarConfig.CreateAtCurrent();

            if (avatarConfig == null)
                avatarConfig = await OscAvatarConfig.WaitAndCreateAtCurrentAsync();

            // Every "Handy/..." avatar parameter change lands here. The parameter's OSC
            // path after "Handy/" tells us which of three unrelated things changed - a
            // VoiceMeeter strip control, a Spotify control, or a misc "Other" control -
            // so this just logs the change, refreshes a few fields from MainWindow, and
            // dispatches to the one handler that actually knows what to do with it.
            OscAvatarParameterChangedEventHandler handler = async (parameter, e) =>
            {
                if (!parameter.Name.Contains("Handy"))
                    return;

                DateTime now = DateTime.Now;
                DebugLogger.Log($"[{now.ToShortDateString()} {now.ToShortTimeString()}] " +
                    $"{parameter.Name}: {e.OldValue} => {e.NewValue}");
                if (Application.Current != null)
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        spotify = MainWindow.spotify;
                        wisper = MainWindow.wisper;
                        modelPath = MainWindow.modelPath;
                    });

                string[] param = parameter.Name.Remove(0, 6).Split('/');

                if (parameter.Name.Contains("Strip") && remoteControle != null && remoteControle.type > 0)
                    HandleStripParameter(remoteControle, param, e.NewValue);
                else if (param[0].Equals("Spotify") && spotify != null)
                    HandleSpotifyParameter(spotify, param[1], e.NewValue);
                else if (param[0].Equals("Other"))
                    HandleOtherParameter(wisper, modelPath, param[1], e.NewValue);
            };

            OscAvatarUtility.AvatarChanged += (sender, e) =>
            {
                avatarConfig.Parameters.ParameterChanged -= handler;

                avatarConfig = OscAvatarConfig.CreateAtCurrent();
                SyncParameter(remoteControle, avatarConfig);
                DebugLogger.Log($"Changed avatar. Name: {avatarConfig.Name}");
                if (Application.Current != null)
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                        mainWindow.CurrentAvatar.Text = avatarConfig.Name;
                        mainWindow.CurrentAvatar.Foreground = new SolidColorBrush(Colors.Lime);
                    });


                avatarConfig.Parameters.ParameterChanged += handler;
            };
        }

        private void HandleStripParameter(RemoteControle remoteControle, string[] param, object newValue)
        {
            string stripPath = AddSquareBrackets(param[0]);
            string paramName = param[1];
            string fullParam = $"{stripPath}.{paramName}";

            if (paramName.Equals("Gain"))
                remoteControle.changeParameter(fullParam, TranslateValue((float)newValue));
            else
                remoteControle.toggleBoolParameter(fullParam, newValue.ToBoolean());
        }

        private void HandleSpotifyParameter(SpotifyClient spotify, string paramName, object newValue)
        {
            try
            {
                var track = updateSpotify.track;
                if (track == null)
                    return;

                switch (paramName)
                {
                    case "Next":
                        if ((bool)newValue && track.IsPlaying)
                            spotify.Player.SkipNext().GetAwaiter().GetResult();
                        break;
                    case "Last":
                        if ((bool)newValue && track.IsPlaying)
                            spotify.Player.SkipPrevious().GetAwaiter().GetResult();
                        break;
                    case "PlayPause":
                        if (!track.IsPlaying && (bool)newValue)
                            spotify.Player.ResumePlayback().GetAwaiter().GetResult();
                        else if (track.IsPlaying && !(bool)newValue)
                            spotify.Player.PausePlayback().GetAwaiter().GetResult();
                        break;
                    case "Song":
                        song = (bool)newValue;
                        break;
                    case "ProgressBar":
                        progress = (bool)newValue;
                        break;
                }
            }
            catch (APITooManyRequestsException)
            {
                Thread.Sleep(sleep);
                sleep += 5000;
            }
        }

        private void HandleOtherParameter(Wisper wisper, string modelPath, string paramName, object newValue)
        {
            switch (paramName)
            {
                case "Time":
                    time = (bool)newValue;
                    break;

                case "STT":
                    bool enableStt = (bool)newValue;
                    if (wisper.isRunning == enableStt)
                        break;

                    if (enableStt)
                    {
                        wisper.start(modelPath, MicrophoneCapture.LANGUAGES.Keys.ElementAt(MainWindow.config.STT.Language), MainWindow.config.STT.Translate);
                        stt = true;
                    }
                    else
                    {
                        wisper.stop();
                        stt = false;
                    }
                    break;
            }
        }

        public void stop()
        {
            ChatToken.Cancel();
        }

        bool running = false;
        string lastStt;
        string totalStt = "";
        int loopStt = 0;

        public void UpdateChat()
        {
            while (!chat_ct.IsCancellationRequested)
            {
                var track = updateSpotify.track;
                if (track?.Item != null) // Item is null during ads/private sessions
                {
                    string msg = "";

                    // Track and Episode both just need a name + duration - the only
                    // difference is which Spotify type they're cast from - so both
                    // song/progress sections below read from this instead of switching
                    // on track.Item.Type themselves.
                    (string name, int durationMs) = GetPlayingItemInfo(track);
                    bool isKnownItemType = name != null;

                    if (song)
                    {
                        running = true;
                        if (isKnownItemType)
                            msg += track.IsPlaying ? $"-Playing: {name}-\n" : "-Paused-\n";
                    }

                    if (progress)
                    {
                        running = true;
                        if (isKnownItemType && track.IsPlaying)
                        {
                            msg += GenerateProgressBar(track.ProgressMs, durationMs, 15) + "\n";
                            TimeSpan progressT = TimeSpan.FromMilliseconds(track.ProgressMs);
                            TimeSpan durationT = TimeSpan.FromMilliseconds(durationMs);
                            string progressTime = string.Format("{0:D2}m:{1:D2}s", progressT.Minutes, progressT.Seconds);
                            string durationTime = string.Format("{0:D2}m:{1:D2}s", durationT.Minutes, durationT.Seconds);
                            msg += progressTime + " / " + durationTime + "\n";
                        }
                    }

                    if (time)
                    {
                        running = true;
                        msg += DateTime.Now.ToString("HH:mm:ss tt") + "\n";
                    }

                    if (stt)
                    {
                        if (msgSst != lastStt) {
                            totalStt += msgSst;
                            lastStt = msgSst;
                        }

                        if(loopStt < 4)
                        {
                            msg += totalStt;
                        }
                        else
                        {
                            loopStt = 0;
                            totalStt = "";
                        }


                    }

                    if ((!song || !progress || !time || !stt) && msg == "" && running)
                    {
                        running = false;
                        OscChatbox.SendMessage("", direct: true);

                    }
                        

                    if ((song || progress || time || stt) && msg != "")
                        OscChatbox.SendMessage(msg, direct: true);
                }
                Thread.Sleep(1500);
            }
        }

        // Spotify reports the currently playing item as either a FullTrack or a
        // FullEpisode - different types, but UpdateChat only ever needs their name and
        // duration. Returns (null, 0) for anything else (there isn't a third case today,
        // but this keeps that path silent instead of throwing, same as the old switch
        // with no default case).
        private static (string Name, int DurationMs) GetPlayingItemInfo(CurrentlyPlayingContext track)
        {
            switch (track.Item.Type)
            {
                case ItemType.Track:
                    FullTrack fullTrack = (FullTrack)track.Item;
                    return (fullTrack.Name, fullTrack.DurationMs);
                case ItemType.Episode:
                    FullEpisode fullEpisode = (FullEpisode)track.Item;
                    return (fullEpisode.Name, fullEpisode.DurationMs);
                default:
                    return (null, 0);
            }
        }

        public string GenerateProgressBar(int timestamp, int duration, int progressBarLength=50)
        {
            // Calculate the percentage of song completion, clamped so a progress value
            // at or past the track end (which happens in practice near song end) can't
            // push remainingCharacters negative below.
            double percentage = duration <= 0 ? 0 : Math.Max(0, Math.Min(1, (double)timestamp / duration));

            // Calculate the number of characters to represent past and remaining time
            int pastCharacters = (int)(percentage * progressBarLength);
            int remainingCharacters = progressBarLength - pastCharacters;

            // Build the progress bar string
            string progressBar = new string('█', pastCharacters) + new string('▒', remainingCharacters);

            // Display the progress bar
            return progressBar;
        }

        // Compiled once instead of on every call - this runs on every OSC "Strip" parameter change.
        static readonly Regex digitsRegex = new Regex(@"\d+", RegexOptions.Compiled);

        static string AddSquareBrackets(string input)
        {
            // Replace numbers in the string with numbers enclosed in square brackets
            string result = digitsRegex.Replace(input, match => "[" + match.Value + "]");

            return result;
        }

        public float TranslateValue(float inputValue)
        {
            // Ensure the input value is within the range [-1, 1]
            inputValue = Math.Max(-1, Math.Min(1, inputValue));

            // Translate the input value to the range [-60, 12]
            float translatedValue = (inputValue + 1) * (12 + 60) / 2 - 60;

            return translatedValue;
        }

        public float ReverseTranslateValue(float translatedValue)
        {
            // Ensure the translated value is within the range [-60, 12]
            translatedValue = Math.Max(-60, Math.Min(12, translatedValue));

            // True inverse of TranslateValue: (x+1)*(12-(-60))/2 + (-60) solved for x.
            float inputValue = (translatedValue + 60) * 2 / (12 + 60) - 1;

            return inputValue;
        }

        private void SyncParameter(RemoteControle remoteControle, OscAvatarConfig oscAvatar)
        {
            // remoteControle is null when VoiceMeeter isn't installed (see MainWindow
            // ctor) - skip the strip sync but still sync the non-VoiceMeeter (Spotify)
            // state below, same as the null check already used for HandleStripParameter.
            if (remoteControle != null)
            {
                // VoiceMeeter edition -> how many A/B avatar parameters to sync per strip:
                // 4 for Standard, 5 for Banana, 6 for anything else (Potato).
                int buttonsPerStrip = remoteControle.type == 1 ? 4 : remoteControle.type == 2 ? 5 : 6;

                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < buttonsPerStrip; j++)
                    {
                        OscParameter.SendAvatarParameter($"Handy/Strip{i}/A{j + 1}", remoteControle.getBoolParameter($"Strip[{i}].A{j + 1}"));
                        OscParameter.SendAvatarParameter($"Handy/Strip{i}/B{j + 1}", remoteControle.getBoolParameter($"Strip[{i}].B{j + 1}"));
                    }

                    OscParameter.SendAvatarParameter($"Handy/Strip{i}/Mute", remoteControle.getBoolParameter($"Strip[{i}].Mute"));
                    OscParameter.SendAvatarParameter($"Handy/Strip{i}/Gain", ReverseTranslateValue(remoteControle.getParameter($"Strip[{i}].Gain")));
                }
            }

            var track = updateSpotify.track;
            if ( track != null )
            {
                OscParameter.SendAvatarParameter("Handy/Spotify/PlayPause", track.IsPlaying);
            }
        }
    }
}
