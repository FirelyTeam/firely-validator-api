#if !R5 && !R4B

/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using M = Hl7.Fhir.Model;

namespace Firely.Fhir.Validation.Tests
{
    internal record TestFile(string InstancePath, string CheckPath);

    [TestClass]
    public class ValidationDemoTests
    {
#if R4
        private const FhirRelease RELEASE = FhirRelease.R4;
#elif STU3
        private const FhirRelease RELEASE = FhirRelease.STU3;
#endif

        private static readonly string DEBUG_TO_SRC = Path.Combine("..", "..", "..");

        private static IEnumerable<object[]> getTestSuites()
        {
            var td = Directory.EnumerateDirectories(Path.Combine(DEBUG_TO_SRC, "TestData"));
            return td.Select(f => new object[] { Path.GetFileName(f) });
        }

        private static string getSuiteDirectory(string suiteName) =>
            Path.GetFullPath(Path.Combine(DEBUG_TO_SRC, "TestData", suiteName));

        [TestMethod]
        [DynamicData(nameof(getTestSuites), DynamicDataSourceType.Method)]
        public async Task RunValidateTestSuite(string suiteName)
        {
            var overwrite = false;

            var pd = new DirectoryInfo(Path.GetFullPath(Path.Combine(getSuiteDirectory(suiteName), "data")));
            var externalReferenceResolver = new FileBasedExternalReferenceResolver(pd);

            foreach (var (testFile, result) in runValidation(suiteName, externalReferenceResolver))
            {
                var expected = clean(await File.ReadAllTextAsync(testFile.CheckPath));
                var actual = clean(result.ToString());

                if (overwrite)
                    await File.WriteAllTextAsync(testFile.CheckPath, result.ToString());
                else
                    actual.Should().Be(expected);
            }
        }

        private static string clean(string checkData)
        {
            return checkData
                .Replace(Environment.NewLine, " ")
                .Replace('\t', ' ')
                .Trim();
        }

        private static IEnumerable<(TestFile testFile, M.OperationOutcome result)> runValidation(string suiteName,
            IExternalReferenceResolver? resolver = null)
        {
            Validator validator = buildValidator(suiteName, resolver);
            var testList = enumerateTests(suiteName).ToList();

            // Make sure we capture this suspicious situation, since it will cause the test to
            // succeed without running anything.
            if (!testList.Any())
                throw new Exception($"No test files found in suite {suiteName}");

            foreach (var testFile in testList)
            {
                var instance = parseResource(testFile.InstancePath);
                var result = validator.Validate(instance);
                yield return (testFile, result);
            }
        }

        private static IEnumerable<TestFile> enumerateTests(string suiteName)
        {
            var dataDirectory = Path.Combine(getSuiteDirectory(suiteName), "data");
            foreach (var file in Directory.EnumerateFiles(dataDirectory))
            {
                var extension = Path.GetExtension(file);
                if (extension is ".xml" or ".json")
                {
                    var checkFile = Path.ChangeExtension(file, ".check.txt");
                    if (File.Exists(checkFile))
                        yield return new TestFile(file, checkFile);
                }
            }
        }

        private static M.Resource parseResource(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            var text = File.ReadAllText(filePath);
            return extension switch
            {
                ".xml" => new FhirXmlParser().Parse<M.Resource>(text),
                ".json" => new FhirJsonParser().Parse<M.Resource>(text),
                _ => throw new NotImplementedException()
            };
        }

        private static Validator buildValidator(string suiteName, IExternalReferenceResolver? resolver)
        {
            var sources = new List<IAsyncResourceResolver>();

            var suiteDirectory = getSuiteDirectory(suiteName);
            var conformanceDirectory = Path.Combine(suiteDirectory, "conformance");
            if (Directory.Exists(conformanceDirectory))
                sources.Add(new DirectorySource(conformanceDirectory));

            var externalPackagesManifest = Path.Combine(suiteDirectory, PackageFileNames.MANIFEST);
            if (File.Exists(externalPackagesManifest))
                sources.Add(NpmPackageHelper.Create(M.ModelInfo.ModelInspector, suiteDirectory).Result);

            var packageSource = FhirPackageSource.CreateCorePackageSource(
                M.ModelInfo.ModelInspector, RELEASE,
                "https://packages.simplifier.net")!;
            sources.Add(packageSource);

            var combinedSource = new MultiResolver(sources);
            var profileSource = new SnapshotSource(new CachedResolver(combinedSource));
            var terminologySource = new LocalTerminologyService(profileSource);

            return new Validator(profileSource, terminologySource, resolver);
        }


        [TestMethod]
        public async Task CheckResolver()
        {
            var sources = new List<IAsyncResourceResolver>();

            var suiteDirectory = getSuiteDirectory("issue-165");
            var conformanceDirectory = Path.Combine(suiteDirectory, "conformance");
            if (Directory.Exists(conformanceDirectory))
                sources.Add(new DirectorySource(conformanceDirectory));

            var externalPackagesManifest = Path.Combine(suiteDirectory, PackageFileNames.MANIFEST);
            if (File.Exists(externalPackagesManifest))
                sources.Add(NpmPackageHelper.Create(M.ModelInfo.ModelInspector, suiteDirectory).Result);

            var usecasePackageSource = NpmPackageHelper.Create(M.ModelInfo.ModelInspector, suiteDirectory).Result;

            var packageSource = FhirPackageSource.CreateCorePackageSource(
                M.ModelInfo.ModelInspector, RELEASE,
                "https://packages.simplifier.net")!;
            sources.Add(packageSource);

            var combinedSource = new MultiResolver(sources);
            var profileSource = new SnapshotSource(new CachedResolver(combinedSource));


            var result = await profileSource.ResolveByCanonicalUriAsync("https://fhir.kbv.de/StructureDefinition/KBV_PR_FOR_Patient");

            result.Should().NotBeNull();
        }
    }

    internal class NpmPackageHelper
    {
        public static async Task<FhirPackageSource> Create(ModelInspector inspector, string projectPath)
        {
            var ps = new FhirPackageSource(inspector);
            var contextMember = typeof(FhirPackageSource).GetField("_context",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

            var context = await Open(projectPath);
            contextMember.SetValue(ps, new Lazy<PackageContext>(() => context));

            return ps;
        }

        public static async Task<PackageContext> Open(string path)
        {
            var client = PackageClient.Create();
            var cache = new DiskPackageCache();
            var project = new FolderProject(path);

            var scope = new PackageContext(cache, project, client);

            var closure = await scope.Restore();

            if (closure.Missing.Any())
            {
                var missingDeps = String.Join(", ", closure.Missing);
                throw new FileNotFoundException($"Could not resolve all dependencies. Missing: {missingDeps}.");
            }

            return scope;
        }

    }

    internal class FileBasedExternalReferenceResolver(DirectoryInfo baseDirectory) : IExternalReferenceResolver
    {
        private FhirXmlPocoDeserializer XmlPocoDeserializer { get; } = new FhirXmlPocoDeserializer();

        public DirectoryInfo BaseDirectory { get; private set; } = baseDirectory;

        public bool Enabled { get; set; } = true;

        public Task<object?> ResolveAsync(string reference)
        {
            if (!Enabled) return Task.FromResult<object?>(null);

            // Now, for our examples we've used the convention that the file can be found in the
            // example directory, with the name <id>.<type>.xml, so let's try to get that file.
            var identity = new ResourceIdentity(reference);
            var filename = $"{identity.Id}.{identity.ResourceType}.xml";
            var path = Path.Combine(BaseDirectory.FullName, filename);

            if (File.Exists(path))
            {
                var xml = File.ReadAllText(path);

                // Note, this will throw if the file is not really FHIR xml
                var poco = XmlPocoDeserializer.DeserializeResource(xml);

                return Task.FromResult<object?>(poco);
            }
            else
            {
                // Unsuccessful
                return Task.FromResult<object?>(null);
            }

        }
    }
}

#endif