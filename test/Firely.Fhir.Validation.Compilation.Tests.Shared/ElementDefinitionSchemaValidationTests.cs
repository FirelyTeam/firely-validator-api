using Firely.Fhir.Validation.Compilation.Tests;
using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Tests.Impl
{
    public class ElementDefinitionSchemaTests : IClassFixture<SchemaBuilderFixture>
    {
        internal SchemaBuilderFixture _fixture;

        public ElementDefinitionSchemaTests(SchemaBuilderFixture fixture) => _fixture = fixture;

        [Fact]
        public void ValidateElementDefinitionValueType()
        {
            var elementDefSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/ElementDefinition");

            var elementDef = new ElementDefinition
            {
                Path = "Patient.deceased[x]",
                Type = new List<ElementDefinition.TypeRefComponent> {
                    new()
                    {
                        Code = "dateTime"
                    }
                },
                Fixed = new FhirDateTime("2020-01-01"),
                MinValue = new FhirDateTime("1900-01-01"),
                MaxValue = new FhirDateTime("2100-01-01"),
                Example = new List<ElementDefinition.ExampleComponent>
                {
                    new()
                    {
                        Label = "example",
                        Value = new FhirDateTime("2020-01-01")
                    }
                }
            };

            var results = elementDefSchema!.Validate(elementDef.ToTypedElement(), _fixture.NewValidationContext());
            results.IsSuccessful.Should().Be(true);

            elementDef = new ElementDefinition
            {
                Path = "Patient.deceased[x]",
                Type = new List<ElementDefinition.TypeRefComponent> {
                    new()
                    {
                        Code = "dateTime"
                    }
                },
                Fixed = new FhirString("2020-01-01"),
                MinValue = new Integer(0),
                MaxValue = new Integer(1),
                Example = new List<ElementDefinition.ExampleComponent>
                {
                    new()
                    {
                        Label = "example",
                        Value = new FhirString("2020-01-01")
                    }
                }
            };
            results = elementDefSchema!.Validate(elementDef.ToTypedElement(), _fixture.NewValidationContext());

            results.Warnings.Select(w => w.Message).Should().Contain("Type of the fixed property 'string' doesn't match with the type(s) of the element 'dateTime'");
            results.Warnings.Select(w => w.Message).Should().Contain("Type of the example.value property 'string' doesn't match with the type(s) of the element 'dateTime'");
            results.Warnings.Select(w => w.Message).Should().Contain("Type of the minValue property 'integer' doesn't match with the type(s) of the element 'dateTime'");
            results.Warnings.Select(w => w.Message).Should().Contain("Type of the maxValue property 'integer' doesn't match with the type(s) of the element 'dateTime'");



        }

        [Fact]
        public void ValidateElementDefinitioninProfileValueType()
        {
            var structureDefSchema = _fixture.SchemaResolver.GetSchema("http://hl7.org/fhir/StructureDefinition/StructureDefinition");

            var elementDef = new ElementDefinition
            {
                Path = "Patient.deceased[x]",
                Type = new List<ElementDefinition.TypeRefComponent> {
                    new()
                    {
                        Code = "dateTime"
                    }
                },
                Fixed = new FhirString("2020-01-01"),
                MinValue = new Integer(0),
                MaxValue = new Integer(1),
                Example = new List<ElementDefinition.ExampleComponent>
                {
                    new()
                    {
                        Label = "example",
                        Value = new FhirString("2020-01-01")
                    }
                }
            };

            var profile = new StructureDefinition
            {
                Snapshot = new()
                {
                    Element = new() {

                        elementDef
                    }
                }
            };

            var results = structureDefSchema!.Validate(profile.ToTypedElement(), _fixture.NewValidationContext());

            results.Warnings.Select(w => w.Message).Should().Contain("Type of the fixed property 'string' doesn't match with the type(s) of the element 'dateTime'");
            results.Warnings.Select(w => w.Message).Should().Contain("Type of the example.value property 'string' doesn't match with the type(s) of the element 'dateTime'");
            results.Warnings.Select(w => w.Message).Should().Contain("Type of the minValue property 'integer' doesn't match with the type(s) of the element 'dateTime'");
            results.Warnings.Select(w => w.Message).Should().Contain("Type of the maxValue property 'integer' doesn't match with the type(s) of the element 'dateTime'");
        }
    }
}
