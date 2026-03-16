using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class CrossVersionConfigurationAttribute : Attribute, IConfigSource
{
    private record PackageVersion(string ValidatorVersion, string SDKVersion, string? Constant = null, bool Baseline = false);
    private readonly static PackageVersion[] ALL_VERSIONS = [
        new("2.7.0",                    "5.12.1"),
        new("2.8.0-alpha-20250905.1",   "6.0.0-rc2-20250915.4")
    ];

    private const string PACKAGE = "Firely.Fhir.Validation.R4";

    public CrossVersionConfigurationAttribute(Type benchmarkType, bool displayGenColumns = false)
    {
        var attributes = benchmarkType.GetCustomAttributes(typeof(PackageVersionAttribute), false);
        var versions = attributes.OfType<PackageVersionAttribute>().Select(x => new PackageVersion(x.ValidatorVersion, x.SDKVersion, x.Constant, x.Baseline)).Distinct().ToList();
        
        if (versions.Count == 0)
            versions.AddRange(ALL_VERSIONS);
        
        Config = ManualConfig.CreateEmpty()
            .AddDiagnoser(new MemoryDiagnoser(new(displayGenColumns)))
            .HideColumns(BenchmarkDotNet.Columns.Column.Arguments, BenchmarkDotNet.Columns.Column.NuGetReferences)
            .AddJob([..buildJobsFromVersions(versions)]);
        
        if (benchmarkType.GetCustomAttribute(typeof(ProjectReferenceAttribute)) is not null)
            Config = Config.AddJob(Job.Default.WithId("Current Branch"));
    }

    private static IEnumerable<Job> buildJobsFromVersions(IEnumerable<PackageVersion> versions)
    {
        foreach (var major in versions.ToLookup(x => x.SDKVersion[0]))
        {
            foreach (var version in major)
            {
                var defConst = version.Constant ?? $"VALSDK{major.Key}";
                var job = Job.Default
                    .WithId(version.ValidatorVersion)
                    // will upgrade the version defined in csproj to a specified version
                    .WithMsBuildArguments($"/p:ValidatorVersion={version.ValidatorVersion}", $"/p:SDKVersion={version.SDKVersion}", getArgFor(defConst));

                if (version.Baseline)
                    yield return job.AsBaseline();
                else
                    yield return job;
            }
        }

    }

    static string getArgFor(string constant)
    {
        return $"/p:DefineConstants={constant}";
    }
    
    public IConfig Config { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PackageVersionAttribute(string ValidatorVersion, string SDKVersion) : Attribute
{
    public string ValidatorVersion { get; } = ValidatorVersion;
    public string SDKVersion { get; } = SDKVersion;
    public string? Constant { get; set; } = null;
    public bool Baseline { get; set; } = false;
}
public class ProjectReferenceAttribute : Attribute;