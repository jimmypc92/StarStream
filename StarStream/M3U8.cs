namespace StarStream
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class M3U8
    {
        public const string FILE_MARKER = "#EXTM3U";
        public const string VERSION_MARKER = "#EXT-X-VERSION:";
        public const string TARGET_DURATION_MARKER = "#EXT-X-TARGETDURATION:";
        public const string MEDIA_SEQUENCE_MARKER = "#EXT-X-MEDIA-SEQUENCE:";
        public const string EXTINF_MARKER = "#EXTINF:";
        public const string END_MARKER = "#EXT-X-ENDLIST";
        public const char LINE_END = '\x0a';


        public int Version { get; set; }
        public int MediaSequence { get; set; }
        public int TargetDuration { get; set; }
        public List<Extinf> Extinfs { get; set; }


        public M3U8(string content)
        {
            this.Extinfs = new List<Extinf>();

            string[] lines = content.Split('\n');

            if(lines.Length == 0 || lines[0] != FILE_MARKER) {
                throw new Exception("Invalid M3U8 file. Missing file identifier.");
            }

            for(int i = 0; i < lines.Length; i++) {
                if(lines[i].StartsWith(FILE_MARKER)) {
                }
                else if(lines[i].StartsWith(VERSION_MARKER)) {
                    this.Version = int.Parse(lines[i].Substring(VERSION_MARKER.Length, lines[i].Length - VERSION_MARKER.Length));
                }
                else if(lines[i].StartsWith(TARGET_DURATION_MARKER)) {
                    this.TargetDuration = int.Parse(lines[i].Substring(TARGET_DURATION_MARKER.Length, lines[i].Length - TARGET_DURATION_MARKER.Length));
                }
                else if(lines[i].StartsWith(MEDIA_SEQUENCE_MARKER)) {
                    this.MediaSequence = int.Parse(lines[i].Substring(MEDIA_SEQUENCE_MARKER.Length, lines[i].Length - MEDIA_SEQUENCE_MARKER.Length));
                }
                else if(lines[i].StartsWith(EXTINF_MARKER)) {
                    this.Extinfs.Add(new Extinf(lines[i], lines[i + 1]));
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(FILE_MARKER);
            sb.Append(LINE_END);
            sb.Append($"{VERSION_MARKER}{this.Version}");
            sb.Append(LINE_END);
            sb.Append($"{MEDIA_SEQUENCE_MARKER}{this.MediaSequence}");
            sb.Append(LINE_END);
            sb.Append($"{TARGET_DURATION_MARKER}{this.TargetDuration}");
            sb.Append(LINE_END);

            foreach(var extinf in this.Extinfs) {
                sb.Append(extinf.Marker);
                sb.Append(extinf.Path);
            }

            sb.Append(END_MARKER);
            sb.Append(LINE_END);

            return sb.ToString();
        }

        public static void AppendEnding(string path)
        {
            File.AppendAllText(path, END_MARKER + LINE_END);
        }
    }

    public class Extinf
    {
        public string Marker { get; set; }
        public string Path { get; set; }
        public int Index { get; set; }

        public Extinf(string marker, string path)
        {
            if(!IsExtinfPath(path)) {
                throw new Exception("Invalid path, no index found.");
            }

            this.Index = int.Parse(path.Substring(6, 10));
            this.Marker = marker;
            this.Path = path;
        }

        public static bool IsExtinfPath(string path)
        {
            return path.StartsWith("index", StringComparison.OrdinalIgnoreCase);
        }
    }
}