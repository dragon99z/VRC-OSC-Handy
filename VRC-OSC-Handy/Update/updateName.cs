using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VRC_OSC_Handy.Config;
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
                if (track != null)
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

        public void WriteSong(TextBlock Song ,string song)
        {
            var uiAccess = Song.Dispatcher.CheckAccess();

            if (uiAccess)
            {
                Song.Text = song;
                if (song.Length > 30)
                    Song.FontSize = 12;
                else
                    Song.FontSize = 18;
                MainWindow.songTextWidth = Song.ActualWidth;
            }
            else
            {
                Song.Dispatcher.Invoke(() => {
                    Song.Text = song;
                    if (song.Length > 30)
                        Song.FontSize = 12;
                    else
                        Song.FontSize = 18;
                    MainWindow.songTextWidth = Song.ActualWidth;
                });
            }

        }

        public void ChangeIconEasterEgg(List<SimpleArtist> artists, ImageSource Icon)
        {

            bool uiAccess = (Application.Current != null);

            foreach (SimpleArtist artist in artists)
            {
                if (artist.Id == "6mEQK9m2krja6X1cfsAjfl")
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                        if (mainWindow.Icon != ado)
                            mainWindow.Icon = ado;

                    });
                    break;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                        if (mainWindow.Icon != image)
                            mainWindow.Icon = image;

                    });
                }

            }



        }

        public void stop()
        {
            updateToken.Cancel();
        }

    }

}
