using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.CommandLineUtils.Models;
using Microsoft.Extensions.DependencyInjection;
using Lapis.CommandLineUtils.Converters;
using Lapis.CommandLineUtils.ResultHandlers;
using Lapis.CommandLineUtils.ExceptionHandlers;

namespace Lapis.CommandLineUtils
{
    public static class CommandLineApplicationExtensions
    {
        public static CommandLineApplicationServiceProviderWrapper Command(this CommandLineApplication app, Type type)
        {
            return app.Command(type?.GetTypeInfo());
        }

        public static CommandLineApplicationServiceProviderWrapper Command(this CommandLineApplicationServiceProviderWrapper app, Type type)
        {
            return app.Command(type?.GetTypeInfo());
        }

        public static CommandLineApplicationServiceProviderWrapper Command(this CommandLineApplication app, TypeInfo typeInfo)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (typeInfo == null)
                throw new ArgumentNullException(nameof(typeInfo));
            
            return app
                .ConfigureServices(_ => { })
                .AddDefaultConverters()
                .AddDefaultResultHandlers()
                .AddDefaultExceptionHandlers()
                .UseDefaultCommandBuilder()
                .UseDefaultCommandBinder()
                .BuildServices()
                .Command(typeInfo);
        }

        public static CommandLineApplicationServiceProviderWrapper Command(this CommandLineApplication app, MethodInfo methodInfo)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            
            return app
                .ConfigureServices(_ => { })
                .AddDefaultConverters()
                .AddDefaultResultHandlers()
                .AddDefaultExceptionHandlers()
                .UseDefaultCommandBuilder()
                .UseDefaultCommandBinder()
                .BuildServices()
                .Command(methodInfo);
        }

        public static CommandLineApplicationServiceProviderWrapper Command(this CommandLineApplicationServiceProviderWrapper app, TypeInfo typeInfo)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (typeInfo == null)
                throw new ArgumentNullException(nameof(typeInfo));

            var modelBuilder = app.ServiceProvider.GetService<ICommandModelBuilder>();
            var commandModel = modelBuilder.BuildCommand(typeInfo);
            if (commandModel == null)
                throw new NotSupportedException($"Type {typeInfo.Name} is not supported.");

            var binder = app.ServiceProvider.GetService<ICommandBinder>();
            binder.BindCommand(app.Application, commandModel);
            return app;
        }

        public static CommandLineApplicationServiceProviderWrapper Command(this CommandLineApplicationServiceProviderWrapper app, MethodInfo methodInfo)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            var modelBuilder = app.ServiceProvider.GetService<ICommandModelBuilder>();
            var commandModel = modelBuilder.BuildCommand(methodInfo);
            if (commandModel == null)
                throw new NotSupportedException($"Method {methodInfo.Name} is not supported.");

            var binder = app.ServiceProvider.GetService<ICommandBinder>();
            binder.BindCommand(app.Application, commandModel);
            return app;
        }

        public static CommandLineApplicationServiceCollectionWrapper AddDefaultConverters(this CommandLineApplicationServiceCollectionWrapper app)
        {
            return app
                .AddConverter<SystemConvertConverter>()
                .AddConverter<TypeConverterConverter>()
                .AddConverter<MethodConverter>()
                .AddConverter<ConstrctorConverter>()
                .AddConverter<EnumNameConverter>()
                .AddConverter<CollectionConverter>();
        }

        public static CommandLineApplicationServiceCollectionWrapper AddConverter<T>(this CommandLineApplicationServiceCollectionWrapper app)
            where T : class, IConverter
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            app.Services.AddScoped<T>();
            return app;
        }

        public static CommandLineApplicationServiceCollectionWrapper AddDefaultResultHandlers(this CommandLineApplicationServiceCollectionWrapper app)
        {
            return app
                .AddResultHandler<ConsoleOutResultHandler>();
        }

        public static CommandLineApplicationServiceCollectionWrapper AddResultHandler<T>(this CommandLineApplicationServiceCollectionWrapper app)
            where T : class, IResultHandler
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            app.Services.AddScoped<T>();
            return app;
        }

        public static CommandLineApplicationServiceCollectionWrapper AddDefaultExceptionHandlers(this CommandLineApplicationServiceCollectionWrapper app)
        {
            return app
                .AddExceptionHandler<ConsoleErrorExceptionHandler>();
        }

        public static CommandLineApplicationServiceCollectionWrapper AddExceptionHandler<T>(this CommandLineApplicationServiceCollectionWrapper app)
            where T : class, IExceptionHandler
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            app.Services.AddScoped<T>();
            return app;
        }

        public static CommandLineApplicationServiceCollectionWrapper UseDefaultCommandBuilder(this CommandLineApplicationServiceCollectionWrapper app)
        {
            return app.UseCommandBuilder<CommandModelBuilder>();
        }

        public static CommandLineApplicationServiceCollectionWrapper UseCommandBuilder<T>(this CommandLineApplicationServiceCollectionWrapper app)
            where T : class, ICommandModelBuilder
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            app.Services.AddScoped<ICommandModelBuilder, T>();
            return app;
        }

        public static CommandLineApplicationServiceCollectionWrapper UseDefaultCommandBinder(this CommandLineApplicationServiceCollectionWrapper app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            
            var types = app.Services.Select(d => d.ImplementationType);
            app.Services.AddScoped<ICommandBinder, CommandBinder>(serviceProvider =>
                new CommandBinder(serviceProvider,
                    types.Where(d => typeof(IConverter).IsAssignableFrom(d)).ToList(),
                    types.Where(d => typeof(IResultHandler).IsAssignableFrom(d)).ToList(),
                    types.Where(d => typeof(IExceptionHandler).IsAssignableFrom(d)).ToList()
                )
            );
            return app;
        }

        public static CommandLineApplicationServiceCollectionWrapper UseCommandBinder<T>(this CommandLineApplicationServiceCollectionWrapper app)
            where T : class, ICommandBinder
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            app.Services.AddSingleton<ICommandBinder, T>();
            return app;
        }

        public static CommandLineApplicationServiceCollectionWrapper ConfigureServices(this CommandLineApplication app,
            Action<IServiceCollection> serviceConfiguration)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            var services = new ServiceCollection();
            serviceConfiguration?.Invoke(services);
            return new CommandLineApplicationServiceCollectionWrapper(app, services);
        }

        public static CommandLineApplicationServiceCollectionWrapper ConfigureServices(this CommandLineApplicationServiceCollectionWrapper app,
            Action<IServiceCollection> serviceConfiguration)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            var services = new ServiceCollection();
            serviceConfiguration?.Invoke(services);
            return app;
        }

        public static CommandLineApplicationServiceProviderWrapper BuildServices(this CommandLineApplicationServiceCollectionWrapper app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            return new CommandLineApplicationServiceProviderWrapper(app.Application, app.Services.BuildServiceProvider());
        }

        public static CommandLineApplicationServiceCollectionWrapper ConfigureApplication(this CommandLineApplicationServiceCollectionWrapper app,
            Action<CommandLineApplication> applicationConfiguration)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            applicationConfiguration?.Invoke(app.Application);
            return app;
        }

        public static CommandLineApplicationServiceProviderWrapper ConfigureApplication(this CommandLineApplicationServiceProviderWrapper app,
            Action<CommandLineApplication> applicationConfiguration)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            applicationConfiguration?.Invoke(app.Application);
            return app;
        }
    }

    public class CommandLineApplicationServiceCollectionWrapper
    {
        public CommandLineApplicationServiceCollectionWrapper(CommandLineApplication application, IServiceCollection services)
        {
            if (application == null)
                throw new ArgumentNullException(nameof(application));
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            Application = application;
            Services = services;
        }

        public CommandLineApplication Application { get; }

        public IServiceCollection Services { get; }
    }

    public class CommandLineApplicationServiceProviderWrapper
    {
        public CommandLineApplicationServiceProviderWrapper(CommandLineApplication application, IServiceProvider serviceProvider)
        {
            if (application == null)
                throw new ArgumentNullException(nameof(application));
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));
            Application = application;
            ServiceProvider = serviceProvider;
        }

        public CommandLineApplication Application { get; }

        public IServiceProvider ServiceProvider { get; }
    }
}