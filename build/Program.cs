namespace build;

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Configuration;
using Extensions.Options.AutoBinder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

internal static class Program
{
    public static string DefaultConfigFile = "buildconfig.json";
    public static Option<FileInfo> ConfigurationFileGlobalOption = new("--config-file", "Specify configuration file");

    public static Option<DirectoryInfo>
        WorkingDirectoryGlobalOption = new("--working-dir", "Specify working directory");

    public static Option<LogLevel> VerbosityGlobalOption =
        new(new[] { "--log-verbosity" }, () => LogLevel.Warning, "Set log verbosity");

    private static async Task<int> Main(string[] args)
    {
        try
        {
            var command = new RootCommand();
            command.RegisterCommandsInAssembly();
            command.AddGlobalOption(ConfigurationFileGlobalOption);
            command.AddGlobalOption(WorkingDirectoryGlobalOption);
            command.AddGlobalOption(VerbosityGlobalOption);

            command.SetHandler(context =>
            {
                // ref: https://github.com/dotnet/command-line-api/issues/1537
                context.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default.GetLayout().Skip(1));
                context.HelpBuilder.Write(context.ParseResult.CommandResult.Command,
                    context.Console.Out.CreateTextWriter());
            });

            var parser = new CommandLineBuilder(command)
                .UseHost(CreateHostBuilder)
                .UseDefaults()
                .UseDebugDirective()
                .UseExceptionHandler((exception, context) =>
                {
                    Console.WriteLine($"Unhandled exception occurred: {exception.Message}");
                    context.ExitCode = 1;
                })
                .Build();

            return await parser.InvokeAsync(args);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Application terminated unexpectedly: {exception.Message}");
            return 1;
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                var fileInfo = context.GetInvocationContext().ParseResult
                    .GetValueForOption(ConfigurationFileGlobalOption);

                var basePath = context.GetInvocationContext().GetWorkingDirectory();

                var configFilePath = fileInfo is { Exists: true }
                    ? fileInfo.FullName
                    : DefaultConfigFile;

                config
                    .SetBasePath(basePath!)
                    .AddJsonFile(configFilePath, true, false)
                    .AddEnvironmentVariables();
            })
            .ConfigureLogging((context, builder) =>
            {
                var logLevel = context.GetInvocationContext().ParseResult
                    .GetValueForOption(VerbosityGlobalOption);
                builder.SetMinimumLevel(logLevel);
                builder.AddFilter<DebugLoggerProvider>(level => level >= LogLevel.Debug);
            })
            .ConfigureServices((context, services) => { services.AutoBindOptions(); });
    }
}