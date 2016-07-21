namespace StarStream
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        public static IEnumerable<Stream> GetStreams() {

    
            using (var client = new HttpClient()) {
                var res = client.GetAsync("https://api.twitch.tv/kraken/streams").Result.Content.ReadAsStringAsync().Result;

                var resource = JsonConvert.DeserializeObject<JObject>(res);
                var jObjs = resource.Value<JArray>("streams");

                return jObjs.Select(o => Stream.FromJObject(o as JObject));
            }

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

    public class Stream
    {
        public string Game { get; set; }
        public long Viewers { get; set; }
        public long VideoHeight { get; set; }
        public bool IsPlaylist { get; set; }
        public Channel Channel { get; set; }
        public long Id { get; set; }

        public static Stream FromJObject(JObject jObj)
        {
            return new Stream() {
                Game = jObj.Value<string>("game"),
                Viewers = jObj.Value<long>("viewers"),
                VideoHeight = jObj.Value<long>("video_height"),
                IsPlaylist = jObj.Value<bool>("is_playlist"),
                Channel = Channel.FromJObject(jObj.Value<JObject>("channel")),
                Id = jObj.Value<long>("_id")
            };
        }

        public override string ToString()
        {
            return $"Viewers: {this.Viewers}, Channel Data - {this.Channel.ToString()}";
        }
    }

    public class Channel
    {
        public bool? Mature { get; set; }
        public string Status { get; set; }
        public string BroadcasterLanguage { get; set; }
        public string DisplayName { get; set; }
        public string Game { get; set; }
        public string Language { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public long Views { get; set; }
        public long Followers { get; set; }

        public static Channel FromJObject(JObject jObj)
        {
            return new Channel() {
                Mature = jObj.Value<bool?>("mature"),
                Status = jObj.Value<string>("status"),
                BroadcasterLanguage = jObj.Value<string>("broadcaster_language"),
                DisplayName = jObj.Value<string>("display_name"),
                Game = jObj.Value<string>("game"),
                Language = jObj.Value<string>("language"),
                Id = jObj.Value<int>("_id"),
                Name = jObj.Value<string>("name"),
                Url = jObj.Value<string>("url"),
                Views = jObj.Value<int>("views"),
                Followers = jObj.Value<int>("followers")
            };
        }

        public override string ToString()
        {
            return $"Name: {this.Name} | Game: {this.Game}";
        }
    }
}