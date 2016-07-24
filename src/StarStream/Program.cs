namespace StarStream
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    class Program
    {
        public static Configuration Configuration { get; set; }

        static void Main(string[] args)
        {
            InitializeConfiguration();

            ParseCommandLine(args);

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

        private static void ParseCommandLine(string[] args)
        {
            if (args.Length == 0) {
                Help();
                return;
            }

            Dictionary<string, string> options = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++) {

                if (args[i].StartsWith("-")) {
                    string val = null;

                    if (i + 1 < args.Length) {
                        val = args[i + 1];
                    }

                    options[args[i]] = val;
                }
            }

            switch(args[0].ToLower()) {
                case "list":
                    ListStreams(options);
                    break;
                default:
                    Console.WriteLine("Unknown command: {0}", args[0]);
                    Help();
                    break;
            }
        }

        private static void Help(string command = null)
        {
            Console.WriteLine("usage: StarStream <command> [<args>]");
            Console.WriteLine();
            Console.WriteLine("  {0} List current broadcasting Twitch streams.", "List".PadRight(10));
            Console.WriteLine();
            Console.WriteLine("Use 'StarStream help <command>' to read more about a specific command.");
        }

        private static void ListStreams(Dictionary<string, string> options = null)
        {
            int offset = 0;
            string input = "";
            int perPage = 25;

            if (options != null) {

                if (options.ContainsKey("-n")) {

                    if (!int.TryParse(options["-n"], out perPage)) {
                        Console.WriteLine("Invalid value for -n");
                    }
                }
            }

            while (true) {
                var streams = TwitchApiWrap.GetStreams(offset, perPage).ToArray();
                Console.WriteLine("Listing streams {0}-{1}", offset + 1, offset + perPage);

                for (int i = 0; i < streams.Length; i++) {
                    Console.WriteLine("{0}. {1}", offset + i + 1, streams[i]);
                }

                Console.Write("q: Exit. ");
                Console.Write("n: List next {0}. ", perPage);
                if (offset > 0) {
                    Console.Write("p: List previous {0}. ", perPage);
                }
                Console.WriteLine();
                input = Console.ReadLine();

                if (input == "n") {
                    offset += perPage;
                }
                else if (input == "p" && offset >= perPage) {
                    offset -= perPage;
                }
                else if (input == "q") {
                    Console.WriteLine("Exiting.");
                    break;
                }
                else {
                    Console.WriteLine("Unknown input received.");
                }
            }
        }

        private static void WaitForUserInput()
        {
            var then = DateTime.UtcNow;
            while (!Console.KeyAvailable) {

                if (DateTime.UtcNow - then > TimeSpan.FromSeconds(3)) {
                    Console.WriteLine("Press any key to stop.");
                    then = DateTime.UtcNow;
                }
            }
        }
    }
}
