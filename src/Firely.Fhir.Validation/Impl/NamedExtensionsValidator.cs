/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Marks a logical-model element as a named-elements extension carrier (per the
    /// <c>extension-style</c> extension) - e.g. CDS Hooks' <c>fhirAuthorization.extension</c>, whose
    /// named properties (<c>davinci-crd.version</c>, etc.) are named extensions rather than a FHIR
    /// array of <c>Extension</c>+<c>url</c>.
    /// </summary>
    /// <remarks>
    /// A named extension's JSON name is resolved to its defining <c>StructureDefinition</c> by
    /// passing it straight into <see cref="ValidationSettings.ConformanceResourceResolver"/> as if it
    /// were a canonical - the resolver is expected to maintain a cache mapping those names to the
    /// actual profiles. The resulting profile's schema is then used to validate the extension's value.
    /// </remarks>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class NamedExtensionsValidator : IValidatable
    {
        /// <summary>
        /// The names of the children of this element that are declared in the profile - any other
        /// child found on the instance is considered a named extension.
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<string> KnownChildNames { get; private set; }

        /// <summary>
        /// Initializes a new NamedExtensionsValidator.
        /// </summary>
        public NamedExtensionsValidator(IEnumerable<string> knownChildNames)
        {
            KnownChildNames = knownChildNames?.ToArray() ?? throw new ArgumentNullException(nameof(knownChildNames));
        }

        /// <inheritdoc />
        public JToken ToJson() => new JProperty("named-extensions", true);

        /// <inheritdoc />
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState state)
        {
            if (vc.ConformanceResourceResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationSettings)} does not contain a ConformanceResourceResolver.");

            var namedExtensions = input.Children().Where(c => !KnownChildNames.Contains(c.Name)).SelectMany(c => c).ToList();

            return ResultReport.Combine([ ..namedExtensions.Select(child => validate(child, vc, state)) ]);
        }

        private ResultReport validate(PocoNode child, ValidationSettings vc, ValidationState state)
        {
            var result = TaskHelper.Await(() => vc.ConformanceResourceResolver!.TryResolveByCanonicalUriAsync(child.Name));
            if (!result.Success || result.Value is not IConformanceResource {Url: {} url } sd)
            {
                return new IssueAssertion(Issue.UNAVAILABLE_REFERENCED_PROFILE,
                    $"Cannot resolve named extension '{child.Name}' to a StructureDefinition.")
                    .AsResult(state, child, nameof(NamedExtensionsValidator), this);
            }

            return new SchemaReferenceValidator(url).ValidateOne(child, vc, state);
        }
    }
}
