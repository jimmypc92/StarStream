namespace StarStream
{
    using System;
    using System.Diagnostics;
    using System.IO;

    class ConversionUtil
    {
        public static bool ConvertM3u8ToMp4(string path)
        {
            if(string.IsNullOrEmpty(path)) {
                throw new ArgumentException(nameof(path));
            }
            if(!File.Exists(Program.Configuration.FfmpegPath)) {
                throw new Exception("Can not find ffmpeg");
            }

            path = path.Replace('\\', '/');

            Process p = new Process();

            string outputPath = null;
            if(path.Contains(".m3u8")) {
                outputPath = path.Replace("m3u8", "mp4");
            }
            else {
                outputPath = path + ".mp4";
            }

            p.StartInfo.FileName = Program.Configuration.FfmpegPath;
            p.StartInfo.Arguments = $" -i {path}  -bsf:a aac_adtstoasc -c:a copy -c:v copy {outputPath}";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;

            p.Start();

            p.WaitForExit();

            return p.ExitCode == 0;
        }
    }
}
