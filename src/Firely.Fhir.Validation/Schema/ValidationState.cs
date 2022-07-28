/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{

    /// <summary>
    /// Represents thread-safe, shareable state for a single run of the validator.
    /// </summary>
    public record ValidationState
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
            public ValidationLogger RunValidations { get; set; } = new();

            /// <summary>
            /// The total number of resources that have been validated in this run.
            /// </summary>
            /// <remarks>This includes referenced external and contained resources.</remarks>
            public int ResourcesValidated { get; set; } = new();
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

            /// <summary>
            /// This keeps track of all validations done within an instance of a resource.
            /// </summary>
            public ValidationLogger InternalValidations { get; set; } = new();
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
              // States = States
          };
    }
}