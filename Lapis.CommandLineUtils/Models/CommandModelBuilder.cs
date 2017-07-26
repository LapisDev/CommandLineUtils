using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.CommandLineUtils.Converters;
using Lapis.CommandLineUtils.Util;
using System.Collections.Generic;

namespace Lapis.CommandLineUtils.Models
{
    public interface ICommandModelBuilder
    {
        CommandModel BuildCommand(TypeInfo typeInfo);

        CommandModel BuildCommand(MethodInfo methodInfo);
    }

    public class CommandModelBuilder : ICommandModelBuilder
    {
        public CommandModel BuildCommand(TypeInfo typeInfo)
        {
            var commandModel = CreateCommandModel(typeInfo);
            if (commandModel == null)
                return null;

            foreach (var methodInfo in typeInfo.GetMethods())
            {
                var subCommandModel = BuildCommand(methodInfo);
                if (subCommandModel != null)
                    commandModel.Command(subCommandModel);
            }
            return commandModel;
        }

        public CommandModel BuildCommand(MethodInfo methodInfo)
        {
            var commandModel = CreateCommandModel(methodInfo);
            if (commandModel == null)
                return null;

            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                var argumentModel = CreateArgumentModel(parameterInfo);
                if (argumentModel != null)
                {
                    commandModel.Argument(argumentModel);
                    continue;
                }
                var optionModel = CreateOptionModel(parameterInfo);
                if (optionModel != null)
                {
                    commandModel.Option(optionModel);
                    continue;
                }                
                return null;
            }
            return commandModel;
        }

        protected virtual CommandModel CreateCommandModel(TypeInfo typeInfo)
        {
            if (typeInfo == null)
                throw new ArgumentNullException(nameof(typeInfo));
            if (!IsCommand(typeInfo))
                return null;

            var commandModel = new CommandModel();
            {
                var attribute = typeInfo.GetCustomAttribute<CommandAttribute>(inherit: true);
                commandModel.Name = attribute?.Name ?? GetCommandName(typeInfo.Name);
                commandModel.Description = attribute?.Description ?? typeInfo.ToString();
                commandModel.AllowArgumentSeparator = attribute?.AllowArgumentSeparator ?? false;
                commandModel.ExtendedHelpText = attribute?.ExtendedHelpText;
                commandModel.ShowInHelpText = attribute?.ShowInHelpText ?? true;
            }

            foreach (var attribute in typeInfo.GetCustomAttributes<ConverterAttribute>(inherit: true))
            {
                if (commandModel.Converters == null)
                    commandModel.Converters = new List<Type>();
                commandModel.Converters.Add(attribute.Type);
            }

            foreach (var attribute in typeInfo.GetCustomAttributes<ResultHandlerAttribute>(inherit: true))
            {
                if (commandModel.ResultHandlers == null)
                    commandModel.ResultHandlers = new List<Type>();
                commandModel.ResultHandlers.Add(attribute.Type);
            }

            foreach (var attribute in typeInfo.GetCustomAttributes<ExceptionHandlerAttribute>(inherit: true))
            {
                if (commandModel.ExceptionHandlers == null)
                    commandModel.ExceptionHandlers = new List<Type>();
                commandModel.ExceptionHandlers.Add(attribute.Type);
            }

            {
                var attribute = typeInfo.GetCustomAttribute<HelpOptionAttribute>();
                if (attribute != null)
                    commandModel.HelpOption = new CommandModel.HelpOptionModel() { Template = attribute.Template };
                else if (typeInfo.GetCustomAttribute<NoHelpOptionAttribute>() == null)
                {
                    attribute = typeInfo.GetCustomAttribute<HelpOptionAttribute>(inherit: true);
                    if (attribute != null)
                        commandModel.HelpOption = new CommandModel.HelpOptionModel() { Template = attribute.Template };
                    else if (typeInfo.GetCustomAttribute<NoHelpOptionAttribute>(inherit: true) == null)
                        commandModel.HelpOption = new CommandModel.HelpOptionModel() { Template = GetDefaultHelpOptionTemplate() };
                }
            }

            {
                var attribute = typeInfo.GetCustomAttribute<VersionOptionAttribute>(inherit: true);
                if (attribute != null)
                    commandModel.VersionOption = new CommandModel.VersionOptionModel()
                    {
                        Template = attribute.Template,
                        ShortFormVersion = attribute.ShortFormVersion,
                        LongFormVersion = attribute.LongFormVersion
                    };
            }

            return commandModel;
        }

        protected virtual CommandModel CreateCommandModel(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            if (!IsCommand(methodInfo))
                return null;

            var commandModel = new CommandModel() { Method = methodInfo };
            {
                var attribute = methodInfo.GetCustomAttribute<CommandAttribute>(inherit: true);
                commandModel.Name = attribute?.Name ?? GetCommandName(methodInfo.Name);
                commandModel.Description = attribute?.Description ?? methodInfo.ToString();
                commandModel.AllowArgumentSeparator = attribute?.AllowArgumentSeparator ?? false;
                commandModel.ExtendedHelpText = attribute?.ExtendedHelpText;
                commandModel.ShowInHelpText = attribute?.ShowInHelpText ?? true;
            }

            foreach (var attribute in methodInfo.GetCustomAttributes<ConverterAttribute>(inherit: true))
            {
                if (commandModel.Converters == null)
                    commandModel.Converters = new List<Type>();
                commandModel.Converters.Add(attribute.Type);
            }

            foreach (var attribute in methodInfo.ReturnParameter.GetCustomAttributes<ResultHandlerAttribute>(inherit: true))
            {
                if (commandModel.ResultHandlers == null)
                    commandModel.ResultHandlers = new List<Type>();
                commandModel.ResultHandlers.Add(attribute.Type);
            }

            {
                var attribute = methodInfo.GetCustomAttribute<HelpOptionAttribute>();
                if (attribute != null)
                    commandModel.HelpOption = new CommandModel.HelpOptionModel() { Template = attribute.Template };
                else if (methodInfo.GetCustomAttribute<NoHelpOptionAttribute>() == null)
                {
                    attribute = methodInfo.GetCustomAttribute<HelpOptionAttribute>(inherit: true);
                    if (attribute != null)
                        commandModel.HelpOption = new CommandModel.HelpOptionModel() { Template = attribute.Template };
                    else if (methodInfo.GetCustomAttribute<NoHelpOptionAttribute>(inherit: true) == null)
                        commandModel.HelpOption = new CommandModel.HelpOptionModel() { Template = GetDefaultHelpOptionTemplate() };
                }
            }

            {
                var attribute = methodInfo.GetCustomAttribute<VersionOptionAttribute>(inherit: true);
                if (attribute != null)
                    commandModel.VersionOption = new CommandModel.VersionOptionModel()
                    {
                        Template = attribute.Template,
                        ShortFormVersion = attribute.ShortFormVersion,
                        LongFormVersion = attribute.LongFormVersion
                    };
            }

            return commandModel;
        }

        protected virtual CommandModel.ArgumentModel CreateArgumentModel(ParameterInfo parameterInfo)
        {
            if (parameterInfo == null)
                throw new ArgumentNullException(nameof(parameterInfo));
            if (!IsArgument(parameterInfo))
                return null;

            var argumentModel = new CommandModel.ArgumentModel() { Parameter = parameterInfo };
            {
                var attribute = parameterInfo.GetCustomAttribute<ArgumentAttribute>(inherit: true);
                argumentModel.Name = attribute?.Name ?? GetArgumentName(parameterInfo.Name);
                argumentModel.Description = attribute?.Description ?? parameterInfo.ToString();
                argumentModel.MultipleValues = attribute?.MultipleValues;
                argumentModel.ShowInHelpText = attribute?.ShowInHelpText ?? true;
            }

            foreach (var attribute in parameterInfo.GetCustomAttributes<ConverterAttribute>(inherit: true))
            {
                if (argumentModel.Converters == null)
                    argumentModel.Converters = new List<Type>();
                argumentModel.Converters.Add(attribute.Type);
            }

            return argumentModel;
        }

        protected virtual CommandModel.OptionModel CreateOptionModel(ParameterInfo parameterInfo)
        {
            if (parameterInfo == null)
                throw new ArgumentNullException(nameof(parameterInfo));
            if (!IsOption(parameterInfo))
                return null;

            var optionModel = new CommandModel.OptionModel() { Parameter = parameterInfo };
            {
                var attribute = parameterInfo.GetCustomAttribute<OptionAttribute>(inherit: true);
                optionModel.Template = attribute?.Template ?? GetOptionTemplate(parameterInfo.Name);
                optionModel.Description = attribute?.Description ?? parameterInfo.ToString();
                optionModel.OptionType = attribute?.OptionType;
                optionModel.ShowInHelpText = attribute?.ShowInHelpText ?? true;

                if (optionModel.OptionType == null)
                    if (parameterInfo.ParameterType.IsAssignableFrom(typeof(bool)))
                        optionModel.OptionType = CommandOptionType.NoValue;

                if (optionModel.Template == null)
                    if (optionModel.OptionType == CommandOptionType.NoValue)
                        optionModel.Template = GetOptionTemplate(parameterInfo.Name, false);
                    else
                        optionModel.Template = GetOptionTemplate(parameterInfo.Name, true);
            }

            foreach (var attribute in parameterInfo.GetCustomAttributes<ConverterAttribute>(inherit: true))
            {
                if (optionModel.Converters == null)
                    optionModel.Converters = new List<Type>();
                optionModel.Converters.Add(attribute.Type);
            }

            return optionModel;
        }

        protected virtual bool IsCommand(TypeInfo typeInfo)
        {
            if (typeInfo == null)
                throw new ArgumentNullException(nameof(typeInfo));

            if (!typeInfo.IsClass)
                return false;
            if (typeInfo.IsAbstract && !typeInfo.IsSealed)
                return false;
            if (!typeInfo.IsPublic)
                return false;
            if (typeInfo.ContainsGenericParameters)
                return false;
            if (typeInfo.IsDefined(typeof(NonCommandAttribute)))
                return false;
            if (typeInfo.IsDefined(typeof(CommandAttribute)))
                return true;
            return true;
        }

        protected virtual bool IsCommand(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            if (methodInfo.IsSpecialName)
                return false;
            if (methodInfo.IsDefined(typeof(NonCommandAttribute)))
                return false;
            if (methodInfo.GetBaseDefinition().DeclaringType == typeof(object))
                return false;
            if (IsIDisposableMethod(methodInfo))
                return false;
            if (methodInfo.IsAbstract)
                return false;
            if (methodInfo.IsConstructor)
                return false;
            if (methodInfo.IsGenericMethod)
                return false;
            if (!methodInfo.IsPublic)
                return false;
            if (methodInfo.IsDefined(typeof(NonCommandAttribute)))
                return false;
            if (methodInfo.IsDefined(typeof(CommandAttribute)))
                return true;
            return true;
        }

        protected virtual bool IsArgument(ParameterInfo parameterInfo)
        {
            if (parameterInfo == null)
                throw new ArgumentNullException(nameof(parameterInfo));

            if (parameterInfo.IsOut || parameterInfo.IsRetval)
                return false;
            if (parameterInfo.IsDefined(typeof(ArgumentAttribute)))
                return true;
            if (parameterInfo.IsDefined(typeof(OptionAttribute)))
                return false;
            if (parameterInfo.ParameterType == typeof(bool))
                return false;
            if (parameterInfo.HasDefaultValue || parameterInfo.IsOptional)
                return false;
            return true;
        }

        protected virtual bool IsOption(ParameterInfo parameterInfo)
        {
            if (parameterInfo == null)
                throw new ArgumentNullException(nameof(parameterInfo));

            if (parameterInfo.IsOut || parameterInfo.IsRetval)
                return false;
            if (parameterInfo.IsDefined(typeof(OptionAttribute)))
                return true;
            if (parameterInfo.IsDefined(typeof(ArgumentAttribute)))
                return false;
            if (parameterInfo.ParameterType == typeof(bool))
                return true;
            if (parameterInfo.IsOptional)
                return true;
            return true;
        }

        protected virtual string GetCommandName(string name)
        {
            var s = name.ToKebabCase();
            if (s.EndsWith("command", StringComparison.OrdinalIgnoreCase) &&
                s.Length > "command".Length)
                s = s.Substring(0, s.Length - "command".Length);
            else if (s.EndsWith("commands", StringComparison.OrdinalIgnoreCase) &&
                s.Length > "commands".Length)
                s = s.Substring(0, s.Length - "commands".Length);            
            return s.Trim('-');
        }

        protected virtual string GetArgumentName(string name, bool isOptional = false)
        {
            return name.ToSnakeCase();
        }

        protected virtual string GetOptionTemplate(string name, bool hasValue = false)
        {
            var s = name.ToKebabCase();
            if (hasValue)
            {
                if (s.Length == 1)
                    return $"-{s} <{name.ToSnakeCase()}>";
                else
                    return $"--{s} <{name.ToSnakeCase()}>";
            }
            else
            {
                if (s.Length == 1)
                    return $"-{s}";
                else
                    return $"--{s}";
            }
        }

        protected virtual string GetDefaultHelpOptionTemplate()
        {
            return "-?|-h|--help";
        }

        private bool IsIDisposableMethod(MethodInfo methodInfo)
        {
            var baseMethodInfo = methodInfo.GetBaseDefinition();
            var declaringTypeInfo = baseMethodInfo.DeclaringType.GetTypeInfo();
            return (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(declaringTypeInfo) &&
                 declaringTypeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0] == baseMethodInfo);
        }
    }
}