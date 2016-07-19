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
        private string _rootDir;
        private Thread _m3u8Thread;
        private Thread _tsThread;
        private bool _shouldStop;
        private M3U8 _m3u8;

        public Action<byte[]> ReceivedSegment { get; set; }
        public Action<string[]> RecievedExtinf { get; set; }

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

            using (HttpClient client = new HttpClient()) {

                while(!_shouldStop) {

                    if(this._queue.Count < 1) {
                        continue;
                    }

                    var indexPath = this._queue.Dequeue();
                    var url = tsUrl + indexPath;

                    var res = client.GetAsync(url).Result;
                    var bytes = res.Content.ReadAsByteArrayAsync().Result;

                    if(this.ReceivedSegment != null) {
                        this.ReceivedSegment(bytes);
                    }
                }
            }
        }

        private void _ConsumeM3u8(object threadStartParam)
        {
            ExtinfIndex extIndex;
            int currentIndex = -1;

            using (HttpClient client = new HttpClient()) {

                while (!_shouldStop) {
                    string content = client.GetAsync(this._m3u8Uri).Result.Content.ReadAsStringAsync().Result;

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
}
