using SpotifyAPI.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace VRC_OSC_Handy.Update
{
    internal class updateProgess
    {
        CancellationTokenSource updateToken;
        CancellationToken ct;

        public void run(Rectangle bar)
        {
            updateToken = new CancellationTokenSource();
            ct = updateToken.Token;
            Task.Run(() => UpdateSongProgress(bar), updateToken.Token);
        }

        public void UpdateSongProgress(Rectangle bar)
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
                            double persentTrack = track.ProgressMs / (double)fullTrack.DurationMs * 100;
                            setProgess(bar, (persentTrack / 100) * MainWindow.songTextWidth);
                            break;
                        case ItemType.Episode:
                            FullEpisode fullEpisod = (FullEpisode)track.Item;
                            double persentEpisod = track.ProgressMs / (double)fullEpisod.DurationMs * 100;
                            setProgess(bar, (persentEpisod / 100) * MainWindow.songTextWidth);
                            break;
                    }
                }

                Thread.Sleep(100);
            }
        }

        public void setProgess(Rectangle bar,double w)
        {
            var uiAccess = bar.Dispatcher.CheckAccess();

            if (uiAccess)
                bar.Width = w;
            else
                bar.Dispatcher.Invoke(() => { bar.Width = w; });
        }

        public void stop()
        {
            updateToken.Cancel();
        }

    }
}
