namespace StarStream
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;

    public class HlsConsumer
    {
        private Queue<string> _queue;
        private string _m3u8Uri;
        private Thread _m3u8Thread;
        private Thread _tsThread;
        private bool _shouldStop;

        public Action<string> ReceivedInitialM3u8 { get; set; }
        public Action<TsSegment> ReceivedSegment { get; set; }
        public Action<Extinf> ReceivedExtinf { get; set; }

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

        public void Stop()
        {
            this._shouldStop = true;
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
            Extinf extinf;
            int currentIndex = -1;

            using (HttpClient client = new HttpClient()) {

                string content = client.GetAsync(this._m3u8Uri).Result.Content.ReadAsStringAsync().Result;

                if(this.ReceivedInitialM3u8 != null) {
                    this.ReceivedInitialM3u8(content);

                    var m3u8 = new M3U8(content);
                    foreach(var e in m3u8.Extinfs) {
                        currentIndex = e.Index;

                        // Don't emit extinf received because it is captured in the m3u8 callback
                        this._queue.Enqueue(e.Path);
                        Console.WriteLine($"Queueing: {e.Path}");
                    }
                }

                while (!_shouldStop) {
                    content = client.GetAsync(this._m3u8Uri).Result.Content.ReadAsStringAsync().Result;

                    var lines = content.Split('\n');

                    for (int i = 0; i < lines.Length; i++) {
                        if (Extinf.IsExtinfPath(lines[i])) {
                            extinf = new Extinf(lines[i-1], lines[i]);

                            int index = extinf.Index;

                            if (index > currentIndex) {
                                currentIndex = index;
                                this._queue.Enqueue(lines[i]);
                                string line = lines[i];
                                Console.WriteLine($"Queueing: {line}");

                                if (this.ReceivedExtinf != null) {
                                    this.ReceivedExtinf(extinf);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
