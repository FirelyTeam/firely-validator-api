/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;

#nullable enable

namespace Firely.Fhir.Validation
{


    /// <summary>
    /// This class is used to track which resources/contained resources/bundled resources are already
    /// validated, and what the previous result was. Since it keeps track of running validations, it
    /// can also be used to detect loops.
    /// </summary>
    internal class ValidationLogger
    {
        //TODO: I think this should have a parent logger
        /// <summary>
        /// A class that keeps track of an ongoing or completed validation.
        /// </summary>
        private class ValidationRun
        {
            public ValidationRun(string location, string profileUrl)
            {
                Location = location;
                ProfileUrl = profileUrl;
                Result = null;
            }

            /// <summary>
            /// The location where validation started, as a path, possibly prefixed by the
            /// absolute url of the instance being validated.
            /// </summary>
            public string Location { get; set; }

            /// <summary>
            /// The profile used for this validation
            /// </summary>
            public string ProfileUrl { get; set; }

            /// <summary>
            /// If null, validation is still ongoing and there is no result yet.
            /// </summary>
            public ValidationResult? Result { get; set; }

            public override string ToString() =>
                Result switch
                {
                    null => $"Validation of {Location} against {ProfileUrl} still running.",
                    var r => $"Validation of {Location} against {ProfileUrl} resulted in {r}.",
                };
        }

        private readonly Dictionary<(string location, string profileUrl), ValidationRun> _data = new();

        /// <summary>
        /// Start a fresh validation run for a profile against a given location in an instance.
        /// </summary>
        /// <param name="state">The validation state</param>
        /// <param name="profileUrl">Profile against which we are validating</param>
        /// <param name="validator">Validation to start when it has not been run before.</param>
        /// <returns>The result of calling the validator, or a historic result if there is one.</returns>
        public ResultReport Start(ValidationState state, string profileUrl, Func<ResultReport> validator)
        {
            var resourceUrl = state.Instance.ResourceUrl;
            var fullLocation = (resourceUrl is not null ? resourceUrl + "#" : "") + state.Location.InstanceLocation.ToString();

            var key = (fullLocation, profileUrl);

            if (_data.TryGetValue(key, out var existing))
            {
                if (existing.Result is null)
                    return new IssueAssertion(Issue.CONTENT_REFERENCE_CYCLE_DETECTED,
                     $"Detected a loop: instance data inside '{fullLocation}' refers back to itself.")
                        .AsResult(fullLocation);
                else
                {
                    // If the validation has been run before, return an outcome with the same result.
                    // Note: we don't keep a copy of the original outcome since it has been included in the
                    // total result (at the first run) and keeping them around costs a lot of memory.
                    return new ResultReport(existing.Result.Value, new TraceAssertion(fullLocation, $"Repeated validation at {fullLocation} against profile {profileUrl}."));
                }
            }
            else
            {
                // This validation is run for the first time
                var newEntry = new ValidationRun(fullLocation, profileUrl);
                _data.Add(key, newEntry);

                // Run the validator passed in and indicate its result when finished
                // by updating the run entry.
                var result = validator();
                newEntry.Result = result.Result;

                return result;
            }
        }

        /// <summary>
        /// The number of run or running validations in this logger.
        /// </summary>
        public int Count => _data.Values.Count;
    }
}
