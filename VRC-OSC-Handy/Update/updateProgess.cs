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

                if (track?.Item != null) // Item is null during ads/private sessions
                {
                    int? durationMs = GetDurationMs(track);
                    if (durationMs.HasValue)
                    {
                        double percent = track.ProgressMs / (double)durationMs.Value * 100;
                        setProgess(bar, (percent / 100) * MainWindow.songTextWidth);
                    }
                }

                Thread.Sleep(100);
            }
        }

        // Track and Episode both just need a duration - the only difference is which
        // Spotify type they're cast from. Null for anything else (there isn't a third
        // case today, same as the old switch with no default case).
        private static int? GetDurationMs(CurrentlyPlayingContext track)
        {
            switch (track.Item.Type)
            {
                case ItemType.Track:
                    return ((FullTrack)track.Item).DurationMs;
                case ItemType.Episode:
                    return ((FullEpisode)track.Item).DurationMs;
                default:
                    return null;
            }
        }

        public void setProgess(Rectangle bar, double w)
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
