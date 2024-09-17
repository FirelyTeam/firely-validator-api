/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.RegularExpressions;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class FhirUriValidatorTests : BasicValidatorTests
    {
        [TestMethod]
        public void TestFhirUriValidation()
        {
            var validator = new FhirUriValidator();

            base.BasicValidatorTestcases(validator, ElementNode.ForPrimitive("http://hl7.org/fhir"), true, null, "result must be true");
            base.BasicValidatorTestcases(validator, ElementNode.ForPrimitive("http://hl7.org/fhir/StructureDefinition/regex"), true, null, "result must be true");
            base.BasicValidatorTestcases(validator, ElementNode.ForPrimitive("http://hl7.org/fhir/StructureDefinition/regex#"), true, null, "result must be true");
            base.BasicValidatorTestcases(validator, ElementNode.ForPrimitive("http://hl7.org/fhir/StructureDefinition/regex#regex"), true, null, "result must be true");
            base.BasicValidatorTestcases(validator, ElementNode.ForPrimitive("urn:oid:4.4.5"), false, Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "result must be false");
            base.BasicValidatorTestcases(validator, ElementNode.ForPrimitive("urn:uuid:f3b2bd36-199b-4591-b4db-f49db0912b64"), true, null, "result must be true");
        }
    }
}