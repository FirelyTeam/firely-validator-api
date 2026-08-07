/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class WithMembersTests
    {
        private static readonly StructureDefinitionInformation SDINFO =
            new("http://test.org/patientschema", null, "Patient", null, false);

        private static readonly IAssertion NEWMEMBER = new FhirTypeLabelValidator("Patient");

        [TestMethod]
        public void CopyPreservesTheConcreteSchemaType()
        {
            ElementSchema[] schemas =
            [
                new ElementSchema("#anId", ResultAssertion.SUCCESS),
                new ResourceSchema(SDINFO, ResultAssertion.SUCCESS),
                new DatatypeSchema(SDINFO, ResultAssertion.SUCCESS),
                new ExtensionSchema(SDINFO, ResultAssertion.SUCCESS)
            ];

            foreach (var schema in schemas)
            {
                var copy = schema.WithMembers([NEWMEMBER]);

                Assert.AreEqual(schema.GetType(), copy.GetType());
                Assert.AreEqual(schema.Id, copy.Id);
                Assert.AreSame(NEWMEMBER, copy.Members.Single());

                if (schema is FhirSchema original)
                    Assert.AreSame(original.StructureDefinition, ((FhirSchema)copy).StructureDefinition);
            }
        }

        [TestMethod]
        public void CopyRecalculatesShortcutMembers()
        {
            var schema = new ElementSchema("#anId", ResultAssertion.SUCCESS);
            Assert.AreEqual(0, schema.ShortcutMembers.Count);

            // FhirTypeLabelValidator members are extracted as shortcut members by the constructor
            var copy = schema.WithMembers([NEWMEMBER]);
            Assert.AreSame(NEWMEMBER, copy.ShortcutMembers.Single());
        }
    }
}
