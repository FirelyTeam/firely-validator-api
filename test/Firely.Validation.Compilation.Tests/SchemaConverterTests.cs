using Firely.Fhir.Validation;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;
using T = System.Threading.Tasks;

namespace Firely.Validation.Compilation.Tests
{
    public class SchemaConverterFixture
    {
        public readonly IElementSchemaResolver Resolver;
        public readonly FhirPathCompiler FpCompiler;
        public readonly ITerminologyServiceNEW TerminologyService;

        public SchemaConverterFixture()
        {
            var localResolver = new CachedResolver(ZipSource.CreateValidationSource());
            Resolver = new ElementSchemaResolver(localResolver);
            TerminologyService = new TerminologyServiceAdapter(new LocalTerminologyService(localResolver));

            var symbolTable = new SymbolTable();
            symbolTable.AddStandardFP();
            symbolTable.AddFhirExtensions();
            FpCompiler = new FhirPathCompiler(symbolTable);
        }
    }

    public class SchemaConverterTests : IClassFixture<SchemaConverterFixture>
    {
        internal SchemaConverterFixture _fixture;

        public SchemaConverterTests(SchemaConverterFixture fixture) => _fixture = fixture;

        private string bigString()
        {
            var sb = new StringBuilder(1024 * 1024);
            for (int i = 0; i < 1024; i++)
            {
                sb.Append('x');
            }

            var sub = sb.ToString();

            sb = new StringBuilder(1024 * 1024);
            for (int i = 0; i < 1024; i++)
            {
                sb.Append(sub);
            }
            sb.Append("more");
            return sb.ToString();
        }

        [Fact]
        public async T.Task PatientHumanNameTooLong()
        {
            var poco = new Patient() { Name = new List<HumanName>() { new HumanName() { Family = bigString() } } };
            var patient = poco.ToTypedElement();

            var schemaElement = await _fixture.Resolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/Patient", UriKind.Absolute));
            var json = schemaElement!.ToJson().ToString();
            Debug.WriteLine(json);

            var validationContext = new ValidationContext() { FhirPathCompiler = _fixture.FpCompiler, ElementSchemaResolver = _fixture.Resolver };
            var results = await schemaElement.Validate(new[] { patient }, validationContext);

            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeFalse("HumanName is valid");
            results.Result.Evidence.Should().AllBeOfType<IssueAssertion>();
            var referenceObject = new IssueAssertion(Issue.CONTENT_ELEMENT_VALUE_TOO_LONG, "Patient.name[0].family[0].value", "too long!");
            results.Result.Evidence
                .Should()
                .AllBeEquivalentTo(referenceObject, options => options.Excluding(o => o.Message));

            // dump cache to Debug output
            var r = _fixture.Resolver as ElementSchemaResolver;
            r!.DumpCache();
        }

        private string typedElementAsString(ITypedElement element)
        {
            var json = buildNode(element);
            return json.ToString();

            JToken buildNode(ITypedElement elt)
            {
                var result = new JObject
                {
                    { "name", elt.Name },
                    { "type", elt.InstanceType },
                    { "location", elt.Location},
                    { "value", elt.Value?.ToString()},
                    { "definition", elt.Definition == null ? "" : DefintionNode(elt.Definition) }
                };
                result.Add(new JProperty("children", new JArray(elt.Children().Select(c =>
                  buildNode(c).MakeNestedProp()))));


                return result;
            }

            JToken DefintionNode(IElementDefinitionSummary def)
            {
                var result = new JObject
                {
                    { "elementName", def.ElementName },
                    { "inSummary", def.InSummary },
                    { "isChoiceElement", def.IsChoiceElement },
                    { "isCollection", def.IsCollection },
                    { "isRequired", def.IsRequired },
                    { "isResource", def.IsResource },
                    { "nonDefaultNamespace", def.NonDefaultNamespace },
                    { "order", def.Order }
                };
                return result;
            }

        }

        [Fact]
        public async T.Task HumanNameCorrect()
        {
            var poco = HumanName.ForFamily("Visser")
                .WithGiven("Marco")
                .WithGiven("Lourentius");
            poco.Use = HumanName.NameUse.Usual;
            var element = poco.ToTypedElement();

            var schemaElement = await _fixture.Resolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/HumanName", UriKind.Absolute));

            var validationContext = new ValidationContext() { TerminologyService = _fixture.TerminologyService, ElementSchemaResolver = _fixture.Resolver };
            var results = await schemaElement!.Validate(new[] { element }, validationContext);
            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeTrue("HumanName is valid");
        }

        [Fact]
        public async T.Task HumanNameTooLong()
        {
            var poco = HumanName.ForFamily(bigString())
                .WithGiven(bigString())
                .WithGiven("Maria");
            poco.Use = HumanName.NameUse.Usual;
            var element = poco.ToTypedElement();

            var schemaElement = await _fixture.Resolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/HumanName", UriKind.Absolute));

            var results = await schemaElement!.Validate(new[] { element }, new ValidationContext() { TerminologyService = _fixture.TerminologyService });

            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeFalse("HumanName is invalid: name too long");
        }

        [Fact]
        public async T.Task TestEmptyHuman()
        {
            var poco = new HumanName();
            var element = poco.ToTypedElement();

            var schemaElement = await _fixture.Resolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/HumanName", UriKind.Absolute));

            var results = await schemaElement!.Validate(new[] { element }, new ValidationContext() { TerminologyService = _fixture.TerminologyService });
            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeFalse("HumanName is valid, cannot be empty");
        }

        [Fact]
        public async T.Task TestInstance()
        {
            var instantSchema = await _fixture.Resolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/instant", UriKind.Absolute));

            var instantPoco = new Instant(DateTimeOffset.Now);

            var element = instantPoco.ToTypedElement();

            var validationContext = new ValidationContext() { FhirPathCompiler = _fixture.FpCompiler, ElementSchemaResolver = _fixture.Resolver };
            var results = await instantSchema!.Validate(new[] { element }, validationContext);

            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public async T.Task ValidateMaxStringonFhirString()
        {
            var fhirString = new FhirString(bigString()).ToTypedElement();

            var stringSchema = await _fixture.Resolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/string", UriKind.Absolute));

            var results = await stringSchema!.Validate(new[] { fhirString }, new ValidationContext() { FhirPathCompiler = _fixture.FpCompiler });

            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeFalse("fhirString is not valid");
        }
    }
}

