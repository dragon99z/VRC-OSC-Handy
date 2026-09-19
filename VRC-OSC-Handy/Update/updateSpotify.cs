using SpotifyAPI.Web;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VRC_OSC_Handy.Update
{
    internal class updateSpotify
    {
        public static CurrentlyPlayingContext track;

        CancellationTokenSource updateToken;
        CancellationToken ct;

        public void run(SpotifyClient spotify)
        {
            updateToken = new CancellationTokenSource();
            ct = updateToken.Token;
            Task.Run(() => Update(spotify), updateToken.Token);

        }

        private async Task Update(SpotifyClient spotify)
        {
            int sleep = 5000;
            while (!ct.IsCancellationRequested)
            {
                if (Application.Current != null)
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        spotify = MainWindow.spotify;

                    });

                if (spotify != null)
                {
                    try
                    {
                        track = await spotify.Player.GetCurrentPlayback();
                        sleep = 5000; // reset backoff after a successful call
                    }
                    catch (APITooManyRequestsException)
                    {
                        await Task.Delay(sleep);
                        sleep += 5000;
                    }
                }
                await Task.Delay(1000);
            }
        }

        public void stop()
        {
            updateToken.Cancel();
        }
    }
}
