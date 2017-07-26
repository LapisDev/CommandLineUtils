using System;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.CommandLineUtils;
using Lapis.CommandLineUtils.ResultHandlers;
using Lapis.CommandLineUtils.Converters;
using System.IO;
using System.Text;

namespace CommandLineSampleApp
{
    [Converter(typeof(PathFileConverter))]
    public class FileCommands
    {
        public string Head(FileInfo file, int n = 5)
        {
            using (var reader = file.OpenText())
            {
                var sb = new StringBuilder();
                for (var i = 0; i < n && !reader.EndOfStream; i++)
                    sb.AppendLine(reader.ReadLine());
                return sb.ToString();
            }
        }

        [return: ResultHandler(typeof(FileWritingResultHandler))]
        public string Base64(FileInfo file)
        {
            using (var reader = file.OpenRead())
            {
                var bytes = new byte[file.Length];
                reader.Read(bytes, 0, bytes.Length);
                return Base64(bytes);
            }
        }

        [NonCommand]
        public string Base64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }
    }

    public class PathFileConverter : IConverter
    {
        public bool CanConvert(Type sourceType, Type targetType)
        {
            return (targetType.IsAssignableFrom(typeof(FileInfo)) ||
                targetType.IsAssignableFrom(typeof(FileStream))) &&
                sourceType == typeof(string);
        }

        public object Convert(object value, Type targetType)
        {
            var s = value as string;
            if (s == null)
                throw new InvalidCastException();
            if (targetType.IsAssignableFrom(typeof(FileInfo)))
                return new FileInfo(s);
            if (targetType.IsAssignableFrom(typeof(FileStream)))
                return File.Open(s, FileMode.OpenOrCreate);
            throw new InvalidCastException();
        }
    }

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