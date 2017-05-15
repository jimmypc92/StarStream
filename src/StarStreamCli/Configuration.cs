namespace StarStreamCli
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.IO;

    public class Configuration
    {
        public string FfmpegPath { get; set; }
        public string TwitchClientId { get; set; }

        public Configuration(string path)
        {
            string content = File.ReadAllText(path);
            var configObj = JsonConvert.DeserializeObject<JObject>(content);

            this.FfmpegPath = configObj.Value<string>("ffmpeg_path");
            this.TwitchClientId = configObj.Value<string>("twitch_client_id");
        }
    }
}
