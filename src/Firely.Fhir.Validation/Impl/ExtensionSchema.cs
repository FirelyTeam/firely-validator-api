﻿/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A schema representing a FHIR Extension datatype.
    /// </summary>
    [DataContract]
    internal class ExtensionSchema : FhirSchema
    {
        /// <summary>
        /// Constructs a new <see cref="ExtensionSchema"/>
        /// </summary>
        public ExtensionSchema(StructureDefinitionInformation structureDefinition, params IAssertion[] members) : base(structureDefinition, members)
        {
            // nothing
        }

        /// <summary>
        /// Constructs a new <see cref="ExtensionSchema"/>
        /// </summary>
        public ExtensionSchema(StructureDefinitionInformation structureDefinition, IEnumerable<IAssertion> members) : base(structureDefinition, members)
        {
            // nothing
        }

        /// <summary>
        /// Gets the canonical of the profile referred to in the <c>url</c> property of the extension.
        /// </summary>
        public static Canonical? GetExtensionUri(IScopedNode instance) =>
            instance
                .Children("url")
                .Select(ite => ite.Value)
                .OfType<string>()
                .Select(s => new Canonical(s))
                .Where(s => s.IsAbsolute)  // don't include relative references in complex extensions
                .FirstOrDefault(); // this will actually always be max one, but that's validated by a cardinality validator.

        /// <inheritdoc/>
        public override ResultReport Validate(IEnumerable<IScopedNode> input, ValidationContext vc, ValidationState state)
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

                    var extensionHandling = callback(vc.FollowExtensionUrl).Invoke(state.Location.InstanceLocation.ToString(), group.Key);

                    if (extensionHandling is ExtensionUrlHandling.DontResolve)
                    {
                        // Just validate the Extension schema itself.
                        evidence.Add(ValidateExtensionSchema(group, vc, state));
                    }
                    else
                    {
                        // Resolve the uri to a schema only when instructed
                        var validator = vc.ElementSchemaResolver!.GetSchema(group.Key);

                        if (validator is null)
                        {
                            var isModifierExtension = group.First().Name == "modifierExtension";

                            var (vr, issue) = extensionHandling switch
                            {
                                _ when isModifierExtension => (ValidationResult.Failure, Issue.UNAVAILABLE_REFERENCED_PROFILE),
                                ExtensionUrlHandling.WarnIfMissing => (ValidationResult.Success, Issue.UNAVAILABLE_REFERENCED_PROFILE_WARNING),
                                ExtensionUrlHandling.ErrorIfMissing => (ValidationResult.Failure, Issue.UNAVAILABLE_REFERENCED_PROFILE),
                                _ => (ValidationResult.Undecided, Issue.UNAVAILABLE_REFERENCED_PROFILE) // this case will never happen
                            };

                            evidence.Add(new ResultReport(vr,
                                new IssueAssertion(issue, $"Unable to resolve reference to extension '{group.Key}'.")
                                    .AsResult(state).Evidence));

                            // No url available - validate the Extension schema itself.
                            evidence.Add(ValidateExtensionSchema(group, vc, state));
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
                            evidence.Add(schema.ValidateExtensionSchema(group, vc, state));
                        }
                    }
                }
                else
                {
                    // No url available - validate the Extension schema itself.
                    evidence.Add(ValidateExtensionSchema(group, vc, state));
                }
            }

            return ResultReport.Combine(evidence);

            static ExtensionUrlFollower callback(ExtensionUrlFollower? follower) =>
                follower ?? ((l, c) => ExtensionUrlHandling.WarnIfMissing);
        }

        /// <summary>
        /// This invokes the actual validation for an Extension schema, without the special magic of 
        /// fetching the url, so this is the "normal" schema validation.
        /// </summary>
        protected ResultReport ValidateExtensionSchema(IEnumerable<IScopedNode> input,
            ValidationContext vc,
            ValidationState state) => base.Validate(input, vc, state);

        /// <inheritdoc/>
        public override ResultReport Validate(IScopedNode input, ValidationContext vc, ValidationState state) =>
            Validate(new[] { input }, vc, state);


        /// <inheritdoc />
        protected override string FhirSchemaKind => "extension";
    }
}
