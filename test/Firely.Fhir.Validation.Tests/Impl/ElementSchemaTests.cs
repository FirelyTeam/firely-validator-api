using FluentAssertions;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ElementSchemaTests
    {
        [TestMethod]
        public void ShortcutMemberInitializeTest()
        {
            IEnumerable<BasicValidator> members = new BasicValidator[]
            {
                new FhirTypeLabelValidator("Patient"),
                new MaxLengthValidator(10)
            };

            var schema = new ElementSchema(new Canonical("id"), members);
            schema.ShortcutMembers.Should().AllBeOfType<FhirTypeLabelValidator>().And.HaveCount(1);

            schema = new ElementSchema(new Canonical("id"), new BasicValidator[]
            {
                new MaxLengthValidator(10)
            });
            schema.ShortcutMembers.Should().BeEmpty();
        }

        [TestMethod]
        public void ShortcutMemberValidate()
        {
            IEnumerable<BasicValidator> members = new BasicValidator[]
            {
                new FhirTypeLabelValidator("TypeLabel"),
                new MaxLengthValidator(10)
            };

            var schema = new ElementSchema(new Canonical("id"), members);
            var instance = ElementNodeAdapter.Root("NoTypeLabel", "name", "A very long value");
            ResultReport result = schema.Validate(instance, ValidationSettings.BuildMinimalContext());
            result.Errors.Should().OnlyContain(e => e.IssueNumber == Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE.Code,
                                               because: "MaxLength should not be validated");

            instance = ElementNodeAdapter.Root("TypeLabel", "name", "A very long value");
            result = schema.Validate(instance, ValidationSettings.BuildMinimalContext());
            result.Errors.Should().OnlyContain(e => e.IssueNumber == Issue.CONTENT_ELEMENT_VALUE_TOO_LONG.Code);
        }
    }
}