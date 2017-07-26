using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.CommandLineUtils.Converters;
using Lapis.CommandLineUtils.Util;
using System.Collections.Generic;

namespace Lapis.CommandLineUtils.ResultHandlers
{
    public interface IResultHandler
    {
        int Handle(object value);
    }
}