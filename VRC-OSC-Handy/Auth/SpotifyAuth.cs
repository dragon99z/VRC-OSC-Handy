using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using CefSharp.Wpf;
using System.IO;
using VRC_OSC_Handy.Logger;
using VRC_OSC_Handy;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace VRC_OSC_Handy.Auth
{
    internal class SpotifyAuth
    {

        private EmbedIOAuthServer _server;
        SpotifyClient spotify;
        SpotifyClientConfig spotifyconfig;

        AuthWindow authWindow = new AuthWindow();

        public void runAuth()
        {
            Auth().GetAwaiter().GetResult();
        }

        public async Task Auth()
        {
            // Make sure "http://localhost:5543/callback" is in your spotify application as redirect uri!
            _server = new EmbedIOAuthServer(new Uri("http://localhost:5656/callback"), 5656);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, MainWindow.config.SpotifyConfig.ClientID, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { Scopes.UserReadEmail, Scopes.AppRemoteControl, Scopes.Streaming, Scopes.UserReadPlaybackState, Scopes.UserReadCurrentlyPlaying, Scopes.UserModifyPlaybackState }
            };

            var uiAccess = authWindow.Dispatcher.CheckAccess();

            var settings = new CefSettings();
            settings.CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache");

            if (uiAccess)
            {
                authWindow.Show();
                Thread.Sleep(100);
                authWindow.AuthBrowser.LoadUrl(request.ToUri().ToString());
            }
            else
            {
                authWindow.Dispatcher.Invoke(() => {
                    authWindow.Show();
                    Thread.Sleep(100);
                    authWindow.AuthBrowser.LoadUrl(request.ToUri().ToString());
                });
            }



            
            //BrowserUtil.Open(request.ToUri());
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            spotifyconfig = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(spotifyconfig).RequestToken(
              new AuthorizationCodeTokenRequest(
                MainWindow.config.SpotifyConfig.ClientID, MainWindow.config.SpotifyConfig.ClientSecret, response.Code, new Uri("http://localhost:5656/callback")
              )
            );

            var uiAccess = authWindow.Dispatcher.CheckAccess();

            if (uiAccess)
            {
                authWindow.Close();
            }
            else
            {
                authWindow.Dispatcher.Invoke(() => {
                    authWindow.Close();
                });
            }         

            spotify = new SpotifyClient(tokenResponse.AccessToken);
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                MainWindow.spotify = spotify;
                MainWindow.config.SpotifyConfig.Enabled = true;
                MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.hideSpotifyInput();
                var configJsonString = JsonConvert.SerializeObject(MainWindow.config, Formatting.Indented);
                File.WriteAllText(MainWindow.cfg_path + "config.json", configJsonString);
            });
            
            // do calls with Spotify and save token?
        }

        private async Task OnErrorReceived(object sender, string error, string state)
        {
            DebugLogger.LogError($"Aborting authorization, error received: {error}");
            await _server.Stop();
        }

    }
}
