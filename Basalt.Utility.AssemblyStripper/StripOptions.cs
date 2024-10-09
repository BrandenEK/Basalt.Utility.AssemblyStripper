using Newtonsoft.Json;

namespace Basalt.Utility.AssemblyStripper;

public class StripOptions
{
    public string PackageName { get; set; } = string.Empty;

    public string PackageAuthor { get; set; } = string.Empty;

    public string PackageDescription { get; set; } = string.Empty;

    public string PackageVersion { get; set; } = string.Empty;

    public string TargetFramework { get; set; } = string.Empty;

    public string AssemblyPath { get; set; } = string.Empty;

    public string NugetPath { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string[] IgnoredAssemblies { get; set; } = [];

    [JsonIgnore]
    public bool IsValid => PackageName != string.Empty
        && PackageAuthor != string.Empty
        && PackageDescription != string.Empty
        && PackageVersion != string.Empty
        && TargetFramework != string.Empty
        && AssemblyPath != string.Empty
        && NugetPath != string.Empty
        && ApiKey != string.Empty;

    public override string ToString()
    {
        return $"{PackageName} v{PackageVersion} by {PackageAuthor}\n[{PackageDescription}]";
    }
}
