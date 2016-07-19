namespace StarStream
{
    using System;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            string channel = "";

            var channelInfo = new ChannelInfo(channel);

            ConsumeM3U8(channelInfo.m3u8Uri);
        }

        private static string GetNowPath()
        {
            return string.Format("result-{0:yyyy-MM-dd_hh-mm-ss_fff_tt}", DateTime.Now);
        }

        private static HlsConsumer ConsumeM3U8(string m3u8Uri)
        {
            string folder = GetNowPath();
            string path = Path.Combine(folder, folder + ".m3u8");

            Directory.CreateDirectory(folder);

            var consumer = new HlsConsumer(m3u8Uri);

            consumer.ReceivedInitialM3u8 = (content) => {
                File.WriteAllText(path, content);
                Console.WriteLine("Received Initial m3u8.");
                Console.WriteLine(content);
            };

            consumer.RecievedExtinf = (extinf) => {
                Console.WriteLine($"Received Extinf");
                using(var sw = File.AppendText(path)) {
                    sw.Write(extinf[0]);
                    sw.Write('\r');
                    sw.Write(extinf[1]);
                    sw.Write('\r');
                }
            };

            consumer.ReceivedSegment = (seg) => {
                Console.WriteLine($"Received segment {seg.Name}.");
                File.WriteAllBytes(Path.Combine(folder, seg.Name), seg.Content);
            };

            consumer.Consume();
            while(consumer.Consuming) { }

            return consumer;
        }
    }
}
