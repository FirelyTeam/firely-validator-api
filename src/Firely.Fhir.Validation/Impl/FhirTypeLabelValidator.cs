﻿/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Assertion about the stated instance type of an element.
    /// </summary>
    /// <remarks>The instance type is taken from <see cref="IBaseElementNavigator{IScopedNode}.InstanceType" /></remarks>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class FhirTypeLabelValidator : BasicValidator
    {
        /// <summary>
        /// The stated instance type.
        /// </summary>
        [DataMember]
        public string Label { get; private set; }

        /// <summary>
        /// Creates the validator for a given type label.
        /// </summary>
        /// <param name="label"></param>
        public FhirTypeLabelValidator(string label)
        {
            Label = label;
        }

        /// <inheritdoc/>
        protected override string Key => "fhir-type-label";

        /// <inheritdoc/>
        protected override object Value => Label;

        internal override ResultReport BasicValidate(IScopedNode input, ValidationSettings vc, ValidationState s)
        {
            var result = input.InstanceType == Label ?
                ResultReport.SUCCESS :
                new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE,
                    $"The declared type of the element ({Label}) is incompatible with that of the instance ({input.InstanceType}).")
                    .AsResult(s);
            //
            return result;
        }
    }
}
