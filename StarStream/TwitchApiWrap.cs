namespace StarStream
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;

    class TwitchApiWrap
    {
        public static ChannelToken GetChannelToken(string channel)
        {
            using (HttpClient client = new HttpClient()) {

                var tokenUri = $"http://api.twitch.tv/api/channels/{channel}/access_token";

                var res = client.GetAsync(tokenUri).Result;

                var content = res.Content.ReadAsStringAsync().Result;

                return new ChannelToken(JsonConvert.DeserializeObject<JObject>(content));
            }
        }

        public static string GetPlaylistUri (string channel)
        {
            var cTok = GetChannelToken(channel);
            string random = "123412";
            
            return $"http://usher.twitch.tv/api/channel/hls/{channel}.m3u8?player=twitchweb&&token={cTok.Token}&sig={cTok.Sig}&allow_audio_only=true&allow_source=true&type=any&p={random}";
        }

        public static string getM3u8Uri(string channel)
        {
            var playlistUri = GetPlaylistUri(channel);
            string playlist = "";

            using (HttpClient client = new HttpClient()) {
                playlist = client.GetAsync(playlistUri).Result.Content.ReadAsStringAsync().Result;
            }
            
            if(string.IsNullOrEmpty(playlistUri)) {
                throw new Exception("Invalid playlist.");
            }

            var lines = playlist.Split('\n');

            foreach(var line in lines) {
                if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) {
                    return line;
                }
            }

            throw new Exception("Could not find m3u8 uri.");
        }
    }

    public class ChannelToken
    {
        public string Token { get; set; }
        public string Sig { get; set; }

        public ChannelToken(JObject jObj)
        {
            this.Token = jObj.Value<string>("token");
            this.Sig = jObj.Value<string>("sig");
        }
    }

    public class ChannelInfo
    {
        public ChannelToken ChannelToken { get; set; }
        public string PlaylistUri { get; set; }
        public string m3u8Uri { get; set; }

        public ChannelInfo(string channel)
        {
            this.ChannelToken = TwitchApiWrap.GetChannelToken(channel);
            this.PlaylistUri = TwitchApiWrap.GetPlaylistUri(channel);
            this.m3u8Uri = TwitchApiWrap.getM3u8Uri(channel);
        }
    }
}
