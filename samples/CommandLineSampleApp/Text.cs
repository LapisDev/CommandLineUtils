using System;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.CommandLineUtils;
using Lapis.CommandLineUtils.ResultHandlers;
using System.Text.RegularExpressions;
using Lapis.CommandLineUtils.Converters;

namespace CommandLineSampleApp
{
    public class TextCommands
    {
        [Command]
        public bool Equal([Argument] string a, [Argument] string b, [Option] bool ignoreCase = false)
        {
            return string.Equals(a, b, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        }

        [Command]
        public int Length([Argument] string s = "")
        {
            return s.Length;
        }

        [Command]
        [Converter(typeof(StringRegexConverter))]
        public string Match(Regex pattern, string s)
        {
            return pattern.Match(s).Value;
        }
    }

    public class StringRegexConverter : IConverter
    {
        public bool CanConvert(Type sourceType, Type targetType)
        {
            return targetType == typeof(Regex) && sourceType == typeof(string);
        }

        public object Convert(object value, Type targetType)
        {
            var s = value as string;
            if (s == null)
                throw new InvalidCastException();
            return new Regex(s);
        }
    }
}