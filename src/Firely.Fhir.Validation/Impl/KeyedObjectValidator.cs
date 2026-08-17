/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Validates a repeating logical-model element that is rendered as a JSON object keyed by an
    /// arbitrary map key (per the <c>json-property-key</c> extension), rather than as a JSON array -
    /// e.g. CDS Hooks' <c>prefetch</c> (keyed by prefetch key) or Da Vinci CRD's flat
    /// <c>davinci-crd.configuration</c> code -> value map.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class KeyedObjectValidator : IValidatable
    {
        /// <summary>
        /// The assertion each entry (JSON property value) of the keyed object is validated against.
        /// </summary>
        [DataMember]
        public IAssertion EntryAssertion { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="KeyedObjectValidator"/> given the assertion each entry should be
        /// validated against.
        /// </summary>
        public KeyedObjectValidator(IAssertion entryAssertion)
        {
            EntryAssertion = entryAssertion ?? throw new ArgumentNullException(nameof(entryAssertion));
        }

        /// <inheritdoc />
        public JToken ToJson() =>
            new JProperty("keyed-object", EntryAssertion.ToJson().MakeNestedProp());

        /// <inheritdoc />
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState state)
        {
            // Each JSON property of the keyed object is a single map entry (not a repeating group),
            // so flatten the named PocoNodeOrList groups into individual entry nodes.
            var entries = input.Children().SelectMany(c => c).ToList();

            var evidence = entries.Select(entry =>
                EntryAssertion.ValidateOne(entry, vc, state.UpdateLocation(vs => vs.ToChild(entry.Name))));

            return ResultReport.Combine(evidence.ToList());
        }
    }
}
