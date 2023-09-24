namespace build.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;
using Bullseye;
using static Bullseye.Targets;
using static SimpleExec.Command;

public class TargetsCommand : Command
{
    private const string ArtifactsDirectory = ".artifacts";

    private static readonly Option<string> ConfigurationOption =
        new(new[] { "--configuration", "-C" }, () => "Release", "The configuration to run the target");

    public TargetsCommand() : base("targets", "Execute build targets")
    {
        AddOption(ConfigurationOption);
        ImportBullseyeConfigurations();

        this.SetHandler(async context =>
        {
            var configuration = context.ParseResult.GetValueForOption(ConfigurationOption);
            var testOutputPath = Path.Combine(ArtifactsDirectory, "test-results");
            var reportsOutputPath = Path.Combine(ArtifactsDirectory, "coveragereport");

            Target(Targets.RestoreTools, async () => { await RunAsync("dotnet", "tool restore"); });

            Target(Targets.CleanArtifactsOutput, () =>
            {
                if (Directory.Exists(ArtifactsDirectory))
                {
                    Directory.Delete(ArtifactsDirectory, true);
                }
            });

            Target(Targets.CleanBuildOutput,
                async () => { await RunAsync("dotnet", $"clean -c {configuration} -v m --nologo"); });

            Target(Targets.CleanAll,
                DependsOn(Targets.CleanArtifactsOutput, Targets.CleanBuildOutput));

            Target(Targets.Build, DependsOn(Targets.CleanBuildOutput),
                async () => { await RunAsync("dotnet", $"build -c {configuration} --nologo"); });

            Target(Targets.Pack, DependsOn(Targets.CleanArtifactsOutput, Targets.Build), async () =>
            {
                await RunAsync("dotnet",
                    $"pack -c {configuration} -o {Directory.CreateDirectory(ArtifactsDirectory).FullName} --no-build --nologo");
            });

            Target(Targets.PublishArtifacts, DependsOn(Targets.Pack), () => Console.WriteLine("publish artifacts"));

            Target("default", DependsOn(Targets.RunTests, Targets.PublishArtifacts));

            Target(Targets.RunTests, DependsOn(Targets.Build), async () =>
            {
                await RunAsync("dotnet",
                    $"test -c {configuration} --no-build --nologo --collect:\"XPlat Code Coverage\" --results-directory {testOutputPath}");
            });

            Target(Targets.RunTestsCoverage, DependsOn(Targets.RestoreTools, Targets.RunTests), () =>
            {
                Run("dotnet",
                    $"reportgenerator -reports:{testOutputPath}/**/*cobertura.xml -targetdir:{reportsOutputPath} -reporttypes:HtmlSummary");
            });

            await RunBullseyeTargetsAsync(context);
        });
    }

    private void ImportBullseyeConfigurations()
    {
        Add(new Argument<string[]>("targets")
        {
            Description =
                "A list of targets to run or list. If not specified, the \"default\" target will be run, or all targets will be listed. Target names may be abbreviated. For example, \"b\" for \"build\"."
        });

        foreach (var (aliases, description) in Bullseye.Options.Definitions)
            Add(new Option<bool>(aliases.ToArray(), description));
    }

    private async Task RunBullseyeTargetsAsync(InvocationContext context)
    {
        var targets = context.ParseResult.CommandResult.Tokens.Select(token => token.Value);
        var options = new Options(Bullseye.Options.Definitions.Select(definition => (definition.Aliases[0],
            context.ParseResult.GetValueForOption(Options.OfType<Option<bool>>()
                .Single(option => option.HasAlias(definition.Aliases[0]))))));
        await RunTargetsWithoutExitingAsync(targets, options);
    }
}

internal static class Targets
{
    public const string RunTestsCoverage = "run-tests-coverage";
    public const string RestoreTools = "restore-tools";
    public const string CleanBuildOutput = "clean-build-output";
    public const string CleanArtifactsOutput = "clean-artifacts-output";
    public const string CleanAll = "clean";
    public const string Build = "build";
    public const string RunTests = "run-tests";
    public const string Pack = "pack";
    public const string PublishArtifacts = "publish-artifacts";
}