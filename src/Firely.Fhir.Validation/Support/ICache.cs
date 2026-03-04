/* 
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Caching interface as a wrapper around a custom caching solution
    /// </summary>
    /// <typeparam name="TKey">Type of the key</typeparam>
    /// <typeparam name="TValue">Type of the value</typeparam>
    public interface ICache<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// Get a value from the cache or add it if it does not exist, atomically
        /// </summary>
        /// <param name="key">Key of the item to retrieve or add</param>
        /// <param name="valueFactory">Factory function to create the value if it does not exist</param>
        /// <returns>The cached or newly added value</returns>
        TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

        /// <summary>
        /// Get a value from the cache or add it if it does not exist, atomically. With a possibility to include a typed argument to avoid a closure object.
        /// </summary>
        /// <param name="key">Key of the item to retrieve or add</param>
        /// <param name="valueFactory">Factory function to create the value if it does not exist</param>
        /// <param name="factoryArg">Argument to use in the factory function. Can be a tuple if needed.</param>
        /// <typeparam name="TArg">Type of the factory argument</typeparam>
        /// <returns>The cached or newly added value</returns>
        TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArg);
    }
}