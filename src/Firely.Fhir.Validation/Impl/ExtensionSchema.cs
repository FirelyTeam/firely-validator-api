/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A schema representing a FHIR Extension datatype.
    /// </summary>
    public class ExtensionSchema : FhirSchema
    {
        /// <summary>
        /// Constructs a new <see cref="ExtensionSchema"/>
        /// </summary>
        public ExtensionSchema(StructureDefinitionInformation sdi, params IAssertion[] members) : base(sdi, members)
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ExtensionSchema"/>
        /// </summary>
        public ExtensionSchema(StructureDefinitionInformation sdi, IEnumerable<IAssertion> members) : base(sdi, members)
        {
            // nothing
        }

        /// <inheritdoc />
        protected override Canonical[] GetAdditionalSchemas(ITypedElement instance) =>
           instance
               .Children("url")
               .Select(ite => ite.Value)
               .OfType<string>()
               .Select(s => new Canonical(s))
               .Where(s => s.IsAbsolute)  // don't include relative references in complex extensions
               .ToArray(); // this will actually always be max one...

        /// <inheritdoc/>
        public override ResultReport Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            var evidence = new List<ResultReport>
            {
                validateExtensionCardinality(input, groupLocation, vc, state),
                base.Validate(input, groupLocation, vc, state)
            };

            return ResultReport.FromEvidence(evidence);
        }

        private ResultReport validateExtensionCardinality(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            var evidence = new List<ResultReport>();

            var groups = input.GroupBy(instance => GetAdditionalSchemas(instance).SingleOrDefault());

            if (groups.Any() && vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            foreach (var group in groups)
            {
                if (group.Key is not null)
                {
                    // Resolve the uri to a schema.
                    var schema = vc.ElementSchemaResolver!.GetSchema(group.Key);

                    if (schema is null)
                        return new ResultReport(ValidationResult.Undecided, new IssueAssertion(Issue.UNAVAILABLE_REFERENCED_PROFILE,
                           groupLocation, $"Unable to resolve reference to profile '{group.Key}'."));

                    evidence.AddRange(
                        schema.CardinalityValidators?.Select(c => c.ValidateMany(group, groupLocation, vc, state)) ?? Enumerable.Empty<ResultReport>());
                }
            }

            return ResultReport.FromEvidence(evidence);
        }

        /// <inheritdoc />
        protected override string FhirSchemaKind => "extension";
    }
}
