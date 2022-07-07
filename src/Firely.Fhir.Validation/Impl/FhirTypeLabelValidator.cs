﻿/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Assertion about the stated instance type of an element.
    /// </summary>
    /// <remarks>The instance type is taken from <see cref="ITypedElement.InstanceType" /></remarks>
    [DataContract]
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
        public override string Key => "fhir-type-label";

        /// <inheritdoc/>
        public override object Value => Label;

        /// <inheritdoc />
        public override ResultAssertion Validate(ITypedElement input, ValidationContext _, ValidationState __)
        {
            var result = input.InstanceType == Label ?
                ResultAssertion.SUCCESS :
                new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, input.Location, $"Type of instance ({input.InstanceType}) is expected to be {Label}.").AsResult();

            return result;
        }
    }
}
