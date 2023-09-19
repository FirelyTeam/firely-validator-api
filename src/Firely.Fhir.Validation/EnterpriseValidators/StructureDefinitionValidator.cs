using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Support;
using Hl7.FhirPath.Sprache;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An <see cref="ElementSchema"/> that represents a FHIR StructureDefinition
    /// </summary>    
    [DataContract]
    public class StructureDefinitionValidator : IValidatable
    {
        /// <inheritdoc/>
        public JToken ToJson() => new JProperty("elementDefinition", new JObject());

        /// <inheritdoc/>
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            //this can be expanded with other validate functionality
            var evidence = validateInvariantUniqueness(input, state).ToList();

            return ResultReport.FromEvidence(evidence);
        }

        /// <summary>
        /// Validates if the invariants defined in the snapshot and differentials have a unique key. 
        /// Duplicate keys can exist when the element paths are also the same (e.g. in slices).
        /// </summary>
        /// <param name="input"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static IEnumerable<ResultReport> validateInvariantUniqueness(ITypedElement input, ValidationState state)
        {
            var snapshotElements = input.Children("snapshot").SelectMany(c => c.Children("element"));
            var diffElements = input.Children("differential").SelectMany(c => c.Children("element"));

            var snapshotEvidence = validateInvariantUniqueness(snapshotElements);
            var diffEvidence = validateInvariantUniqueness(diffElements);

            return snapshotEvidence.Concat(diffEvidence).Select(i => i.AsResult(input, state));
        }

        private static List<IssueAssertion> validateInvariantUniqueness(IEnumerable<ITypedElement> elements)
        {
            //Selects the combination of key and elementDefintion path for the duplicate keys where the paths are not also the same.

            IEnumerable<(string Key, string Path)> PathsPerInvariantKey = elements
                                     .SelectMany(e => e.Children("constraint")
                                                       .Select(c => (Key: c.Children("key")
                                                                           .Single().Value.ToString(),
                                                                    Path: e.Children("path")
                                                                           .Single().Value.ToString())));

            IEnumerable<(string Key, IEnumerable<string> Paths)> PathsPerDuplicateInvariantKey = PathsPerInvariantKey.GroupBy(pair => pair.Key)
                                                                                     .Select(group => (Key: group.Key, Paths: group.Select(pair => pair.Path) // select all paths, per invariant key
                                                                                                            .Distinct())) //Distinct to remove paths that are encountered multiple times per invariant
                                                                                     .Where(kv => kv.Paths.Count() > 1); //Remove entries that only have a single path. These are not incorrect.

            return PathsPerDuplicateInvariantKey.Select(c => new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INCORRECT, $"Duplicate key '{c.Key}' in paths: {string.Join(", ", c.Paths)}")).ToList();
        }
    }
}

#nullable restore
