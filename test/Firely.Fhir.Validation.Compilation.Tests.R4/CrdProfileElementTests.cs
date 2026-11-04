/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    /// <summary>
    /// The Da Vinci CRD (hl7.fhir.us.davinci-crd#2.2.1) slices CRDHooksResponse.cards on profile, each slice referring to
    /// the CDSHooksResponse.cards element of a card-type profile through the profile-element extension. This validates
    /// a card against that element of CRDHooksResponse-instructions, as the slice does.
    /// </summary>
    public class CrdProfileElementTests
    {
        private const string INSTRUCTIONS_CARD = "http://hl7.org/fhir/us/davinci-crd/StructureDefinition/CRDHooksResponse-instructions#CDSHooksResponse.cards";

        private static ResultReport validate(DynamicDataType card)
        {
            var source = new CachedResolver(new MultiResolver(
                new DirectorySource("CrdProfiles"),
                ZipSource.CreateValidationSource()));
            var settings = new ValidationSettings(StructureDefinitionToElementSchemaResolver.CreatedCached(source), new LocalTerminologyService(source));

            return new SchemaReferenceValidator(INSTRUCTIONS_CARD).Validate(card.ToPocoNode(), settings);
        }

        private static DynamicDataType cdsHooksElement() => new() { DynamicTypeName = "http://hl7.org/fhir/tools/StructureDefinition/CDSHooksElement" };

        private static DynamicDataType instructionsCard()
        {
            var source = cdsHooksElement();
            source.SetValue("label", new FhirString("Example payer"));
            source.SetValue("topic", new Coding("http://hl7.org/fhir/us/davinci-crd/CodeSystem/temp", "guideline"));

            var card = cdsHooksElement();
            card.SetValue("uuid", new Uuid("d1577c69-dfbe-44ad-ba6d-3e05e953b2ea"));
            card.SetValue("summary", new FhirString("Prior authorization required"));
            card.SetValue("detail", new Markdown("Submit the form before ordering."));
            card.SetValue("source", source);
            return card;
        }

        [Fact]
        public void InstructionsCardIsValid()
        {
            var result = validate(instructionsCard());
            result.IsSuccessful.Should().BeTrue(string.Join("\n", result.Evidence.OfType<IssueAssertion>().Select(i => i.Message)));
        }

        [Fact]
        public void InstructionsCardRequiresDetail()
        {
            var card = instructionsCard();
            card.SetValue("detail", null);

            validate(card).Evidence.OfType<IssueAssertion>().Should().ContainSingle(i => i.Message.Contains("detail"));
        }

        [Fact]
        public void InstructionsCardDoesNotAllowLinks()
        {
            var link = cdsHooksElement();
            link.SetValue("label", new FhirString("More"));
            link.SetValue("url", new FhirUrl("https://example.org"));
            link.SetValue("type", new Code("absolute"));

            var card = instructionsCard();
            card.SetValue("links", link);

            validate(card).Evidence.OfType<IssueAssertion>().Should().Contain(i => i.Message.Contains("'links'") && i.Message.Contains("0..0"));
        }
    }
}
