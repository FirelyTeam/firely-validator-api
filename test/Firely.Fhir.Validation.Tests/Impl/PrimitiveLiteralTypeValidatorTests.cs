/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
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

namespace Firely.Fhir.Validation.Tests;

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
    public void Fails_when_boolean_literal_is_the_string_true() =>
        BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new FhirBoolean { JsonValue = "true" }), false,
            Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "a JSON string is not a valid boolean literal, even when it reads as one");

    [TestMethod]
    public void Fails_when_integer_literal_is_a_numeric_string() =>
        BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new Integer { JsonValue = "2" }), false,
            Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "a JSON string is not a valid integer literal");

    [TestMethod]
    public void Passes_when_decimal_string_overflows() =>
        BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new FhirDecimal { JsonValue = "-1.000000000000000000E+245" }), true, null,
            "a JSON number too big for System.Decimal is a range problem, left to the regex validator");

    [TestMethod]
    public void Fails_when_decimal_literal_is_a_numeric_string() =>
        BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new FhirDecimal { JsonValue = "1.5" }), false,
            Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "a JSON string is not a valid decimal literal");

    [TestMethod]
    public void Fails_when_boolean_string_is_not_true_or_false() =>
        BasicValidatorTestcases(_validator, PocoNode.ForPrimitive(new FhirBoolean { JsonValue = "yes" }), false,
            Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "any string left in a boolean is the wrong literal type");

    [TestMethod]
    public void Serializes_the_format_flag_when_set() =>
        new PrimitiveLiteralTypeValidator(validateFormat: true).ToJson().ToString().Should().Contain("\"formatCheck\": true");

    [TestMethod]
    public void Fails_on_malformed_boolean_string_when_validating_format() =>
        BasicValidatorTestcases(new PrimitiveLiteralTypeValidator(validateFormat: true), PocoNode.ForPrimitive(new FhirBoolean { JsonValue = "yes" }), false,
            Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "without a regex, malformed strings are this validator's concern");

    [TestMethod]
    public void Fails_on_numeric_boolean_when_validating_format() =>
        BasicValidatorTestcases(new PrimitiveLiteralTypeValidator(validateFormat: true), PocoNode.ForPrimitive(new FhirBoolean { JsonValue = 1 }), false,
            Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, "wrong literal types are still reported");

    [TestMethod]
    public void Passes_on_valid_boolean_when_validating_format() =>
        BasicValidatorTestcases(new PrimitiveLiteralTypeValidator(validateFormat: true), PocoNode.ForPrimitive(new FhirBoolean { JsonValue = true }), true, null,
            "a true literal is accepted");

    [TestMethod]
    public void Does_not_rewrite_the_literal_it_validates()
    {
        var base64 = new Base64Binary { JsonValue = "PFBh cmFt" };
        new PrimitiveLiteralTypeValidator(validateFormat: true).Validate(PocoNode.ForPrimitive(base64), ValidationSettings.BuildMinimalContext());
        base64.JsonValue.Should().Be("PFBh cmFt");
    }
}
