namespace build.Configuration;

using Extensions.Options.AutoBinder;

[AutoBind]
public class BuildOptions
{
    public NugetOptions Nuget { get; set; } = new();
    public string? ArtifactsDirectory { get; set; }
}

public class NugetOptions
{
    public string? Source { get; set; }
    public string? ApiKey { get; set; }
}