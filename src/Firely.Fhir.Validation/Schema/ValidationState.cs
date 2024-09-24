/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.FhirPath;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Transactions;


namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents thread-safe, shareable state for a single run of the validator.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public record ValidationState
    {
        /// <summary>
        /// A container for state properties that are shared across a full run of the validator.
        /// </summary>
        internal class GlobalState
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
        internal GlobalState Global { get; private set; } = new();

        /// <summary>
        /// A container for state to be kept for an instance encountered during validation
        /// (if the resource under validation references an external resource,
        /// the validation of that resource will have its own <see cref="InstanceState"/>.
        /// </summary>
        internal record InstanceState
        {
            /// <summary>
            /// The URL where the current instance was retrieved (if known).
            /// </summary>
            public string? ResourceUrl { get; set; }
        }

        internal ValidationState? Parent { get; private set; }

        internal IEnumerable<ValidationState> Parents()
        {
            var current = this;
            while (current.Parent != null)
            {
                yield return current.Parent;
                current = current.Parent;
            }
        }

        /// <summary>
        /// State to be kept while validating a single instance.
        /// </summary>
        internal InstanceState Instance { get; private set; } = new();

        /// <summary>
        /// Create a state with a clean instance container (<see cref="Instance"/>).
        /// </summary>
        internal ValidationState NewInstanceScope() =>
          new()
          {
              // Global data is shared across ValidationState instances.
              Global = Global,
              Parent = this
          };

        /// <summary>
        /// A container for state to be kept for a given element + its definition during validation.
        /// Currently, we are just keeping a reference to the definition, but we are planning to keep
        /// the instance location as well.
        /// </summary>
        internal class LocationState
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
        internal LocationState Location { get; private set; } = new();

        /// <summary>
        /// Update the location, returning a new state with the updated location.
        /// </summary>
        internal ValidationState UpdateLocation(Func<DefinitionPath, DefinitionPath> pathStackUpdate) =>
            this with {Location = new LocationState { DefinitionPath = pathStackUpdate(Location.DefinitionPath), InstanceLocation = Location.InstanceLocation }};
        
        internal ValidationState UpdateInstanceLocation(Func<InstancePath, InstancePath> pathStackUpdate) =>
            this with {Location = new LocationState { DefinitionPath = Location.DefinitionPath, InstanceLocation = pathStackUpdate(Location.InstanceLocation) }};
    }
}