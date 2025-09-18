using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class CrossVersionConfigurationAttribute : Attribute, IConfigSource
{
    private record PackageVersion(string Version, string? Constant = null, bool Baseline = false);
    private readonly static string[] ALL_VERSIONS = [
        "2.7.0",
        "2.8.0-alpha-20250905.1"
    ];

    private const string PACKAGE = "Firely.Fhir.Validation.R4";

    public CrossVersionConfigurationAttribute(Type benchmarkType, bool displayGenColumns = false)
    {
        var attributes = benchmarkType.GetCustomAttributes(typeof(PackageVersionAttribute), false);
        var versions = attributes.OfType<PackageVersionAttribute>().Select(x=> new PackageVersion(x.PackageVersion, x.Constant, x.Baseline)).Distinct().ToList();
        
        if (versions.Count == 0)
            versions.AddRange(ALL_VERSIONS.Select(x => new PackageVersion(x)));
        
        Config = ManualConfig.CreateEmpty()
            .AddDiagnoser(new MemoryDiagnoser(new(displayGenColumns)))
            .HideColumns(BenchmarkDotNet.Columns.Column.Arguments, BenchmarkDotNet.Columns.Column.NuGetReferences)
            .AddJob([..buildJobsFromVersions(versions)]);
        
        if (benchmarkType.GetCustomAttribute(typeof(ProjectReferenceAttribute)) is not null)
            Config = Config.AddJob(Job.Default.WithId("Current Branch"));
    }

    private static IEnumerable<Job> buildJobsFromVersions(IEnumerable<PackageVersion> versions)
    {
        foreach (var major in versions.ToLookup(x => x.Version[0]+x.Version[2]))
        {
            List<Argument> args = GetArgsFor($"VAL{major.Key}");
            foreach (var version in major)
            {
                var job = Job.Default
                    .WithId(version.Version)
                    .WithArguments(version.Constant is null ? args : GetArgsFor(version.Constant))
                    // will upgrade the version defined in csproj to a specified version
                    .WithNuGet(PACKAGE, version.Version);

                if (version.Baseline)
                    yield return job.AsBaseline();
                else
                    yield return job;
            }
        }

    }

    static List<Argument> GetArgsFor(string constant)
    {
        return [new MsBuildArgument($"/p:DefineConstants={constant}")];
    }
    
    public IConfig Config { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PackageVersionAttribute(string PackageVersion) : Attribute
{
    public string PackageVersion { get; } = PackageVersion;
    public string? Constant { get; set; } = null;
    public bool Baseline { get; set; } = false;
}
public class ProjectReferenceAttribute : Attribute;