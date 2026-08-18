/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The possible values for the id-expectation marker extension.
    /// </summary>
    public enum IdExpectation
    {
        /// <summary>
        /// The contained/referenced resource may carry an id.
        /// </summary>
        Optional,

        /// <summary>
        /// The contained/referenced resource must carry an id.
        /// </summary>
        Required,

        /// <summary>
        /// The contained/referenced resource must not carry an id.
        /// </summary>
        Prohibited
    }

    /// <summary>
    /// Asserts the expectation on the presence of an id on a contained/referenced resource, as recorded
    /// by the id-expectation extension.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class IdExpectationValidator : BasicValidator
    {
        /// <summary>
        /// The expectation on the presence of an id on the contained/referenced resource.
        /// </summary>
        [DataMember]
        public IdExpectation Expectation { get; private set; }

        /// <summary>
        /// Initializes a new IdExpectationValidator given an expectation.
        /// </summary>
        /// <param name="expectation"></param>
        public IdExpectationValidator(IdExpectation expectation)
        {
            Expectation = expectation;
        }

        /// <inheritdoc />
        protected override string Key => "id-expectation";

        /// <inheritdoc />
        protected override object Value => Expectation.ToString().ToLowerInvariant();

        /// <inheritdoc />
        internal override ResultReport BasicValidate(PocoNode input, ValidationSettings vc, ValidationState s)
        {
            var hasId = input.Child("id") is not null;

            return Expectation switch
            {
                IdExpectation.Required when !hasId =>
                    new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE, "This contained resource is required to carry an 'id', but none was found.")
                        .AsResult(s, input, nameof(IdExpectationValidator), this),
                IdExpectation.Prohibited when hasId =>
                    new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE, "This contained resource must not carry an 'id', but one was found.")
                        .AsResult(s, input, nameof(IdExpectationValidator), this),
                _ => ResultReport.SUCCESS
            };
        }
    }
}
