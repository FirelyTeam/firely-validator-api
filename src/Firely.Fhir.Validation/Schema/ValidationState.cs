/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.FhirPath;
using System;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents thread-safe, shareable state for a single run of the validator.
    /// </summary>
    internal record ValidationState
    {
        /// <summary>
        /// A container for state properties that are shared across a full run of the validator.
        /// </summary>
        public class GlobalState
        {
            /// <summary>
            /// This keeps track of all validations done on external resources
            /// referenced from the original root resource passed to the Validate() call.
            /// </summary>
            public ValidationLogger RunValidations { get; private set; } = new();

            /// <summary>
            /// The total number of resources that have been validated in this run.
            /// </summary>
            /// <remarks>This includes referenced external and contained resources.</remarks>
            public int ResourcesValidated { get; set; } = new();

            /// <summary>
            /// A cache of compiled FhirPath expressions used in <see cref="PathSelectorValidator"/>
            /// </summary>
            public FhirPathCompilerCache? FPCompilerCache { get; internal set; }
        }

        /// <summary>
        /// State to be kept for one full run of the validator.
        /// </summary>
        public GlobalState Global { get; private set; } = new();

        /// <summary>
        /// A container for state to be kept for an instance encountered during validation
        /// (if the resource under validation references an external resource,
        /// the validation of that resource will have its own <see cref="InstanceState"/>.
        /// </summary>
        public class InstanceState
        {
            /// <summary>
            /// The URL where the current instance was retrieved (if known).
            /// </summary>
            public string? ResourceUrl { get; set; }
        }

        /// <summary>
        /// State to be kept while validating a single instance.
        /// </summary>
        public InstanceState Instance { get; private set; } = new();

        /// <summary>
        /// Create a state with a clean instance container (<see cref="Instance"/>).
        /// </summary>
        public ValidationState NewInstanceScope() =>
          new()
          {
              // Global data is shared across ValidationState instances.
              Global = Global,
          };

        /// <summary>
        /// A container for state to be kept for a given element + its definition during validation.
        /// Currently, we are just keeping a reference to the definition, but we are planning to keep
        /// the instance location as well.
        /// </summary>
        public class LocationState
        {
            /// <summary>
            /// The path to the definition for the current location
            /// </summary>
            public DefinitionPath DefinitionPath { get; set; } = DefinitionPath.Start();

            /// <summary>
            /// 
            /// </summary>
            public InstancePath InstanceLocation { get; set; } = InstancePath.Start();
        }

        /// <summary>
        /// State to be kept while validating at the same location in the instance and definition
        /// </summary>
        public LocationState Location { get; private set; } = new();

        /// <summary>
        /// Update the location, returning a new state with the updated location.
        /// </summary>
        public ValidationState UpdateLocation(Func<DefinitionPath, DefinitionPath> pathStackUpdate) =>
            new()
            {
                Global = Global,
                Instance = Instance,
                Location = new LocationState
                {
                    DefinitionPath = pathStackUpdate(Location.DefinitionPath),
                    InstanceLocation = Location.InstanceLocation // is this correct
                }
            };
        internal ValidationState UpdateInstanceLocation(Func<InstancePath, InstancePath> pathStackUpdate) =>
            new()
            {
                Global = Global,
                Instance = Instance,
                Location = new LocationState
                {
                    DefinitionPath = Location.DefinitionPath, // is this correct?
                    InstanceLocation = pathStackUpdate(Location.InstanceLocation)
                }
            };
    }
}