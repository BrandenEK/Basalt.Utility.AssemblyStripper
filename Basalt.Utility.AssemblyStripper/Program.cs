using Basalt.Framework.Logging.Standard;
using Basalt.Framework.Logging;
using BepInEx.AssemblyPublicizer;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Basalt.Utility.AssemblyStripper;

internal class Program
{
    private static readonly string TEMP_DIR = "temp";
    private static readonly string LIB_DIR = Path.Combine(TEMP_DIR, "lib");

    static void Main()
    {
        // Initialization
        Logger.AddLogger(new ConsoleLogger("Assembly Stripper"));
        EmptyDirectory(TEMP_DIR);
        EmptyDirectory(LIB_DIR);

        // Load stripper options from config file
        StripOptions options = LoadOptions();

        if (options.IsValid)
        {
            Logger.Info($"Loaded options: {options}");
        }
        else
        {
            Logger.Fatal("The config options were invalid");
            Console.ReadKey(true);
            return;
        }

        // Get all dlls to strip
        Logger.Info($"Getting all dlls from {options.AssemblyPath}");
        var dlls = GetAssemblyPaths(options);

        // Strip all found assemblies
        StripAssemblies(dlls);

        // Generate and pack nuspec file
        Logger.Info($"Creating nuspec file");
        GenerateNuspec(options);
        Logger.Info($"Packing nuspec file");
        PackNuspec(options);

        // Prompt user for confirmation
        Logger.Info("Are you sure you want to upload this package? [y/n]");
        ConsoleKey key = Console.ReadKey().Key;
        Console.WriteLine();
        if (key != ConsoleKey.Y)
        {
            Terminate();
            return;
        }

        // Push nupkg file
        Logger.Info($"Pushing nupkg file");
        PushNupkg(options);

        Terminate();
    }

    static void Terminate()
    {
        Directory.Delete(TEMP_DIR, true);
        Logger.Info("Application terminated.  Press any key to exit...");
        Console.ReadKey(true);
    }

    static StripOptions LoadOptions()
    {
        string path = Path.GetFullPath("options.json");

        StripOptions options = File.Exists(path)
            ? JsonConvert.DeserializeObject<StripOptions>(File.ReadAllText(path))!
            : new StripOptions();

        return WriteConfig(options, path);
    }

    static StripOptions WriteConfig(StripOptions options, string path)
    {
        JsonSerializerSettings settings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        };

        File.WriteAllText(path, JsonConvert.SerializeObject(options, settings));
        return options;
    }

    static IEnumerable<string> GetAssemblyPaths(StripOptions options)
    {
        return Directory.GetFiles(options.AssemblyPath)
            .Where(x => x.EndsWith(".dll") && !options.IgnoredAssemblies.Any(y => x.EndsWith(y)));
    }

    static void StripAssemblies(IEnumerable<string> paths)
    {
        AssemblyPublicizerOptions options = new()
        {
            Strip = true
        };

        foreach (string path in paths)
        {
            string name = Path.GetFileName(path);

            Logger.Warn($"Stripping {name}");
            AssemblyPublicizer.Publicize(path, Path.Combine(TEMP_DIR, "lib", name), options);
        }
    }

    static void GenerateNuspec(StripOptions options)
    {
        string text = string.Format(NUSPEC_TEMPLATE, options.PackageName, options.PackageVersion, options.PackageAuthor, options.PackageDescription, options.TargetFramework);
        string path = Path.Combine(TEMP_DIR, "package.nuspec");

        File.WriteAllText(path, text);
    }

    static void PackNuspec(StripOptions options)
    {
        WaitForCommand($"\"{options.NugetPath}\" pack package.nuspec");
    }

    static void PushNupkg(StripOptions options)
    {
        WaitForCommand($"dotnet nuget push **\\*.nupkg -k {options.ApiKey} -s https://api.nuget.org/v3/index.json");
    }

    private static readonly string NUSPEC_TEMPLATE = """
        <?xml version="1.0" encoding="utf-8"?>
        <package >
          <metadata>
            <id>{0}</id>
            <version>{1}</version>
            <authors>{2}</authors>
            <requireLicenseAcceptance>false</requireLicenseAcceptance>
        	<description>{3}</description>
          </metadata>
          <files>
        	<file src="lib\*.dll" target="lib\{4}" />
          </files>
        </package>
        """;

    // Helpers

    static void EmptyDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
        Directory.CreateDirectory(directory);
    }

    static void WaitForCommand(string command)
    {
        Logger.Debug($"Executing command: {command}");
        ProcessStartInfo info = new()
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            WorkingDirectory = TEMP_DIR,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        Process p = Process.Start(info);
        string output = p.StandardOutput.ReadToEnd();
        if (!string.IsNullOrEmpty(output))
            Logger.Warn(output);
        string error = p.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(error))
            Logger.Error(error);
        p.WaitForExit();
    }
}
