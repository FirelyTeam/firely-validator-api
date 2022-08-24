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
using static Firely.Fhir.Validation.ValidationContext;

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

        /// <summary>
        /// Gets the canonical of the profile referred to in the <c>url</c> property of the extension.
        /// </summary>
        public static Canonical? GetExtensionUri(ITypedElement instance) =>
            instance
                .Children("url")
                .Select(ite => ite.Value)
                .OfType<string>()
                .Select(s => new Canonical(s))
                .Where(s => s.IsAbsolute)  // don't include relative references in complex extensions
                .FirstOrDefault(); // this will actually always be max one, but that's validated by a cardinality validator.

        /// <inheritdoc/>
        public override ResultReport Validate(IEnumerable<ITypedElement> input, string groupLocation, ValidationContext vc, ValidationState state)
        {
            // Group the instances by their url - this allows a IGroupValidatable schema for the 
            // extension to validate the "extension cardinality".
            var groups = input.GroupBy(instance => GetExtensionUri(instance)).ToArray();

            if (groups.Any() && vc.ElementSchemaResolver is null)
                throw new ArgumentException($"Cannot validate the extension because {nameof(ValidationContext)} does not contain an ElementSchemaResolver.");

            var evidence = new List<ResultReport>();

            foreach (var group in groups)
            {
                if (group.Key is not null)
                {
                    var extensionHandling = callback(vc).Invoke(groupLocation, group.Key);

                    // Resolve the uri to a schema only when instructed
                    var validator = extensionHandling is not ExtensionUrlHandling.DontResolve ? vc.ElementSchemaResolver!.GetSchema(group.Key) : null;

                    if (validator is null)
                    {
                        var isModifierExtension = group.First().Name == "modifierExtension";

                        var (vr, issue) = extensionHandling switch
                        {
                            _ when isModifierExtension => (ValidationResult.Failure, Issue.UNAVAILABLE_REFERENCED_PROFILE),
                            ExtensionUrlHandling.WarnIfMissing => (ValidationResult.Undecided, Issue.UNAVAILABLE_REFERENCED_PROFILE_WARNING),
                            ExtensionUrlHandling.ErrorIfMissing => (ValidationResult.Undecided, Issue.UNAVAILABLE_REFERENCED_PROFILE),
                            _ => (ValidationResult.Undecided, Issue.UNAVAILABLE_REFERENCED_PROFILE) // this case will never happen
                        };

                        evidence.Add(new ResultReport(vr, new IssueAssertion(issue, groupLocation,
                            $"Unable to resolve reference to extension '{group.Key}'.")));

                        // No url available - validate the Extension schema itself.
                        evidence.Add(ValidateExtensionSchema(group, groupLocation, vc, state));
                    }
                    else
                    {
                        var schema = validator switch
                        {
                            ExtensionSchema es => es,
                            var other => throw new InvalidOperationException($"The schema returned for an extension should be of type {nameof(ExtensionSchema)}, not {other.GetType()}.")
                        };

                        // Now that we have fetched the extension, call its constraint validation - this should exclude the
                        // special fetch magic for the url (this function) to avoid a loop, so we call the actual validation here.
                        evidence.Add(schema.ValidateExtensionSchema(group, groupLocation, vc, state));
                    }
                }
                else
                {
                    // No url available - validate the Extension schema itself.
                    evidence.Add(ValidateExtensionSchema(group, groupLocation, vc, state));
                }
            }

            return ResultReport.FromEvidence(evidence);

            static Func<string, Canonical?, ExtensionUrlHandling> callback(ValidationContext context) =>
                context.FollowExtensionUrl ?? ((l, c) => ExtensionUrlHandling.WarnIfMissing);
        }

        /// <summary>
        /// This invokes the actual validation for an Extension schema, without the special magic of 
        /// fetching the url, so this is the "normal" schema validation.
        /// </summary>
        protected ResultReport ValidateExtensionSchema(IEnumerable<ITypedElement> input,
            string groupLocation, ValidationContext vc,
            ValidationState state) => base.Validate(input, groupLocation, vc, state);

        /// <inheritdoc/>
        public override ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state) =>
            Validate(new[] { input }, input.Location, vc, state);


        /// <inheritdoc />
        protected override string FhirSchemaKind => "extension";
    }
}
