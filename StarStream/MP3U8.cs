namespace StarStream
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class M3U8
    {
        public const string FILE_MARKER = "#EXTM3U";
        public const string VERSION_MARKER = "#EXT-X-VERSION:";
        public const string TARGET_DURATION_MARKER = "#EXT-X-TARGETDURATION:";
        public const string MEDIA_SEQUENCE_MARKER = "#EXT-X-MEDIA-SEQUENCE:";
        public const string EXTINF_MARKER = "#EXTINF:";
        public const string END_MARKER = "#EXT-X-ENDLIST";


        public int Version { get; set; }
        public int MediaSequence { get; set; }
        public int TargetDuration { get; set; }
        public List<string[]> Extinfs { get; set; }


        public M3U8(string content)
        {
            this.Extinfs = new List<string[]>();

            string[] lines = content.Split('\n');

            if(lines.Length == 0 || lines[0] != FILE_MARKER) {
                throw new Exception("Invalid M3U8 file. Missing file identifier.");
            }

            for(int i = 0; i < lines.Length; i++) {

                switch(lines[i]) {
                    case FILE_MARKER:
                        break;
                    case VERSION_MARKER:
                        this.Version = int.Parse(lines[i].Substring(VERSION_MARKER.Length, lines.Length - VERSION_MARKER.Length));
                        break;
                    case TARGET_DURATION_MARKER:
                        this.TargetDuration = int.Parse(lines[i].Substring(TARGET_DURATION_MARKER.Length, lines.Length - TARGET_DURATION_MARKER.Length));
                        break;
                    case MEDIA_SEQUENCE_MARKER:
                        this.MediaSequence = int.Parse(lines[i].Substring(MEDIA_SEQUENCE_MARKER.Length, lines.Length - MEDIA_SEQUENCE_MARKER.Length));
                        break;
                    case EXTINF_MARKER:
                        string[] pair = new string[2];
                        pair[0] = lines[i];
                        pair[1] = lines[i + 1];
                        this.Extinfs.Add(pair);
                        break;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string cr = "\r";
            sb.Append(FILE_MARKER);
            sb.Append(cr);
            sb.Append($"{VERSION_MARKER}{this.Version}");
            sb.Append(cr);
            sb.Append($"{MEDIA_SEQUENCE_MARKER}{this.MediaSequence}");
            sb.Append(cr);
            sb.Append($"{TARGET_DURATION_MARKER}{this.TargetDuration}");
            sb.Append(cr);

            foreach(var pair in this.Extinfs) {
                sb.Append(pair[0]);
                sb.Append(cr);
                sb.Append(pair[1]);
                sb.Append(cr);
            }

            sb.Append(END_MARKER);
            sb.Append(cr);

            return sb.ToString();
        }
    }

    public class ExtinfIndex
    {
        public int Index { get; set; }

        public ExtinfIndex(string line)
        {
            this.Index = int.Parse(line.Substring(6, 10));
        }

        public static bool IsExtinfIndex(string line)
        {
            return line.StartsWith("index", StringComparison.OrdinalIgnoreCase);
        }
    }
}