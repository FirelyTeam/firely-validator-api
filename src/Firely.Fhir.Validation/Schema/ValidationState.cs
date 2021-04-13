/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System;
using System.Collections.Concurrent;

namespace Firely.Fhir.Validation
{
    public class ValidationState
    {
        private readonly ConcurrentDictionary<Type, object> _states = new();

        /// <summary>
        /// Returns the state item of type <typeparamref name="T"/>. When the state item does not exist, it will return a new instance of
        /// <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetStateItem<T>() where T : new()
            => (T)(_states.GetOrAdd(typeof(T), new T()))!;


        /// <summary>
        /// Creates a new instance of ValidationState
        /// </summary>
        public ValidationState() { }
    }
}