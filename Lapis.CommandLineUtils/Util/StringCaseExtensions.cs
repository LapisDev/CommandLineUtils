using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lapis.CommandLineUtils.Util
{
    internal static class StringCaseExtensions
    {
        public static string ToPascalCase(this string s)
        {
            return SplitPhrase(s).ToPascalCase();
        }

        public static string ToCamelCase(this string s)
        {
            return SplitPhrase(s).ToCamelCase();
        }

        public static string ToSnakeCase(this string s)
        {
            return SplitPhrase(s).ToSnakeCase();
        }

        public static string ToKebabCase(this string s)
        {
            return SplitPhrase(s).ToKebabCase();
        }

        public static string ToTrainCase(this string s)
        {
            return SplitPhrase(s).ToTrainCase();
        }

        private static string ToPascalCase(this IEnumerable<string> words)
        {
            return string.Join("", words.Select(s => 
            {
                if (s.Length == 2)
                    if (char.IsLower(s[0]))
                        return string.Concat(char.ToUpperInvariant(s[0]), s[1]);
                    else
                        return s;
                else if (s.Length > 2)
                    return string.Concat(char.ToUpperInvariant(s[0]), s.Substring(1).ToLowerInvariant());
                else
                    return s.ToUpperInvariant();
            }));
        }

        private static string ToCamelCase(this IEnumerable<string> words)
        {
            return string.Join("", words.Take(1).Select(s => s.ToLowerInvariant()).Concat(words.Skip(1).Select(s => 
            {
                if (s.Length == 2)
                    if (char.IsLower(s[0]))
                        return string.Concat(char.ToUpperInvariant(s[0]), s[1]);
                    else
                        return s;
                else if (s.Length > 2)
                    return string.Concat(char.ToUpperInvariant(s[0]), s.Substring(1).ToLowerInvariant());
                else
                    return s.ToUpperInvariant();
            }))
            );            
        }

        private static string ToSnakeCase(this IEnumerable<string> words)
        {
            return string.Join("_", words.Select(s => s.ToLowerInvariant()));
        }

        private static string ToKebabCase(this IEnumerable<string> words)
        {
            return string.Join("-", words.Select(s => s.ToLowerInvariant()));
        }

        private static string ToTrainCase(this IEnumerable<string> words)
        {
            return string.Join("-", words.Select(s => 
            {
                if (s.Length == 2)
                    if (char.IsLower(s[0]))
                        return string.Concat(char.ToUpperInvariant(s[0]), s[1]);
                    else
                        return s;
                else if (s.Length > 2)
                    return string.Concat(char.ToUpperInvariant(s[0]), s.Substring(1).ToLowerInvariant());
                else
                    return s.ToUpperInvariant();
            }));
        }

        private static IEnumerable<string> SplitPhrase(string s)
        {
            return Regex.Matches(s, @"(^\p{Ll}+|\p{Lu}+(?!\p{Ll})|\p{Lu}\p{Ll}+|\p{N}+|\p{Ll}+)")
                .OfType<Match>()
                .Select(m => m.Value);
        }
    }
}