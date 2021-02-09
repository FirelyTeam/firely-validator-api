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
using MessagePack;
using MessagePack.Formatters;
using MessagePack.ImmutableCollection;
using MessagePack.Resolvers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;
using T = System.Threading.Tasks;

namespace Firely.Validation.Compilation
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
                sb.Append("x");
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
            var json = schemaElement.ToJson().ToString();
            Debug.WriteLine(json);

            var validationContext = new ValidationContext() { FhirPathCompiler = _fixture.FpCompiler, ElementSchemaResolver = _fixture.Resolver };
            var results = await schemaElement.Validate(new[] { patient }, validationContext);

            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeFalse("HumanName is valid");
            results.Result.Evidence.Should().AllBeOfType<IssueAssertion>();
            var referenceObject = new IssueAssertion(Issue.CONTENT_ELEMENT_VALUE_TOO_LONG, "Patient.name[0].family[0].value", null);
            results.Result.Evidence
                .Should()
                .AllBeEquivalentTo(referenceObject, options => options.Excluding(o => o.Message));

            // dump cache to Debug output
            var r = _fixture.Resolver as ElementSchemaResolver;
            r.DumpCache();
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
            var results = await schemaElement.Validate(new[] { element }, validationContext);
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

            var results = await schemaElement.Validate(new[] { element }, new ValidationContext() { TerminologyService = _fixture.TerminologyService });

            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeFalse("HumanName is invalid: name too long");
        }

        [Fact]
        public async T.Task TestEmptyHuman()
        {
            var poco = new HumanName();
            var element = poco.ToTypedElement();

            var schemaElement = await _fixture.Resolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/HumanName", UriKind.Absolute));

            var results = await schemaElement.Validate(new[] { element }, new ValidationContext() { TerminologyService = _fixture.TerminologyService });
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
            var results = await instantSchema.Validate(new[] { element }, validationContext);

            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public async T.Task ValidateMaxStringonFhirString()
        {
            var fhirString = new FhirString(bigString()).ToTypedElement();

            var stringSchema = await _fixture.Resolver.GetSchema(new Uri("http://hl7.org/fhir/StructureDefinition/string", UriKind.Absolute));

            var results = await stringSchema.Validate(new[] { fhirString }, new ValidationContext() { FhirPathCompiler = _fixture.FpCompiler });

            results.Should().NotBeNull();
            results.Result.IsSuccessful.Should().BeFalse("fhirString is not valid");
        }

        [Fact]
        public void CanSerializeITypedElement()
        {
            var options = buildOptions();
            var demoData = buildDemoData();
            var bytes = MessagePackSerializer.Serialize(demoData, options: options);
            var deInstance = MessagePackSerializer.Deserialize<ITypedElement>(bytes, options: options);

            deInstance.Should().BeEquivalentTo(demoData);
        }


        [Fact]
        public void CanSerializeAssertions()
        {
            autoDeclareImplementers(typeof(IAssertion));

            var cardinality = new CardinalityAssertion(0, "*", "somewhere");
            var binding = new BindingAssertion("http://nu.nl", BindingAssertion.BindingStrength.Example, true, "bla");
            var all = new AllAssertion(cardinality, binding);

            var fhirPath = new FhirPathAssertion("inv-1", "true", "Always true", IssueSeverity.Information, false);
            var fixd = new Fixed(buildDemoData());
            var any = new AnyAssertion(fhirPath, fixd);

            var pat = new Pattern("a*");
            var slice1 = new SliceAssertion.Slice("slice1", new FhirTypeLabel("Identifier"), new MaxLength(100));
            var slice2 = new SliceAssertion.Slice("slice2", new FhirTypeLabel("Coding"), new BindingAssertion("http://nu.nl", BindingAssertion.BindingStrength.Preferred));
            var slice = new SliceAssertion(true, pat, slice1, slice2);

            var type = new FhirTypeLabel("HumanName");
            var order = new XmlOrder(10);
            var child = new Children(allowAdditionalChildren: false, ("child1", type), ("child2", order), ("child3", slice));

            var minmax = new MinMaxValue(314, MinMax.MinValue);
            var ps = new PathSelectorAssertion("Patient.active", new Fixed(true));
            var pattern = new Pattern("pattern string");
            var subElement = new ElementSchema("nested", minmax, ps, pattern);
            var defs = new Definitions(subElement);

            var result = new ResultAssertion(ValidationResult.Failure, new Fhir.Validation.Trace("Just because"));

            var schema = new ElementSchema(defs, all, any, child, result);
            assertRoundTrip(schema);
        }

        private void autoDeclareImplementers(Type polymorphType)
        {
            var iaAssembly = polymorphType.Assembly;
            var derivedTypes = iaAssembly.DefinedTypes.Where(dt => polymorphType.IsAssignableFrom(dt)).ToList();
            derivedTypes.Remove(polymorphType.GetTypeInfo());

            if (derivedTypes.Any())
                ConfigurableDynamicUnionResolver.DeclareUnion(polymorphType, derivedTypes);

            foreach (var derivedType in derivedTypes)
                autoDeclareImplementers(derivedType);
        }

        private MessagePackSerializerOptions buildOptions()
        {
            var resolver = CompositeResolver.Create(formatters: new[] { TypedElementFormatter.Instance }, resolvers: buildResolvers());
            return MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }

        private void assertRoundTrip(IAssertion instance)
        {
            var options = buildOptions();

            var bytes = MessagePackSerializer.Serialize(instance, options: options);
            var dump = MessagePackSerializer.ConvertToJson(bytes);
            var deInstance = MessagePackSerializer.Deserialize<IAssertion>(bytes, options: options);

            var left = Convert.ChangeType(deInstance, deInstance.GetType());
            var right = Convert.ChangeType(instance, instance.GetType());
            left.Should().BeEquivalentTo(right);
        }

        private ITypedElement buildDemoData() => new SimpleTypedElement("Root")
        {
            Location = "Root",
            InstanceType = "DemoType",
            Children = new List<SimpleTypedElement>()
            {
                elem("time",Hl7.Fhir.ElementModel.Types.Time.Now()),
                elem("date",Hl7.Fhir.ElementModel.Types.Date.Today()),
                elem("dateTime",Hl7.Fhir.ElementModel.Types.DateTime.Now()),
                elem("decimal", 3.141M),
                elem("bool", true),
                elem("integer", 314L),
                elem("unsignedInt", 314L),
                elem("positiveInt", 314L),
                elem("string", "Hi!"),
            }
        };

        private static SimpleTypedElement elem(string type, object value) =>
            new(type + "Element")
            {
                Location = "Root." + type + "Element",
                InstanceType = type,
                Value = value
            };

        private IFormatterResolver[] buildResolvers()
        {
            return new IFormatterResolver[]
            {
                BuiltinResolver.Instance, // Try Builtin
                AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]
                ImmutableCollectionResolver.Instance,
                CompositeResolver.Create(ExpandoObjectFormatter.Instance),
                DynamicGenericResolver.Instance, // Try Array, Tuple, Collection, Enum(Generic Fallback)
                ConfigurableDynamicUnionResolver.Instance, // Try Union(Interface)
                DynamicObjectResolver.Instance, // Try Object
            };
        }
    }
}

