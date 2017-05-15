namespace StarStream
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public class Ffmpeg
    {
        private string _path;

        public Ffmpeg(string ffmpegPath)
        {
            if (!File.Exists(ffmpegPath))
            {
                throw new Exception("Can not find ffmpeg");
            }

            this._path = ffmpegPath;
        }

        public bool ConvertM3u8ToMp4(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(nameof(path));
            }

            path = path.Replace('\\', '/');

            Process p = new Process();

            string outputPath = null;
            if (path.Contains(".m3u8"))
            {
                outputPath = path.Replace("m3u8", "mp4");
            }
            else
            {
                outputPath = path + ".mp4";
            }

            p.StartInfo.FileName = _path;
            p.StartInfo.Arguments = $" -i {path}  -bsf:a aac_adtstoasc -c:a copy -c:v copy {outputPath}";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;

            p.Start();

            p.WaitForExit();

            return p.ExitCode == 0;
        }
    }
}
