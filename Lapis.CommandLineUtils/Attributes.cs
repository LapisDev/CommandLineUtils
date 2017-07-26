using System;
using Microsoft.Extensions.CommandLineUtils;

namespace Lapis.CommandLineUtils
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute() { }

        public CommandAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool AllowArgumentSeparator { get; set; }

        public string ExtendedHelpText { get; set; }

        public bool ShowInHelpText { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class NonCommandAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ArgumentAttribute : Attribute
    {
        public ArgumentAttribute() { }

        public ArgumentAttribute(string name, string description, bool multipleValues = false)
        {
            Name = name;
            Description = description;
            MultipleValues = multipleValues;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool? MultipleValues { get; set; }

        public bool ShowInHelpText { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class OptionAttribute : Attribute
    {
        public OptionAttribute() { }

        public OptionAttribute(string template, string description, CommandOptionType optionType)
        {
            Template = template;
            Description = description;
            OptionType = optionType;
        }

        public string Template { get; set; }

        public string Description { get; set; }

        public CommandOptionType? OptionType { get; set; }

        public bool ShowInHelpText { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HelpOptionAttribute : Attribute
    {
        public HelpOptionAttribute() { }

        public HelpOptionAttribute(string template)
        {
            Template = template;
        }

        public string Template { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class NoHelpOptionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class VersionOptionAttribute : Attribute
    {
        public VersionOptionAttribute() { }

        public string Template { get; set; }

        public string ShortFormVersion { get; set; }

        public string LongFormVersion { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
    public class ConverterAttribute : Attribute
    {
        public ConverterAttribute(Type type)
        { 
            Type = type;
        }

        public Type Type { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.ReturnValue, AllowMultiple = true)]
    public class ResultHandlerAttribute : Attribute
    {
        public ResultHandlerAttribute(Type type)
        { 
            Type = type;
        }

        public Type Type { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ExceptionHandlerAttribute : Attribute
    {
        public ExceptionHandlerAttribute(Type type)
        { 
            Type = type;
        }

        public Type Type { get; }
    }
}