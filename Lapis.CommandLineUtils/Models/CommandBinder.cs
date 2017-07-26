using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Lapis.CommandLineUtils.Converters;
using Lapis.CommandLineUtils.Util;
using System.Collections.Generic;
using Lapis.CommandLineUtils.ResultHandlers;
using Lapis.CommandLineUtils.ExceptionHandlers;

namespace Lapis.CommandLineUtils.Models
{
    public interface ICommandBinder
    {
        void BindCommand(CommandLineApplication app, CommandModel commandModel);
    }

    public class CommandBinder : ICommandBinder
    {
        public CommandBinder(IServiceProvider serviceProvider,
            IReadOnlyList<Type> globalConverters,
            IReadOnlyList<Type> globalResultHandlers,
            IReadOnlyList<Type> globalExceptionHandlers)
        {
            ServiceProvider = serviceProvider;
            GlobalConverters = globalConverters;
            GlobalResultHandlers = globalResultHandlers;
            GlobalExceptionHandlers = globalExceptionHandlers;
        }

        public IServiceProvider ServiceProvider { get; }

        public IReadOnlyList<Type> GlobalConverters { get; }

        public IReadOnlyList<Type> GlobalResultHandlers { get; }

        public IReadOnlyList<Type> GlobalExceptionHandlers { get; }

        public virtual void BindCommand(CommandLineApplication app, CommandModel commandModel)
        {
            if (commandModel == null)
                throw new ArgumentNullException(nameof(commandModel));

            app.Command(commandModel.Name, command =>
            {
                command.Description = commandModel.Description;
                command.ShowInHelpText = commandModel.ShowInHelpText;
                command.AllowArgumentSeparator = commandModel.AllowArgumentSeparator;
                command.ExtendedHelpText = commandModel.ExtendedHelpText;

                if (commandModel.HelpOption != null)
                    command.HelpOption(commandModel.HelpOption.Template);
                if (commandModel.VersionOption != null)
                    command.VersionOption(
                        commandModel.VersionOption.Template,
                        commandModel.VersionOption.ShortFormVersion,
                        commandModel.VersionOption.LongFormVersion
                    );

                if (commandModel.Method != null)
                {
                    var parameterInfo = commandModel.Method.GetParameters().ToList();
                    var parameterFunc = parameterInfo.Select(p => (Func<object>)null).ToList();
                    if (commandModel.Arguments != null)
                        foreach (var arg in commandModel.Arguments)
                        {
                            var index = parameterInfo.IndexOf(arg.Parameter);
                            if (index < 0)
                                throw new InvalidOperationException($"Method {commandModel.Method.Name} doesn't have parameter {arg.Parameter.Name}.");
                            if (parameterFunc[index] != null)
                                throw new InvalidOperationException($"Parameter {arg.Parameter.Name} alreadly exists.");

                            parameterFunc[index] = BindArgument(command, arg);
                        };
                    if (commandModel.Options != null)
                        foreach (var opt in commandModel.Options)
                        {
                            var index = parameterInfo.IndexOf(opt.Parameter);
                            if (index < 0)
                                throw new InvalidOperationException($"Method {commandModel.Method.Name} doesn't have parameter {opt.Parameter.Name}.");
                            if (parameterFunc[index] != null)
                                throw new InvalidOperationException($"Parameter {opt.Parameter.Name} alreadly exists.");

                            parameterFunc[index] = BindOption(command, opt);
                        };
                    {
                        var index = parameterFunc.IndexOf(null);
                        if (index >= 0)
                            throw new InvalidOperationException($"Parameter {parameterInfo[index].Name} cannot be bound to any argument or option.");
                    }

                    var methodInfo = commandModel.Method;
                    var parameters = parameterFunc.Select(func => func.Invoke());
                    var resultHandler = SelectResultHandler(EnumerateResultHandlerTypes(commandModel));
                    var exceptionHandler = SelectExceptionHandler(EnumerateExceptionHandlerTypes(commandModel));
                    if (methodInfo.IsStatic)
                        command.OnExecute(() =>
                        {
                            int returnValue = 0;
                            try
                            {
                                var result = methodInfo.Invoke(null, parameters.ToArray());
                                returnValue = resultHandler?.Invoke(result) ?? 0;
                            }
                            catch (Exception ex)
                            {
                                returnValue = exceptionHandler?.Invoke(ex) ?? ex.HResult;
                            }
                            return returnValue;
                        });
                    else
                    {
                        var instanceType = methodInfo.DeclaringType;
                        command.OnExecute(() =>
                        {
                            int returnValue = 0;
                            try
                            {
                                var instance = ServiceProvider.GetService(instanceType);
                                if (instance == null)
                                {
                                    var constructor = instanceType
                                        .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                        .Where(ctor => !ctor.IsAbstract)
                                        .FirstOrDefault(ctor => ctor.GetParameters().Length == 0);
                                    instance = constructor?.Invoke(null);
                                }
                                var result = methodInfo.Invoke(instance, parameters.ToArray());
                                returnValue = resultHandler?.Invoke(result) ?? 0;
                            }
                            catch (Exception ex)
                            {
                                returnValue = exceptionHandler?.Invoke(ex) ?? ex.HResult;
                            }
                            return returnValue;
                        });
                    }
                }

                if (commandModel.Commands != null)
                {
                    foreach (var cmd in commandModel.Commands)
                        BindCommand(command, cmd);
                }
            });

        }

        protected virtual Func<object> BindArgument(CommandLineApplication command, CommandModel.ArgumentModel argumentModel)
        {
            if (argumentModel == null)
                throw new ArgumentNullException(nameof(argumentModel));
            if (argumentModel.Parameter == null)
                throw new ArgumentNullException(nameof(argumentModel.Parameter));

            if (argumentModel.MultipleValues != true)
            {
                var converter = SelectConverter(
                    EnumerateConverterTypes(argumentModel),
                    typeof(string), argumentModel.Parameter.ParameterType
                );
                if (converter != null)
                {
                    var argument = command.Argument(argumentModel.Name, argumentModel.Description,
                        arg => arg.ShowInHelpText = argumentModel.ShowInHelpText,
                        multipleValues: false);
                    if (argumentModel.Parameter.HasDefaultValue)
                    {
                        var defaultValue = argumentModel.Parameter.RawDefaultValue;
                        return () => argument.Value != null ? converter.Invoke(argument.Value) : defaultValue;
                    }
                    else
                        return () => converter.Invoke(argument.Value);
                }
            }
            if (argumentModel.MultipleValues != false)
            {
                var converter = SelectConverter(
                    EnumerateConverterTypes(argumentModel),
                    typeof(List<string>), argumentModel.Parameter.ParameterType
                );
                if (converter != null)
                {
                    var argument = command.Argument(argumentModel.Name, argumentModel.Description,
                        arg => arg.ShowInHelpText = argumentModel.ShowInHelpText,
                        multipleValues: true);
                    return () => converter.Invoke(argument.Values);
                }
            }
            if (argumentModel.MultipleValues != false)
            {
                var converter = SelectConverter(
                    EnumerateConverterTypes(argumentModel),
                    typeof(string[]), argumentModel.Parameter.ParameterType
                );
                if (converter != null)
                {
                    var argument = command.Argument(argumentModel.Name, argumentModel.Description,
                        arg => arg.ShowInHelpText = argumentModel.ShowInHelpText,
                        multipleValues: true);
                    return () => converter.Invoke(argument.Values.ToArray());
                }
            }
            if (argumentModel.MultipleValues != true)
            {
                if (argumentModel.Parameter.ParameterType.IsAssignableFrom(typeof(string)))
                {
                    var argument = command.Argument(argumentModel.Name, argumentModel.Description,
                        arg => arg.ShowInHelpText = argumentModel.ShowInHelpText,
                        multipleValues: false);
                    if (argumentModel.Parameter.HasDefaultValue)
                    {
                        var defaultValue = argumentModel.Parameter.RawDefaultValue;
                        return () => argument.Value ?? defaultValue;
                    }
                    else
                        return () => argument.Value;
                }
            }
            if (argumentModel.MultipleValues != false)
            {
                if (argumentModel.Parameter.ParameterType.IsAssignableFrom(typeof(List<string>)))
                {
                    var argument = command.Argument(argumentModel.Name, argumentModel.Description,
                        arg => arg.ShowInHelpText = argumentModel.ShowInHelpText,
                        multipleValues: true);
                    return () => argument.Values;
                }
                if (argumentModel.Parameter.ParameterType.IsAssignableFrom(typeof(string[])))
                {
                    var argument = command.Argument(argumentModel.Name, argumentModel.Description,
                        arg => arg.ShowInHelpText = argumentModel.ShowInHelpText,
                        multipleValues: true);
                    return () => argument.Values.ToArray();
                }
            }

            if (argumentModel.MultipleValues == true)
                throw new NotSupportedException($"No converter from string[] to {argumentModel.Parameter.ParameterType.FullName}.");
            else
                throw new NotSupportedException($"No converter from string to {argumentModel.Parameter.ParameterType.FullName}.");
        }

        protected virtual Func<object> BindOption(CommandLineApplication command, CommandModel.OptionModel optionModel)
        {
            if (optionModel == null)
                throw new ArgumentNullException(nameof(optionModel));
            if (optionModel.Parameter == null)
                throw new ArgumentNullException(nameof(optionModel.Parameter));

            if (optionModel.OptionType != CommandOptionType.MultipleValue &&
                optionModel.OptionType != CommandOptionType.SingleValue)
            {
                if (optionModel.Parameter.ParameterType.IsAssignableFrom(typeof(bool)))
                {
                    var option = command.Option(optionModel.Template, optionModel.Description,
                        optionType: CommandOptionType.NoValue);
                    option.ShowInHelpText = optionModel.ShowInHelpText;
                    return () => option.HasValue();
                }
            }
            if (optionModel.OptionType != CommandOptionType.MultipleValue &&
                optionModel.OptionType != CommandOptionType.NoValue)
            {
                var converter = SelectConverter(
                    EnumerateConverterTypes(optionModel),
                    typeof(string), optionModel.Parameter.ParameterType
                );
                if (converter != null)
                {
                    var option = command.Option(optionModel.Template, optionModel.Description,
                        optionType: CommandOptionType.SingleValue);
                    option.ShowInHelpText = optionModel.ShowInHelpText;
                    if (optionModel.Parameter.HasDefaultValue)
                    {
                        var defaultValue = optionModel.Parameter.RawDefaultValue;
                        return () => option.HasValue() ? converter.Invoke(option.Value()) : defaultValue;
                    }
                    else
                        return () => option.HasValue() ? converter.Invoke(option.Value()) : null;
                }
            }
            if (optionModel.OptionType != CommandOptionType.SingleValue &&
                optionModel.OptionType != CommandOptionType.NoValue)
            {
                var converter = SelectConverter(
                    EnumerateConverterTypes(optionModel),
                    typeof(List<string>), optionModel.Parameter.ParameterType
                );
                if (converter != null)
                {
                    var option = command.Option(optionModel.Template, optionModel.Description,
                        optionType: CommandOptionType.MultipleValue);
                    option.ShowInHelpText = optionModel.ShowInHelpText;
                    if (optionModel.Parameter.HasDefaultValue)
                    {
                        var defaultValue = optionModel.Parameter.RawDefaultValue;
                        return () => option.HasValue() ? converter.Invoke(option.Values) : defaultValue;
                    }
                    else
                        return () => converter.Invoke(option.Values);
                }
            }
            if (optionModel.OptionType != CommandOptionType.SingleValue &&
                optionModel.OptionType != CommandOptionType.NoValue)
            {
                var converter = SelectConverter(
                    EnumerateConverterTypes(optionModel),
                    typeof(string[]), optionModel.Parameter.ParameterType
                );
                if (converter != null)
                {
                    var option = command.Option(optionModel.Template, optionModel.Description,
                        optionType: CommandOptionType.MultipleValue);
                    option.ShowInHelpText = optionModel.ShowInHelpText;
                    if (optionModel.Parameter.HasDefaultValue)
                    {
                        var defaultValue = optionModel.Parameter.RawDefaultValue;
                        return () => option.HasValue() ? converter.Invoke(option.Values.ToArray()) : defaultValue;
                    }
                    else
                        return () => converter.Invoke(option.Values.ToArray());
                }
            }
            if (optionModel.OptionType != CommandOptionType.MultipleValue &&
                optionModel.OptionType != CommandOptionType.NoValue)
            {
                if (optionModel.Parameter.ParameterType.IsAssignableFrom(typeof(string)))
                {
                    var option = command.Option(optionModel.Template, optionModel.Description,
                        optionType: CommandOptionType.SingleValue);
                    option.ShowInHelpText = optionModel.ShowInHelpText;
                    if (optionModel.Parameter.HasDefaultValue)
                    {
                        var defaultValue = optionModel.Parameter.RawDefaultValue;
                        return () => option.HasValue() ? option.Value() : defaultValue;
                    }
                    else
                        return () => option.Value();
                }
            }
            if (optionModel.OptionType != CommandOptionType.SingleValue &&
                optionModel.OptionType != CommandOptionType.NoValue)
            {
                if (optionModel.Parameter.ParameterType.IsAssignableFrom(typeof(List<string>)))
                {
                    var option = command.Option(optionModel.Template, optionModel.Description,
                        optionType: CommandOptionType.MultipleValue);
                    option.ShowInHelpText = optionModel.ShowInHelpText;
                    if (optionModel.Parameter.HasDefaultValue)
                    {
                        var defaultValue = optionModel.Parameter.RawDefaultValue;
                        return () => option.HasValue() ? option.Values : defaultValue;
                    }
                    else
                        return () => option.Values;
                }
                if (optionModel.Parameter.ParameterType.IsAssignableFrom(typeof(string[])))
                {
                    var option = command.Option(optionModel.Template, optionModel.Description,
                        optionType: CommandOptionType.MultipleValue);
                    option.ShowInHelpText = optionModel.ShowInHelpText;
                    if (optionModel.Parameter.HasDefaultValue)
                    {
                        var defaultValue = optionModel.Parameter.RawDefaultValue;
                        return () => option.HasValue() ? option.Values.ToArray() : defaultValue;
                    }
                    else
                        return () => option.Values.ToArray();
                }
            }
            if (optionModel.OptionType != CommandOptionType.MultipleValue &&
                optionModel.OptionType != CommandOptionType.SingleValue)
            {
                var converter = SelectConverter(
                    EnumerateConverterTypes(optionModel),
                    typeof(bool), optionModel.Parameter.ParameterType
                );
                if (converter != null)
                {
                    var option = command.Option(optionModel.Template, optionModel.Description,
                        optionType: CommandOptionType.SingleValue);
                    option.ShowInHelpText = optionModel.ShowInHelpText;
                    return () => converter.Invoke(option.HasValue());
                }
            }

            if (optionModel.OptionType == CommandOptionType.MultipleValue)
                throw new NotSupportedException($"No converter from string[] to {optionModel.Parameter.ParameterType.FullName}.");
            else if (optionModel.OptionType == CommandOptionType.NoValue)
                throw new NotSupportedException($"No converter from bool to {optionModel.Parameter.ParameterType.FullName}.");
            else
                throw new NotSupportedException($"No converter from string to {optionModel.Parameter.ParameterType.FullName}.");
        }

        private Func<object, object> SelectConverter(IEnumerable<Type> converterTypes, Type sourceType, Type targetType)
        {
            return converterTypes.Distinct().Select(type =>
            {
                var converter = CreateConverter(type);
                if (converter != null)
                {
                    if (converter.CanConvert(sourceType, targetType))
                        return converter;
                }
                return null;
            })
            .Where(converter => converter != null)
            .Select(converter => (Func<object, object>)(source => converter.Convert(source, targetType)))
            .FirstOrDefault();
        }

        private IConverter CreateConverter(Type type)
        {
            if (typeof(IConverter).IsAssignableFrom(type))
            {
                IConverter converter;
                converter = ServiceProvider.GetService(type) as IConverter;
                if (converter != null)
                    return converter;
                if (!type.IsAbstract)
                {
                    var constructor = type
                        .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                        .Where(ctor => !ctor.IsAbstract)
                        .FirstOrDefault(ctor => ctor.GetParameters().Length == 0);
                    converter = constructor?.Invoke(null) as IConverter;
                    if (converter != null)
                        return converter;
                }
            }
            return null;
        }

        private IEnumerable<Type> EnumerateConverterTypes(CommandModel commandModel)
        {
            var converters = commandModel.Converters;
            if (converters != null)
                foreach (var converter in converters)
                    yield return converter;
            var parentConverters = commandModel.Parent?.Converters;
            if (commandModel.Parent != null)
                foreach (var converter in EnumerateConverterTypes(commandModel.Parent))
                    yield return converter;
            else if (GlobalConverters != null)
                foreach (var converter in GlobalConverters)
                    yield return converter;
        }

        private IEnumerable<Type> EnumerateConverterTypes(CommandModel.ArgumentModel argumentModel)
        {
            var converters = argumentModel.Converters;
            if (converters != null)
                foreach (var converter in converters)
                    yield return converter;
            if (argumentModel.Command != null)
                foreach (var converter in EnumerateConverterTypes(argumentModel.Command))
                    yield return converter;
            else if (GlobalConverters != null)
                foreach (var converter in GlobalConverters)
                    yield return converter;
        }

        private IEnumerable<Type> EnumerateConverterTypes(CommandModel.OptionModel optionModel)
        {
            var converters = optionModel.Converters;
            if (converters != null)
                foreach (var converter in converters)
                    yield return converter;
            if (optionModel.Command != null)
                foreach (var converter in EnumerateConverterTypes(optionModel.Command))
                    yield return converter;
            else if (GlobalConverters != null)
                foreach (var converter in GlobalConverters)
                    yield return converter;
        }

        private Func<object, int> SelectResultHandler(IEnumerable<Type> resultHandlerTypes)
        {
            var handlers = resultHandlerTypes.Distinct().Select(type => CreateResultHandler(type))
                .Where(handler => handler != null).ToList();

            return value =>
            {
                int result = 0;
                foreach (var r in handlers.Select(handler => handler.Handle(value)))
                {
                    result = r;
                    if (result >= 0)
                        break;
                    else
                        continue;
                }
                return result;
            };
        }

        private IResultHandler CreateResultHandler(Type type)
        {
            if (typeof(IResultHandler).IsAssignableFrom(type))
            {
                IResultHandler handler = ServiceProvider.GetService(type) as IResultHandler;
                if (handler != null)
                    return handler;
                if (!type.IsAbstract)
                {
                    var constructor = type
                        .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                        .Where(ctor => !ctor.IsAbstract)
                        .FirstOrDefault(ctor => ctor.GetParameters().Length == 0);
                    handler = constructor?.Invoke(null) as IResultHandler;
                    if (handler != null)
                        return handler;
                }
            }
            return null;
        }

        private IEnumerable<Type> EnumerateResultHandlerTypes(CommandModel commandModel)
        {
            var handlers = commandModel.ResultHandlers;
            if (handlers != null)
                foreach (var handler in handlers)
                    yield return handler;
            var parentHandlers = commandModel.Parent?.ResultHandlers;
            if (commandModel.Parent != null)
                foreach (var handler in EnumerateResultHandlerTypes(commandModel.Parent))
                    yield return handler;
            else if (GlobalResultHandlers != null)
                foreach (var handler in GlobalResultHandlers)
                    yield return handler;
        }

        private Func<Exception, int> SelectExceptionHandler(IEnumerable<Type> exceptionHandlerTypes)
        {
            var handlers = exceptionHandlerTypes.Distinct().Select(type => CreateExceptionHandler(type))
                .Where(handler => handler != null).ToList();

            return ex =>
            {
                int result = ex.HResult;
                foreach (var r in handlers.Select(handler => handler.Handle(ex)))
                {
                    result = r;
                    if (result >= 0)
                        break;
                    else
                        continue;
                }
                return result;
            };
        }

        private IExceptionHandler CreateExceptionHandler(Type type)
        {
            if (typeof(IExceptionHandler).IsAssignableFrom(type))
            {
                IExceptionHandler handler = ServiceProvider.GetService(type) as IExceptionHandler;
                if (handler != null)
                    return handler;
                if (!type.IsAbstract)
                {
                    var constructor = type
                        .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                        .Where(ctor => !ctor.IsAbstract)
                        .FirstOrDefault(ctor => ctor.GetParameters().Length == 0);
                    handler = constructor?.Invoke(null) as IExceptionHandler;
                    if (handler != null)
                        return handler;
                }
            }
            return null;
        }

        private IEnumerable<Type> EnumerateExceptionHandlerTypes(CommandModel commandModel)
        {
            var handlers = commandModel.ExceptionHandlers;
            if (handlers != null)
                foreach (var handler in handlers)
                    yield return handler;
            var parentHandlers = commandModel.Parent?.ExceptionHandlers;
            if (commandModel.Parent != null)
                foreach (var handler in EnumerateExceptionHandlerTypes(commandModel.Parent))
                    yield return handler;
            else if (GlobalExceptionHandlers != null)
                foreach (var handler in GlobalExceptionHandlers)
                    yield return handler;
        }
    }
}