/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using P = Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Validation;
using System.Linq;

namespace Firely.Fhir.Validation;

internal static class PrimitiveLiteralHelpers
{
    /// <summary>
    /// Returns the SDK's incorrect-literal-type error when a primitive holds a JSON literal of the wrong type
    /// (e.g. a date holding 1990, or a boolean holding the string "true").
    /// </summary>
    public static CodedValidationException? GetLiteralTypeError(this PocoNode input, ValidationSettings vc) =>
        input is PrimitiveNode { Primitive: { JsonValue: { } value } p } && (value is not string s || isWrongTypeString(p, s)) && !p.HasValidValue()
            ? p.Validate(vc.GetModelInspector(input), validateRecursively: false)
                .FirstOrDefault(e => e.ErrorCode == CodedValidationException.INCORRECT_LITERAL_VALUE_TYPE_CODE)
            : null;

    // Only these types have a non-string JSON literal; for all others a string is the right type, so there's nothing to check.
    // It also keeps HasValidValue() away from Base64Binary, whose validation rewrites the literal
    // (https://github.com/FirelyTeam/firely-net-sdk/issues/3648); revisit this filter once that is fixed.
    private static bool isWrongTypeString(PrimitiveType primitive, string literal) => primitive switch
    {
        FhirBoolean or Integer or PositiveInt or UnsignedInt => true,
        // The JSON reader keeps a number too big for System.Decimal as a string; that's out of range, which the regex reports.
        FhirDecimal => P.Decimal.TryParse(literal, out _),
        _ => false
    };
}
