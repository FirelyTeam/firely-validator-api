using Firely.Fhir.Validation.Compilation.Tests;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Tests.Impl
{
    public class StructureDefinitionSchemaTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public StructureDefinitionSchemaTests(SchemaBuilderFixture fixture) => _fixture = fixture;


        [Fact]
        public void ValidateElementDefinitioninProfileValueType()
        {
            var structureDefSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/StructureDefinition");

            var elementDef1 = new ElementDefinition
            {
                Path = "Patient.gender",
                Constraint = new()
                {
                    new()
                    {
                        Key = "key1",
                        Expression = "foo.bar"
                    },
                     new()
                    {
                        Key = "key2",
                        Expression = "bar.foo"
                    }
                }
            };
            var elementDef2 = new ElementDefinition
            {
                Path = "Patient.gender",
                Constraint = new()
                {
                    new()
                    {
                        Key = "key1",
                        Expression = "foo.bar"
                    },
                }
            };
            var elementDef3 = new ElementDefinition
            {
                Path = "Patient.birthdate",
                Constraint = new()
                {
                     new()
                    {
                        Key = "key2",
                        Expression = "bar.foo"
                    }
                }
            };
            var elementDef4 = new ElementDefinition
            {
                Path = "Patient.birthdate",
                Constraint = new()
                {
                     new()
                    {
                        Key = "key4",
                        Expression = "bar.foo"
                    }
                }
            };

            var profile = new StructureDefinition
            {
                Snapshot = new()
                {
                    Element = new() {

                        elementDef1,
                        elementDef2,
                        elementDef3,
                        elementDef4
                    }
                },
                Differential = new()
                {
                    Element = new() {

                        elementDef1,
                        elementDef2,
                        elementDef3,
                        elementDef4
                    }
                }
            };

            var results = structureDefSchema!.Validate(profile.ToTypedElement(), _fixture.NewValidationContext());

            results.Warnings.Select(w => w.Message).Should().Contain($"Duplicate key 'key2' in paths: Patient.gender, Patient.birthdate");
        }
    }
}
