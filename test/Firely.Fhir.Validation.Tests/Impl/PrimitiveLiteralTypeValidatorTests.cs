/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class PrimitiveLiteralTypeValidatorTests : BasicValidatorTests
    {
        private readonly PrimitiveLiteralTypeValidator _validator = new();

        [TestMethod]
        public void Serializes_under_the_primitiveLiteralType_key() =>
            _validator.ToJson().ToString().Should().Contain("\"primitiveLiteralType\"");

        [TestMethod]
        public void Fails_when_date_literal_is_a_number() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new Date { JsonValue = 1990 }), false,
                Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "a numeric literal is not a valid date");

        [TestMethod]
        public void Fails_when_boolean_literal_is_a_number() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new FhirBoolean { JsonValue = 1 }), false,
                Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "a numeric literal is not a valid boolean");

        [TestMethod]
        public void Fails_when_integer_literal_is_a_decimal() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new Integer { JsonValue = 5.5m }), false,
                Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "a decimal literal is not a valid integer");

        [TestMethod]
        public void Passes_when_date_literal_is_a_string() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new Date { JsonValue = "1990" }), true, null,
                "a string date literal is accepted");

        [TestMethod]
        public void Passes_when_boolean_literal_is_a_boolean() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new FhirBoolean { JsonValue = true }), true, null,
                "a true literal is accepted");

        [TestMethod]
        public void Passes_when_integer_literal_is_an_integer() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new Integer { JsonValue = 5 }), true, null,
                "an integer literal is accepted");

        [TestMethod]
        public void Passes_when_literal_is_absent_but_extension_present()
        {
            var date = new Date();
            date.SetExtension("http://hl7.org/fhir/StructureDefinition/data-absent-reason", new Code("unknown"));
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(date), true, null, "an absent literal is not this validator's concern");
        }

        [TestMethod]
        public void Passes_when_positiveInt_literal_is_an_out_of_range_integer() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new PositiveInt { JsonValue = 0 }), true, null,
                "range failures belong to the regex validator");

        [TestMethod]
        public void Passes_when_unsignedInt_literal_is_an_out_of_range_integer() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new UnsignedInt { JsonValue = -1 }), true, null,
                "range failures belong to the regex validator");

        [TestMethod]
        public void Passes_when_date_string_is_malformed() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new Date { JsonValue = "1990-13-01" }), true, null,
                "malformed strings are left to the regex validator");

        [TestMethod]
        public void Passes_when_boolean_string_is_not_true_or_false() =>
            BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new FhirBoolean { JsonValue = "yes" }), true, null,
                "malformed strings are left to the regex validator");
    }
}
