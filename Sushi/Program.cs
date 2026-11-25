using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Sushi.Commands;
using Sushi.Compiler;
using Sushi.Compiler.Configuration;
using Sushi.Compiler.Diagnostics;
using Sushi.Compiler.Lexing;
using Sushi.Compiler.Source;
using Sushi.Core.Configuration;
using Sushi.Core.Diagnostics;
using Sushi.Core.Lexing;
using Sushi.Core.Source;
using Sushi.Diagnostics;
using Sushi.Diagnostics.Exceptions;
using Sushi.LanguageServer;

namespace Sushi;

/// <summary>
/// Contains the application entry point and top-level application lifecycle.
/// </summary>
internal static class Program
{
    private static ServiceProvider? serviceProvider;

    /// <summary>
    /// The service provider that acts as a container for dependency injection.
    /// </summary>
    public static IServiceProvider? ServiceProvider => serviceProvider;

    /// <summary>
    /// The application entry point.
    /// </summary>
    /// <param name="args">
    /// The command line arguments.
    /// </param>
    internal static async Task Main(string[] args)
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            if (args is null || args.Length == 0)
            {
                Log.Information("Use the help command to see available commands. ex: \"Sushi help\"");
                Exit(ExitCode.Success);
            }

            Initialize(args);

            ICommandContext context = ServiceProvider!.GetRequiredService<ICommandContext>();
            ICommandProvider provider = ServiceProvider!.GetRequiredService<ICommandProvider>();

            ISushiCommand? command = provider.Commands.FirstOrDefault(x => x.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase)) ?? throw new SushiProgramException(SushiProgramError.InvalidParameterSyntax, $"Command {args[0]} was recognized by the command context, but no such command was found in the command registry. This is a bug.");

            CommandExecutionResult result = await command.Execute(context);

            if (!result.Success)
            {
                throw new SushiProgramException(result.ErrorType.Value, result.FailureReason);
            }
        }
        catch (SushiProgramException exception)
        {
            Log.Error("{Message}", exception.Message);
            Exit(exception.Error switch
            {
                SushiProgramError.InvalidParameterSyntax => ExitCode.InvalidParameterSyntax,
                SushiProgramError.InvalidPath => ExitCode.InvalidProjectFileOrFolder,
                _ => ExitCode.UnhandledException
            });
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Unhandled Exception");
            Exit(ExitCode.UnhandledException);
        }
#pragma warning restore CA1031 // Do not catch general exception types    
    }

    /// <summary>
    /// Initializes the program.
    /// </summary>
    /// <param name="args">
    /// The command line arguments.
    /// </param>
    private static void Initialize([NotNull] string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

        Directory.CreateDirectory(logsPath);

        LoggingLevelSwitch levelSwitch = new();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.File(new CompactJsonFormatter(), Path.Combine(logsPath, "info.log"), rollingInterval: RollingInterval.Day)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {Message:lj}{NewLine}{Exception}",
                theme: AppMeta.ConsoleTheme,
                applyThemeToRedirectedOutput: false,
                formatProvider: CultureInfo.CurrentCulture,
                standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();

        Log.Information("Welcome to Sushi Version {Version}.", AppMeta.GetVersion());

        Log.Information("This assembly is running in {Mode} mode.", AppMeta.IsDebug ? "DEBUG" : "RELEASE");

        AppDomain.CurrentDomain.ProcessExit += OnExit;

        InitializeDependencyInjection(args[0], [.. args.Skip(1)], levelSwitch);
    }

    /// <summary>
    /// Initializes the dependency injection container for the application.
    /// </summary>
    /// <param name="command">
    /// The command used to initialize the command provider.
    /// </param>
    /// <param name="args">
    /// The command line arguments used to initialize the command provider.
    /// </param>
    /// <param name="levelSwitch">
    /// The switch used to change the logging level of the logger after configuration.
    /// </param>
    private static void InitializeDependencyInjection([NotNull] string command, [NotNull] string[] args, [NotNull] LoggingLevelSwitch levelSwitch)
    {
        ServiceCollection services = new();

        ConfigureServices(services, command, args, levelSwitch);

        serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Configures the services of the application that will be used for dependency injection.
    /// </summary>
    /// <param name="services">
    /// The service collection used to register services.
    /// </param>
    /// <param name="command">
    /// The command used to initialize the command provider.
    /// </param>
    /// <param name="args">
    /// The command line arguments used to initialize the command provider.
    /// </param>
    /// <param name="levelSwitch">
    /// The switch used to change the logging level of the logger after configuration.
    /// </param>
    private static void ConfigureServices([NotNull] IServiceCollection services, [NotNull] string command, [NotNull] string[] args, [NotNull] LoggingLevelSwitch levelSwitch)
    {
        services.AddSingleton<ISushiCompiler, SushiCompiler>();
        services.AddSingleton<ISushiLanguageServer, SushiLanguageServer>();
        services.AddSingleton<ISourceDocumentProvider, SushiSourceDocumentProvider>();
        services.AddSingleton<IDiagnosticReporterFactory, DiagnosticReporterFactory>();
        services.AddSingleton<ICommandProvider, SushiCommandProvider>();
        services.AddSingleton<IProjectSourceLoader, ProjectSourceLoader>();
        services.AddSingleton<IKeywordProvider, KeywordProvider>();
        services.AddTransient<ICommandContext, SushiCommandContext>(services
            => ActivatorUtilities.CreateInstance<SushiCommandContext>(services, command, args, levelSwitch));
    }
    
    /// <summary>
    /// Event that fires when the application is exiting.
    /// </summary>
    /// <param name="sender">
    /// The event sender.
    /// </param>
    /// <param name="args">
    /// The event arguments.
    /// </param>
    private static void OnExit(object? sender, EventArgs args)
    {
        Log.CloseAndFlush();
        serviceProvider?.Dispose();
    }
    
    /// <summary>
    /// Exits the program gracefully.
    /// </summary>
    /// <param name="exitCode">
    /// The exit code to use.
    /// </param>
    [DoesNotReturn]
    public static void Exit(ExitCode exitCode) => Environment.Exit((int)exitCode);
}
