namespace StarStream
{
    using System.IO;
    using System;

    public class ChannelConsumer
    {
        public static void Consume(string channel)
        {
            string folder = string.Format("{0}_{1:yyyy-MM-dd_hh-mm-ss_fff_tt}", channel, DateTime.Now);
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
                using (var sw = File.AppendText(path))
                {
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
        }

        private static void WaitForUserInput()
        {
            var then = DateTime.UtcNow;
            while (!Console.KeyAvailable)
            {

                if (DateTime.UtcNow - then > TimeSpan.FromSeconds(3))
                {
                    Console.WriteLine("Press any key to stop.");
                    then = DateTime.UtcNow;
                }
            }
        }
    }
}
