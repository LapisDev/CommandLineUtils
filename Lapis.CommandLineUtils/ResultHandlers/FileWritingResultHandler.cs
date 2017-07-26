using System;
using System.IO;

namespace Lapis.CommandLineUtils.ResultHandlers
{
    public class FileWritingResultHandler : IResultHandler
    {
        public FileWritingResultHandler() { }

        public FileWritingResultHandler(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public int Handle(object value)
        {
            using (var writer = File.CreateText(Path ?? System.IO.Path.GetRandomFileName()))
                writer.WriteLine(value);
            return 0;
        }
    }
}