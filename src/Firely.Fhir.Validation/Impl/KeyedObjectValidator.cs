/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
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
    public class KeyedObjectValidator : IValidatable, IAssertionContainer
    {
        /// <summary>
        /// The assertion each entry (JSON property value) of the keyed object is validated against.
        /// </summary>
        [DataMember]
        public IAssertion EntryAssertion { get; private set; }

        /// <summary>
        /// Lower bound on the number of entries (JSON properties) of the keyed object. If not set,
        /// there is no lower bound.
        /// </summary>
        /// <remarks>This is the cardinality declared on the logical element itself: since the element is
        /// rendered as a single JSON object rather than as an array, its cardinality constrains the map
        /// entries, not the number of occurrences of the object.</remarks>
        [DataMember]
        public int? Min { get; private set; }

        /// <summary>
        /// Upper bound on the number of entries (JSON properties) of the keyed object. If not set,
        /// there is no upper bound.
        /// </summary>
        /// <remarks>See the remarks on <see cref="Min"/>.</remarks>
        [DataMember]
        public int? Max { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="KeyedObjectValidator"/> given the assertion each entry should be
        /// validated against, and the cardinality the entries should adhere to.
        /// </summary>
        public KeyedObjectValidator(IAssertion entryAssertion, int? min = null, int? max = null)
        {
            EntryAssertion = entryAssertion ?? throw new ArgumentNullException(nameof(entryAssertion));

            if (min < 0 || max < 0)
                throw new IncorrectElementDefinitionException("Cardinality cannot be lower than 0.");
            if (min > max)
                throw new IncorrectElementDefinitionException("Upper cardinality must be higher than the lower cardinality.");

            Min = min;
            Max = max;
        }

        /// <inheritdoc cref="IAssertionContainer.WithChildren(Func{AssertionStep, IAssertion, IAssertion})"/>
        IAssertion IAssertionContainer.WithChildren(Func<AssertionStep, IAssertion, IAssertion> rewrite)
        {
            var entryAssertion = rewrite(AssertionStep.Member(), EntryAssertion);

            return ReferenceEquals(entryAssertion, EntryAssertion)
                ? this
                : new KeyedObjectValidator(entryAssertion, Min, Max);
        }

        /// <inheritdoc />
        public JToken ToJson() =>
            new JProperty("keyed-object", new JObject(
                new JProperty("cardinality", CardinalityDisplay),
                new JProperty("entry", EntryAssertion.ToJson().MakeNestedProp())));

        private bool inRange(int x) => (!Min.HasValue || x >= Min.Value) && (!Max.HasValue || x <= Max.Value);

        private string CardinalityDisplay => $"{Min?.ToString() ?? "<-"}..{Max?.ToString() ?? "*"}";

        /// <inheritdoc />
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState state)
        {
            // Each JSON property of the keyed object is a single map entry (not a repeating group),
            // so flatten the named PocoNodeOrList groups into individual entry nodes.
            var entries = input.Children().SelectMany(c => c).ToList();

            var evidence = entries.Select(entry =>
                EntryAssertion.ValidateOne(entry, vc, state.UpdateLocation(vs => vs.ToChild(entry.Name)))).ToList();

            // The declared cardinality of the logical element applies to the map entries: the element
            // itself always occurs exactly once (as the JSON object container), so the normal
            // CardinalityValidator is not built for this representation and the check happens here.
            if (!inRange(entries.Count))
                evidence.Add(new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE,
                        $"Instance count at element '{input.Name}' is {entries.Count}, which is not within the specified cardinality of {CardinalityDisplay}")
                    .AsResult(state, input, nameof(KeyedObjectValidator), this));

            return ResultReport.Combine(evidence);
        }
    }
}
