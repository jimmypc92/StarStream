namespace StarStream
{
    using System;
    using System.IO;
    using System.Linq;

    class Program
    {
        public static Configuration Configuration { get; set; }

        static void Main(string[] args)
        {
            InitializeConfiguration();
            var streams = TwitchApiWrap.GetStreams();

            foreach(var stream in streams) {
                Console.WriteLine(stream);
            }

            ConsumeChannel(streams.First().Channel.Name);

            Console.ReadLine();
        }

        private static void InitializeConfiguration()
        {
            Configuration = new Configuration("./configuration.json");
        }

        private static string GetNowPath(string prefix = null)
        {
            return string.Format("{0}_{1:yyyy-MM-dd_hh-mm-ss_fff_tt}", prefix, DateTime.Now);
        }

        private static HlsConsumer ConsumeChannel(string channel)
        {
            string folder = GetNowPath(channel);
            string path = Path.Combine(folder, folder + ".m3u8");

            Directory.CreateDirectory(folder);

            var channelInfo = new ChannelInfo(channel);
            var consumer = new HlsConsumer(channelInfo.m3u8Uri);

            consumer.ReceivedInitialM3u8 = (content) => {
                File.WriteAllText(path, content);
                Console.WriteLine("Received Initial m3u8.");
                Console.WriteLine(content);
            };

            consumer.RecievedExtinf = (extinf) => {
                Console.WriteLine($"Received Extinf");
                using(var sw = File.AppendText(path)) {
                    sw.Write(extinf.Marker);
                    sw.Write(M3U8.LINE_END);
                    sw.Write(extinf.Path);
                    sw.Write(M3U8.LINE_END);
                }
            };

            consumer.ReceivedSegment = (seg) => {
                Console.WriteLine($"Received segment {seg.Name}.");
                File.WriteAllBytes(Path.Combine(folder, seg.Name), seg.Content);
            };

            consumer.Consume();
            WaitForUserInput();

            consumer.Stop();
            M3U8.AppendEnding(path);

            ConversionUtil.ConvertM3u8ToMp4(path);

            return consumer;
        }


        private static void WaitForUserInput()
        {
            var then = DateTime.UtcNow;
            while(!Console.KeyAvailable) {

                if(DateTime.UtcNow - then > TimeSpan.FromSeconds(3)) {
                    Console.WriteLine("Press any key to stop.");
                    then = DateTime.UtcNow;
                }
            }
        }
    }
}
