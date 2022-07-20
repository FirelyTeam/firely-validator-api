/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System;
using System.Collections.Concurrent;

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
            public ValidationLogger ExternalValidations { get; set; } = new();

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
            public string? ExternalUrl { get; set; }

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
              States = States
          };


        //============================== EXISTING STATE CODE - will be replaced ===================

        private ConcurrentDictionary<Type, object> States { get; init; } = new();

        /// <summary>
        /// Returns the state item of type <typeparamref name="T"/>. When the state item does not exist, it will return a new instance of
        /// <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetStateItem<T>() where T : new()
            => (T)(States.GetOrAdd(typeof(T), new T()))!;

        /// <summary>
        /// Creates a new ValidationState where the given item has been inserted or updated.
        /// </summary>
        public ValidationState WithStateItem(object item)
        {
            var newStates = new ConcurrentDictionary<Type, object>(States);
            newStates.AddOrUpdate(item.GetType(), item, (_, _) => item);
            return this with { States = newStates };
        }

        /// <summary>
        /// Creates a new instance of ValidationState
        /// </summary>
        public ValidationState() { }
    }
}