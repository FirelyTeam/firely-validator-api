/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Records that a JSON null value is a legitimate value for this element, as recorded
    /// by the json-nullable extension.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class JsonNullableValidator : BasicValidator
    {
        /// <summary>
        /// Whether a JSON null value is a legitimate value for this element.
        /// </summary>
        [DataMember]
        public bool IsNullable { get; private set; }

        /// <summary>
        /// Initializes a new JsonNullableValidator given a nullable flag.
        /// </summary>
        /// <param name="isNullable"></param>
        public JsonNullableValidator(bool isNullable)
        {
            IsNullable = isNullable;
        }

        /// <inheritdoc />
        protected override string Key => "json-nullable";

        /// <inheritdoc />
        protected override object Value => IsNullable;

        /// <inheritdoc />
        internal override ResultReport BasicValidate(PocoNode input, ValidationSettings vc, ValidationState s) =>
            // Documentation/schema only marker: JSON null never yields a child node to validate against per FHIR.
            ResultReport.SUCCESS;
    }
}
