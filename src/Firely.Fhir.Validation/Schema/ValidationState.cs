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
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
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