/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;
using System.Linq;

namespace Firely.Fhir.Validation;

internal static class PrimitiveLiteralHelpers
{
    /// <summary>
    /// Returns the SDK's incorrect-literal-type error when a primitive holds a non-string JSON literal of the wrong type (e.g. a date holding 1990).
    /// </summary>
    public static CodedValidationException? GetLiteralTypeError(this PocoNode input, ValidationSettings vc) =>
        input is PrimitiveNode { Primitive: { JsonValue: not null and not string } p } && !p.HasValidValue()
            ? p.Validate(vc.GetModelInspector(input), validateRecursively: false)
                .FirstOrDefault(e => e.ErrorCode == CodedValidationException.INCORRECT_LITERAL_VALUE_TYPE_CODE)
            : null;
}
