using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VRC_OSC_Handy.Auth;

namespace VRC_OSC_Handy
{
    // Spotify playback controls (next/pause/previous) and the client ID/secret
    // settings UI (masked text boxes + the "connect" button). Split out of
    // MainWindow.xaml.cs since it's a self-contained UI area.
    public partial class MainWindow
    {
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

        private void runSpotify_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (SotifyClientID.RealText != SpotifyClientIDPlaceholder && SotifyClientSecret.RealText != SpotifyClientSecretPlaceholder)
            {
                button.IsEnabled = false;
                button.Foreground = new SolidColorBrush(Colors.Red);
                config.SpotifyConfig.ClientID = SotifyClientID.RealText;
                config.SpotifyConfig.ClientSecret = SotifyClientSecret.RealText;

                SpotifyAuth auth = new SpotifyAuth();
                auth.runAuth();
                update();

            }
        }

        public void hideSpotifyInput()
        {
            runSpotify.Visibility = Visibility.Hidden;

            SotifyClientID.Visibility = Visibility.Hidden;
            SotifyClientIDText.Visibility = Visibility.Hidden;

            SotifyClientSecret.Visibility = Visibility.Hidden;
            SotifyClientSecretText.Visibility = Visibility.Hidden;
        }

        private void SotifyClientID_Loaded(object sender, RoutedEventArgs e)
        {
            SotifyClientID.RealText = config.SpotifyConfig.ClientID;

            SotifyClientID.IsMasked =
                SotifyClientID.RealText != SpotifyClientIDPlaceholder;
        }

        private void SotifyClientSecret_Loaded(object sender, RoutedEventArgs e)
        {
            SotifyClientSecret.RealText = config.SpotifyConfig.ClientSecret;

            SotifyClientSecret.IsMasked =
                SotifyClientSecret.RealText != SpotifyClientSecretPlaceholder;
        }

        private void SotifyClientID_LostFocus(object sender, RoutedEventArgs e)
        {
            SotifyClientID.IsMasked =
                SotifyClientID.RealText != SpotifyClientIDPlaceholder;
        }

        private void SotifyClientSecret_LostFocus(object sender, RoutedEventArgs e)
        {
            SotifyClientSecret.IsMasked =
                SotifyClientSecret.RealText != SpotifyClientSecretPlaceholder;
        }
    }
}
