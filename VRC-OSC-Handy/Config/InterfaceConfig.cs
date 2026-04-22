namespace VRC_OSC_Handy.Config
{
    public class InterfaceConfig
    {
        public SpotifyConfig SpotifyConfig { get; set; }
        public STT STT { get; set; }

    }

    public class SpotifyConfig
    {
        public bool Enabled { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
    }

    public class STT
    {
        public int Model { get; set; }
        public int Language { get; set; }
        public bool Translate { get; set; }
    }
}
