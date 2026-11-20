using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Sushi.Compiler;
using Sushi.Compiler.Source;
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
            Initialize(args);

            ISushiLanguageServer languageServer = ServiceProvider!.GetRequiredService<ISushiLanguageServer>();
            ISushiCompiler compiler = ServiceProvider!.GetRequiredService<ISushiCompiler>();

            if (AppMeta.Options.IsInLanguageServerMode)
            {
                await Diag.Monitor("LanguageServer", () => languageServer.Run(AppMeta.Options.LSPTransportMethod, AppMeta.Options.ListenEndpoint, AppMeta.GetVersion()));
            }
            else
            {
                await Diag.Monitor("Compilation", compiler.CompileJob);
            }
        }
        catch (CompilerOptionsException exception)
        {
            Log.Error("{Message}", exception.Message);
            Exit(exception.Error switch
            {
                CompilerOptionsError.InvalidParameterSyntax => ExitCode.InvalidParameterSyntax,
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
    /// Initializes the compiler.
    /// </summary>
    /// <param name="args">
    /// The command line arguments.
    /// </param>
    private static void Initialize(string[] args)
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

        InitializeDependencyInjection();

        AppMeta.Options = CompilerOptions.FromCommandLineArguments(args);

        if (AppMeta.Options.IsDebugLoggingEnabled)
        {
            levelSwitch.MinimumLevel = LogEventLevel.Debug;
        }
    }

    /// <summary>
    /// Initializes the dependency injection container for the application.
    /// </summary>
    private static void InitializeDependencyInjection()
    {
        ServiceCollection services = new();

        ConfigureServices(services);

        serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Configures the services of the application that will be used for dependency injection.
    /// </summary>
    /// <param name="services">
    /// The service collection used to register services.
    /// </param>
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISushiCompiler, SushiCompiler>();
        services.AddSingleton<ISushiLanguageServer, SushiLanguageServer>();
        services.AddSingleton<ISourceDocumentProvider, SushiSourceDocumentProvider>();
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
