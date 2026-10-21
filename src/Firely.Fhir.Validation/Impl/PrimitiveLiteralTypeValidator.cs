/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation;

/// <summary>
/// Reports the SDK's incorrect-literal-type error for a non-string primitive literal; range and format failures are left to other checks.
/// </summary>
[DataContract]
[EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
[System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
public class PrimitiveLiteralTypeValidator : BasicValidator
{
    internal override ResultReport BasicValidate(PocoNode input, ValidationSettings vc, ValidationState state)
    {
        var wrongType = input.GetLiteralTypeError(vc);

        return wrongType is null
            ? ResultReport.SUCCESS
            : new IssueAssertion(Issue.CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE, wrongType.BaseErrorMessage)
                .AsResult(state, input, nameof(PrimitiveLiteralTypeValidator), this);
    }

    /// <inheritdoc />
    protected override string Key => "primitiveLiteralType";

    /// <inheritdoc />
    protected override object Value => new JObject();
}
