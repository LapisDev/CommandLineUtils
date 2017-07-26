using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.CommandLineUtils.Converters;
using Lapis.CommandLineUtils.Util;
using System.Collections.Generic;

namespace Lapis.CommandLineUtils.Models
{
    public class CommandModel
    {
        public CommandModel(CommandModel parent = null)
        {
            Parent = parent;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool AllowArgumentSeparator { get; set; }

        public string ExtendedHelpText { get; set; }

        public bool ShowInHelpText { get; set; } = true;

        public MethodInfo Method { get; set; }

        public CommandModel Parent
        {
            get => _parent;
            set
            {
                _parent?._commands.Remove(this);
                _parent = value;
                _parent?._commands.Add(this);
            }
        }

        private CommandModel _parent;

        public IReadOnlyList<CommandModel> Commands => _commands;

        private List<CommandModel> _commands = new List<CommandModel>();

        public CommandModel Command(CommandModel subCommand)
        {
            subCommand.Parent = this;
            return this;
        }

        public IReadOnlyList<ArgumentModel> Arguments => _arguments;

        private List<ArgumentModel> _arguments = new List<ArgumentModel>();

        public CommandModel Argument(ArgumentModel argument)
        {
            argument.Command = this;
            return this;
        }

        public IReadOnlyList<OptionModel> Options => _options;

        private List<OptionModel> _options = new List<OptionModel>();

        public CommandModel Option(OptionModel option)
        {
            option.Command = this;
            return this;
        }

        public List<Type> Converters { get; set; }

        public List<Type> ResultHandlers { get; set; }

        public List<Type> ExceptionHandlers { get; set; }

        public HelpOptionModel HelpOption
        {
            get => _helpOption;
            set => value.Command = this;
        }

        private HelpOptionModel _helpOption;

        public VersionOptionModel VersionOption
        {
            get => _versionOption;
            set => value.Command = this;
        }

        private VersionOptionModel _versionOption;


        public class ArgumentModel
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public bool? MultipleValues { get; set; }

            public bool ShowInHelpText { get; set; } = true;

            public ParameterInfo Parameter { get; set; }

            public CommandModel Command
            {
                get => _command;
                set
                {
                    _command?._arguments.Remove(this);
                    _command = value;
                    _command?._arguments.Add(this);
                }
            }

            private CommandModel _command;

            public List<Type> Converters { get; set; }
        }

        public class OptionModel
        {
            public string Template { get; set; }

            public string Description { get; set; }

            public CommandOptionType? OptionType { get; set; }

            public bool ShowInHelpText { get; set; } = true;

            public ParameterInfo Parameter { get; set; }

            public CommandModel Command
            {
                get => _command;
                set
                {
                    _command?._options.Remove(this);
                    _command = value;
                    _command?._options.Add(this);
                }
            }

            private CommandModel _command;

            public List<Type> Converters { get; set; }
        }

        public class HelpOptionModel
        {
            public string Template { get; set; }

            public CommandModel Command
            {
                get => _command;
                set
                {
                    if (_command != null)
                        _command._helpOption = null;
                    _command = value;
                    if (_command != null)
                        _command._helpOption = this;
                }
            }

            private CommandModel _command;
        }

        public class VersionOptionModel
        {
            public string Template { get; set; }

            public string ShortFormVersion { get; set; }

            public string LongFormVersion { get; set; }

            public CommandModel Command
            {
                get => _command;
                set
                {
                    if (_command != null)
                        _command._versionOption = null;
                    _command = value;
                    if (_command != null)
                        _command._versionOption = this;
                }
            }

            private CommandModel _command;
        }
    }
}