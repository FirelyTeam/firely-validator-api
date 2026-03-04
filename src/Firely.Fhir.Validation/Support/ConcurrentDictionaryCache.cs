/* 
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;
using System.Collections.Concurrent;

namespace Firely.Fhir.Validation
{
    internal class ConcurrentDictionaryCache<TKey, TValue>(ConcurrentDictionary<TKey, TValue> dictionary)
        : ICache<TKey, TValue>
        where TKey : notnull
    {
        public ConcurrentDictionaryCache() : this(new())
        {
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
            => dictionary.GetOrAdd(key, valueFactory);

        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArg)
            => dictionary.GetOrAdd(key, valueFactory, factoryArg);
    }
}