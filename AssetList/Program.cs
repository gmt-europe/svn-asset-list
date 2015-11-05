using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AssetList
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
                throw new InvalidOperationException("Specify directory at the command line");

            var lines = new List<Line>();

            var basePath = args[0];
            if (!basePath.EndsWith("\\"))
                basePath += "\\";

            foreach (string path in Directory.GetFiles(basePath, "*", SearchOption.AllDirectories))
            {
                string hashString;
                long length;

                using (var stream = File.OpenRead(path))
                {
                    length = stream.Length;
                    stream.Position = 0;

                    using (var sha = new SHA1Managed())
                    {
                        var hash = sha.ComputeHash(stream);
                        hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
                    }
                }

                if (!path.StartsWith(basePath))
                    throw new InvalidOperationException();

                lines.Add(new Line(
                    path.Substring(basePath.Length),
                    length,
                    new FileInfo(path).LastWriteTime,
                    hashString
                ));
            }

            lines.Sort((a, b) => String.Compare(a.Path, b.Path, StringComparison.InvariantCultureIgnoreCase));

            using (var target = new StreamWriter("out.txt"))
            {
                target.WriteLine("Path\tLength\tTime\tHash");
                foreach (var line in lines)
                {
                    target.WriteLine(
                        new StringBuilder()
                            .Append(line.Path)
                            .Append('\t')
                            .Append(line.Length)
                            .Append('\t')
                            .Append(line.Time.ToString("yyyy-MM-dd hh:mm:ss"))
                            .Append('\t')
                            .Append(line.Hash)
                            .ToString()
                    );
                }
            }
        }
    }

    public class Line
    {
        public string Hash { get; private set; }
        public long Length { get; private set; }
        public string Path { get; private set; }
        public DateTime Time { get; private set; }

        public Line(string path, long length, DateTime time, string hash)
        {
            Path = path;
            Length = length;
            Time = time;
            Hash = hash;
        }
    }
}
