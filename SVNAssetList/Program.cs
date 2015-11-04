using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SharpSvn;

namespace SVNAssetList
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
                throw new InvalidOperationException("Specify directory at the command line");

            var lines = new List<Line>();

            using (var client = new SvnClient())
            {
                Collection<SvnListEventArgs> list;
                var listArgs = new SvnListArgs { Depth = SvnDepth.Infinity };
                client.GetList(new SvnUriTarget(args[0]), listArgs, out list);

                foreach (var item in list)
                {
                    string hashString = null;
                    long length = 0;

                    if (item.Entry.NodeKind == SvnNodeKind.File)
                    {
                        using (var stream = new MemoryStream())
                        {
                            client.Write(item.Uri, stream);
                            length = stream.Length;
                            stream.Position = 0;

                            using (var sha = new SHA1Managed())
                            {
                                var hash = sha.ComputeHash(stream);
                                hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
                            }
                        }
                    }

                    lines.Add(new Line(
                        item.Path,
                        item.Entry.Revision,
                        length,
                        item.Entry.Author,
                        item.Entry.Time,
                        hashString
                    ));
                }
            }

            lines.Sort((a, b) => String.Compare(a.Path, b.Path, StringComparison.InvariantCultureIgnoreCase));

            using (var target = new StreamWriter("out.txt"))
            {
                target.WriteLine("Path\tRevision\tLength\tAuthor\tTime\tHash");
                foreach (var line in lines)
                {
                    target.WriteLine(
                        new StringBuilder()
                            .Append(line.Path)
                            .Append('\t')
                            .Append(line.Revision)
                            .Append('\t')
                            .Append(line.Length)
                            .Append('\t')
                            .Append(line.Author)
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
        public string Author { get; private set; }
        public string Hash { get; private set; }
        public long Length { get; private set; }
        public string Path { get; private set; }
        public long Revision { get; private set; }
        public DateTime Time { get; private set; }

        public Line(string path, long revision, long length, string author, DateTime time, string hash)
        {
            Path = path;
            Revision = revision;
            Length = length;
            Author = author;
            Time = time;
            Hash = hash;
        }
    }
}
