namespace StarStream
{
    using System.IO;
    using System;

    public class ChannelConsumer
    {
        private Client _client;
        private HlsConsumer _consumer;
        private string _m3u8OutputPath;

        public ChannelConsumer(Client client)
        {
            _client = client;
        }

        public string Consume(string channel, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory);
            }

            _m3u8OutputPath = Path.Combine(outputDirectory, outputDirectory + ".m3u8");

            var channelInfo = _client.GetChannelInfo(channel);
            _consumer = new HlsConsumer(channelInfo.m3u8Uri);

            _consumer.ReceivedInitialM3u8 = (content) => {
                File.WriteAllText(_m3u8OutputPath, content);
                Console.WriteLine("Received Initial m3u8.");
                Console.WriteLine(content);
            };

            _consumer.ReceivedExtinf = (extinf) => {
                Console.WriteLine($"Received Extinf");
                using (var sw = File.AppendText(_m3u8OutputPath)) {
                    sw.Write(extinf.Marker);
                    sw.Write(M3U8.LINE_END);
                    sw.Write(extinf.Path);
                    sw.Write(M3U8.LINE_END);
                }
            };

            _consumer.ReceivedSegment = (seg) => {
                Console.WriteLine($"Received segment {seg.Name}.");
                File.WriteAllBytes(Path.Combine(outputDirectory, seg.Name), seg.Content);
            };

            _consumer.Consume();
            return _m3u8OutputPath;
        }

        public void Stop()
        {
            if (_consumer != null) {
                _consumer.Stop();
                M3U8.AppendEnding(_m3u8OutputPath);
            }
        }
    }
}
