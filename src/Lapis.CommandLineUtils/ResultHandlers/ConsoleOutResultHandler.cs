using System;

namespace Lapis.CommandLineUtils.ResultHandlers
{
    public class ConsoleOutResultHandler : IResultHandler
    {
        public int Handle(object value)
        {
            Console.WriteLine(value);
            return 0;
        }
    }
}