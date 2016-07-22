﻿namespace StarStream
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.IO;

    class Configuration
    {
        public string FfmpegPath { get; set; }

        public Configuration(string path)
        {
            string content = File.ReadAllText(path);
            var configObj = JsonConvert.DeserializeObject<JObject>(content);

            this.FfmpegPath = configObj.Value<string>("ffmpeg_path");
        }
    }
}