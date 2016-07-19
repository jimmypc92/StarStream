namespace StarStream
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;

    class HlsConsumer
    {
        private Queue<string> _queue;
        private string _m3u8Uri;
        private Thread _m3u8Thread;
        private Thread _tsThread;
        private bool _shouldStop;

        public Action<string> ReceivedInitialM3u8 { get; set; }
        public Action<TsSegment> ReceivedSegment { get; set; }
        public Action<string[]> RecievedExtinf { get; set; }

        public bool Consuming {
            get {
                return this._m3u8Thread != null && this._m3u8Thread.IsAlive
                    || this._tsThread != null && this._tsThread.IsAlive;
            }
        }

        public HlsConsumer(string m3u8Uri)
        {
            this._queue = new Queue<string>();
            this._m3u8Uri = m3u8Uri;
        }

        public void Consume()
        {
            if(this._tsThread != null || this._m3u8Thread != null) {
                throw new InvalidOperationException("Already started consuming.");
            }

            this._tsThread = new Thread(this._ConsumeTs);
            this._tsThread.Start(null);

            this._m3u8Thread = new Thread(this._ConsumeM3u8);
            this._m3u8Thread.Start(null);
        }

        private void _ConsumeTs(object threadStartParam)
        {
            string tsUrl = _m3u8Uri.Substring(0, this._m3u8Uri.IndexOf("index-live.m3u8"));
            TsSegment currentSegment;

            using (HttpClient client = new HttpClient()) {

                while(!_shouldStop) {

                    if(this._queue.Count < 1) {
                        continue;
                    }

                    var indexPath = this._queue.Dequeue();
                    var url = tsUrl + indexPath;

                    var res = client.GetAsync(url).Result;
                    var bytes = res.Content.ReadAsByteArrayAsync().Result;

                    currentSegment = new TsSegment();
                    currentSegment.Content = bytes;
                    currentSegment.Name = indexPath;

                    if(this.ReceivedSegment != null) {
                        this.ReceivedSegment(currentSegment);
                    }
                }
            }
        }

        private void _ConsumeM3u8(object threadStartParam)
        {
            ExtinfIndex extIndex;
            int currentIndex = -1;

            using (HttpClient client = new HttpClient()) {

                string content = client.GetAsync(this._m3u8Uri).Result.Content.ReadAsStringAsync().Result;

                if(this.ReceivedInitialM3u8 != null) {
                    this.ReceivedInitialM3u8(content);
                }

                while (!_shouldStop) {
                    content = client.GetAsync(this._m3u8Uri).Result.Content.ReadAsStringAsync().Result;

                    var lines = content.Split('\n');

                    for (int i = 0; i < lines.Length; i++) {
                        if (ExtinfIndex.IsExtinfIndex(lines[i])) {
                            extIndex = new ExtinfIndex(lines[i]);

                            int index = extIndex.Index;

                            if (index > currentIndex) {
                                currentIndex = index;
                                this._queue.Enqueue(lines[i]);
                                string[] extinf = new string[2];
                                extinf[0] = lines[i - 1];
                                extinf[1] = lines[i];

                                if(this.RecievedExtinf != null) {
                                    this.RecievedExtinf(extinf);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            this._shouldStop = true;
        }
    }

    public class TsSegment
    {
        public byte[] Content { get; set; }
        public string Name { get; set; }
    }
}
