using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VRC_OSC_Handy.Func;

namespace VRC_OSC_Handy.Update
{
    internal class updateName
    {
        CancellationTokenSource updateToken;
        CancellationToken ct;

        ImageSource image = ByteImageConverter.ByteToImage(Properties.Resources.image);
        ImageSource ado = ByteImageConverter.ByteToImage(Properties.Resources.ado);

        public void run(TextBlock Song, ImageSource Icon)
        {
            updateToken = new CancellationTokenSource();
            ct = updateToken.Token;
            Task.Run(() => UpdateSongName(Song, Icon), updateToken.Token);
        }
        public void UpdateSongName(TextBlock Song, ImageSource Icon)
        {
            while (!ct.IsCancellationRequested)
            {
                var track = updateSpotify.track;
                if (track?.Item != null) // Item is null during ads/private sessions
                {
                    switch (track.Item.Type)
                    {
                        case ItemType.Track:
                            FullTrack fullTrack = (FullTrack)track.Item;
                            WriteSong(Song, fullTrack.Name);
                            ChangeIconEasterEgg(fullTrack.Artists, Icon);


                            break;
                        case ItemType.Episode:
                            FullEpisode fullEpisod = (FullEpisode)track.Item;
                            WriteSong(Song, fullEpisod.Name);
                            break;
                    }
                }
                Thread.Sleep(300);
            }
        }

        public void WriteSong(TextBlock Song, string song)
        {
            void UpdateSongText()
            {
                Song.Text = song;
                Song.FontSize = song.Length > 30 ? 12 : 18;
                MainWindow.songTextWidth = Song.ActualWidth;
            }

            if (Song.Dispatcher.CheckAccess())
                UpdateSongText();
            else
                Song.Dispatcher.Invoke(UpdateSongText);
        }

        public void ChangeIconEasterEgg(List<SimpleArtist> artists, ImageSource Icon)
        {
            if (Application.Current == null)
                return;

            // Only one artist ID triggers the easter egg icon; everything else gets the
            // normal icon. Decide once instead of dispatching a UI update per artist in
            // the loop, which used to flip the window icon back and forth while scanning
            // a multi-artist track.
            bool isEasterEggArtist = artists.Exists(artist => artist.Id == "6mEQK9m2krja6X1cfsAjfl");
            ImageSource targetIcon = isEasterEggArtist ? ado : image;

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                if (mainWindow.Icon != targetIcon)
                    mainWindow.Icon = targetIcon;
            });
        }

        public void stop()
        {
            updateToken.Cancel();
        }

    }

}
