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

            OscAvatarParameterChangedEventHandler handler = async (parameter, e) =>
            {
                if (parameter.Name.Contains("Handy"))
                {
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
                    {
                        param[0] = AddSquareBrackets(param[0]);
                        if (param[1].Equals("Gain"))
                        {
                            remoteControle.changeParameter(param[0] + "." + param[1], TranslateValue((float)e.NewValue));
                        }
                        else
                        {
                            remoteControle.toggleBoolParameter(param[0] + "." + param[1], e.NewValue.ToBoolean());
                        }
                    }
                    else if (param[0].Equals("Spotify") && spotify != null)
                    {
                        try
                        {
                            var track = updateSpotify.track;
                            if (track != null)
                            {
                                switch (param[1])
                                {
                                    case "Next":
                                        if ((bool)e.NewValue)
                                        {
                                            if (track.IsPlaying)
                                            {
                                                spotify.Player.SkipNext().GetAwaiter().GetResult();
                                            }
                                        }
                                        break;
                                    case "Last":
                                        if ((bool)e.NewValue)
                                        {
                                            if (track.IsPlaying)
                                            {
                                                spotify.Player.SkipPrevious().GetAwaiter().GetResult();
                                            }
                                        }

                                        break;
                                    case "PlayPause":
                                        if (!track.IsPlaying && (bool)e.NewValue)
                                        {
                                            spotify.Player.ResumePlayback().GetAwaiter().GetResult();
                                        }
                                        else if (track.IsPlaying && !(bool)e.NewValue)
                                        {
                                            spotify.Player.PausePlayback().GetAwaiter().GetResult();
                                        }
                                        break;
                                    case "Song":
                                        song = (bool)e.NewValue;
                                        break;
                                    case "ProgressBar":
                                        progress = (bool)e.NewValue;
                                        break;
                                }
                            }
                        }
                        catch (APITooManyRequestsException ex)
                        {
                            Thread.Sleep(sleep);
                            sleep += 5000;
                        }


                    }
                    else if (param[0].Equals("Other"))
                    {
                        switch (param[1])
                        {
                            case "Time":
                                time = (bool)e.NewValue;
                                break;
                            case "STT":
                                if(wisper.isRunning != (bool)e.NewValue)
                                {
                                    if ((bool)e.NewValue)
                                    {
                                        wisper.start(modelPath, MicrophoneCapture.LANGUAGES.Keys.ElementAt(MainWindow.config.STT.Language), MainWindow.config.STT.Translate);
                                        stt = true;
                                    }
                                    else
                                    {
                                        wisper.stop();
                                        stt = false;
                                    }
                                        
                                }
                                break;
                        }
                    }
                }
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
                if (track != null)
                {
                    string msg = "";
                    if (song)
                    {
                        running = true;
                        switch (track.Item.Type)
                        {
                            case ItemType.Track:
                                FullTrack fullTrack = (FullTrack)track.Item;
                                if (track.IsPlaying)
                                    msg += "-Playing: " + fullTrack.Name + "-\n";
                                else
                                    msg += "-Paused-\n";

                                break;
                            case ItemType.Episode:
                                FullEpisode fullEpisod = (FullEpisode)track.Item;
                                if (track.IsPlaying)
                                    msg += "-Playing: " + fullEpisod.Name + "-\n";
                                else
                                    msg += "-Paused-\n";
                                break;
                        }
                    }

                    if (progress)
                    {
                        running = true;
                        switch (track.Item.Type)
                        {
                            case ItemType.Track:
                                FullTrack fullTrack = (FullTrack)track.Item;
                                if (track.IsPlaying)
                                {
                                    msg += (GenerateProgressBar(track.ProgressMs, fullTrack.DurationMs, 15) + "\n");
                                    TimeSpan progressT = TimeSpan.FromMilliseconds(track.ProgressMs);
                                    TimeSpan durationT = TimeSpan.FromMilliseconds(fullTrack.DurationMs);
                                    string progressTime = string.Format("{0:D2}m:{1:D2}s", progressT.Minutes, progressT.Seconds);
                                    string durationTime = string.Format("{0:D2}m:{1:D2}s", durationT.Minutes, durationT.Seconds);
                                    msg += (progressTime + " / " + durationTime + "\n");
                                }
                                    
                                    
                                break;
                            case ItemType.Episode:
                                FullEpisode fullEpisod = (FullEpisode)track.Item;
                                if (track.IsPlaying)
                                {
                                    msg += (GenerateProgressBar(track.ProgressMs, fullEpisod.DurationMs, 15) + "\n");
                                    TimeSpan progressT = TimeSpan.FromMilliseconds(track.ProgressMs);
                                    TimeSpan durationT = TimeSpan.FromMilliseconds(fullEpisod.DurationMs);
                                    string progressTime = string.Format("{0:D2}m:{1:D2}s", progressT.Minutes, progressT.Seconds);
                                    string durationTime = string.Format("{0:D2}m:{1:D2}s", durationT.Minutes, durationT.Seconds);
                                    msg += (progressTime + " / " + durationTime + "\n");
                                }
                                break;
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

        public string GenerateProgressBar(int timestamp, int duration, int progressBarLength=50)
        {
            // Calculate the percentage of song completion
            double percentage = (double)timestamp / duration;

            // Calculate the number of characters to represent past and remaining time
            int pastCharacters = (int)(percentage * progressBarLength);
            int remainingCharacters = progressBarLength - pastCharacters;

            // Build the progress bar string
            string progressBar = new string('█', pastCharacters) + new string('▒', remainingCharacters);

            // Display the progress bar
            return progressBar;
        }

        static string AddSquareBrackets(string input)
        {
            // Regular expression to match numbers
            Regex regex = new Regex(@"\d+");

            // Replace numbers in the string with numbers enclosed in square brackets
            string result = regex.Replace(input, match => "[" + match.Value + "]");

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

            // Reverse the translation to get the original input value
            float inputValue = (2 * translatedValue - (12 + 60)) / (12 + 60);

            return inputValue;
        }

        private void SyncParameter(RemoteControle remoteControle, OscAvatarConfig oscAvatar)
        {

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < (remoteControle.type == 1 ? 4 : remoteControle.type == 2 ? 5 : 6); j++)
                {
                    OscParameter.SendAvatarParameter($"Handy/Strip{i}/A{j + 1}", remoteControle.getBoolParameter($"Strip[{i}].A{j + 1}"));
                    OscParameter.SendAvatarParameter($"Handy/Strip{i}/B{j + 1}", remoteControle.getBoolParameter($"Strip[{i}].B{j + 1}"));
                }

                OscParameter.SendAvatarParameter($"Handy/Strip{i}/Mute", remoteControle.getBoolParameter($"Strip[{i}].Mute"));
                OscParameter.SendAvatarParameter($"Handy/Strip{i}/Gain", ReverseTranslateValue(remoteControle.getParameter($"Strip[{i}].Gain")));
            }

            var track = updateSpotify.track;
            if ( track != null )
            {
                OscParameter.SendAvatarParameter("Handy/Spotify/PlayPause", track.IsPlaying);
            }
        }
    }
}
