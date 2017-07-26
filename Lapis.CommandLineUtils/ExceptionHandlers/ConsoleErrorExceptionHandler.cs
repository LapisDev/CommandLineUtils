using System;

namespace Lapis.CommandLineUtils.ExceptionHandlers
{
    public class ConsoleErrorExceptionHandler : IExceptionHandler
    {
        public int Handle(Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 0;
        }
    }
}