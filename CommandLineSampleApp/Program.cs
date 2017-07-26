using System;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.CommandLineUtils;
using System.Reflection;

namespace CommandLineSampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "sample";
            app.Description = "Sample app.";
            app.HelpOption("-?|-h|--help");

            app.Command(typeof(MathCommands))
                .Command(typeof(TextCommands))
                .Command(typeof(FileCommands));

            app.Execute(args);
        }
    }
}
