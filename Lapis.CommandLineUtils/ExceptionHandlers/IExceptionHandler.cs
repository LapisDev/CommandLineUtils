using System;

namespace Lapis.CommandLineUtils.ExceptionHandlers
{
    public interface IExceptionHandler
    {
        int Handle(Exception exception);
    }
}