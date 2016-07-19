namespace StarStream
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    class Program
    {
        static void Main(string[] args)
        {
            using (HttpClient client = new HttpClient()) {
                //client.BaseAddress = new Uri("http://www.twitch.tv");

                var channel = "2ggaming";
                var tokenUri = $"http://api.twitch.tv/api/channels/{channel}/access_token";

                var res = client.GetAsync(tokenUri).Result;

                var content = res.Content.ReadAsStringAsync().Result;
                Console.WriteLine(content);

                JObject tokenWrapper = JsonConvert.DeserializeObject<JObject>(content);

                File.WriteAllText(GetNowPath() + ".txt", content);

                string token = tokenWrapper.Value<string>("token");
                string sig = tokenWrapper.Value<string>("sig");
                string random = "123412";

                var playlistUri = $"http://usher.twitch.tv/api/channel/hls/{channel}.m3u8?player=twitchweb&&token={token}&sig={sig}&allow_audio_only=true&allow_source=true&type=any&p={random}";

                res = client.GetAsync(playlistUri).Result;

                content = res.Content.ReadAsStringAsync().Result;
                Console.WriteLine($"{Environment.NewLine}{content}");

                File.WriteAllText(GetNowPath() + ".txt", content);

                var lines = content.Split('\n');

                string m38Uri = "";

                foreach(var line in lines) {
                    if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) {
                        m38Uri = line;
                        break;
                    }
                }

                if(string.IsNullOrEmpty(m38Uri)) {
                    throw new Exception("Could not find m3u8 uri.");
                }

                content = client.GetAsync(m38Uri).Result.Content.ReadAsStringAsync().Result;
                Console.WriteLine($"{Environment.NewLine}{content}");

                File.WriteAllText(GetNowPath() + ".txt", content);

                ConsumeM3U8(m38Uri);

                Console.WriteLine(Environment.NewLine);
                Console.WriteLine("Finished. Press any key to continue. . . ");
                Console.ReadKey();
            }
        }

        private static string GetNowPath()
        {
            return string.Format("result-{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);
        }

        private static void ConsumeM3U8(string m3u8Uri)
        {
            Queue<string> queue = new Queue<string>();
            List<string> indexLines = new List<string>();
            string folder = GetNowPath();
            string path = Path.Combine(folder, folder + ".m3u8");
            int currentIndex = -1;

            Directory.CreateDirectory(folder);

            var consumer = new Consumer(queue, folder, m3u8Uri.Substring(0, m3u8Uri.IndexOf("index-live.m3u8")));
            consumer.Consume();
            
            using (HttpClient client = new HttpClient()) {

                while(true) {
                    string content = client.GetAsync(m3u8Uri).Result.Content.ReadAsStringAsync().Result;

                    var lines = content.Split('\n');
                    indexLines.Clear();

                    for(int i = 0; i < lines.Length; i++) {
                        if(lines[i].StartsWith("index", StringComparison.OrdinalIgnoreCase)) {
                            indexLines.Add(lines[i]);

                            int index = int.Parse(lines[i].Substring(6, 10));

                            if(index > currentIndex) {
                                currentIndex = index;
                                queue.Enqueue(lines[i]);
                                using (var sw = File.AppendText(path)) {
                                    sw.AutoFlush = true;
                                    Console.WriteLine($"Writing to the continuous m3u8 file: {lines[i-1]}");
                                    sw.WriteLine(lines[i - 1]);
                                    Console.WriteLine($"Writing to the continuous m3u8 file: {lines[i]}");
                                    sw.WriteLine(lines[i]);
                                }
                            }
                        }
                    }
                }





            }
        }

        private class Consumer
        {
            private Queue<string> _queue;
            private string _baseUrl;
            private string _rootDir;
            private Thread _thread;
            private bool _shouldStop;

            public Consumer(Queue<string> queue, string rootDir, string baseUrl)
            {
                this._queue = queue;
                this._baseUrl = baseUrl;
                this._rootDir = rootDir;
            }

            public void Consume()
            {
                if(this._thread != null) {
                    return;
                }

                this._thread = new Thread(this._Consume);

                this._thread.Start(null);
            }

            private void _Consume(object threadStartParam)
            {
                using (HttpClient client = new HttpClient()) {

                    while(!_shouldStop) {

                        if(this._queue.Count < 1) {
                            continue;
                        }

                        var indexPath = this._queue.Dequeue();
                        var url = this._baseUrl + indexPath;

                        var res = client.GetAsync(url).Result;
                        var bytes = res.Content.ReadAsByteArrayAsync().Result;

                        var writePath = Path.Combine(this._rootDir, indexPath);
                        Console.WriteLine($"Writing to {indexPath}");
                        File.WriteAllBytes(writePath, bytes);
                    }
                }
            }

            public void Stop()
            {
                this._shouldStop = true;
            }

        }

    }
}
