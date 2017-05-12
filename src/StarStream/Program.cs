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
        }

        private static void InitializeConfiguration()
        {
            Configuration = new Configuration("./configuration.json");
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
                case "consume":
                    Consume(options);
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

            string list = "List";
            Console.WriteLine("  {0} List current broadcasting Twitch streams.", list.PadRight(14));
            Console.WriteLine();

            string consume = "Consume";
            Console.WriteLine("  {0} Consume the channel with the specified name.", consume.PadRight(14));
            Console.WriteLine();

            Console.WriteLine("Use 'StarStream help <command>' to read more about a specific command.");
        }

        private static void Consume(Dictionary<string, string> options = null)
        {
            string channel = null;

            if (options != null)
            {
                options.TryGetValue("-channel", out channel);
            }

            if (string.IsNullOrEmpty(channel))
            {
                Help("channel");
                return;
            }

            ChannelConsumer.Consume(channel);
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
    }
}
