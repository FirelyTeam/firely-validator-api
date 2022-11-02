/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    public class ValueRod : ROD
    {
        public ValueRod(object primitiveValue)
        {
            PrimitiveValue = primitiveValue;
        }

        public object this[string key] => key == "value" ? PrimitiveValue : throw new KeyNotFoundException();

        public string Location => "@@value@@";

        public Canonical InstanceType => throw new NotImplementedException("Should be one of the System types");

        public IEnumerable<string> Keys => Enumerable.Repeat("value", 1);

        public IEnumerable<object> Values => Enumerable.Repeat(PrimitiveValue, 1);

        public int Count => 1;

        public object PrimitiveValue { get; }

        public string Name => "value";

        public bool ContainsKey(string key) => key == "value";
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => throw new NotImplementedException();
        public bool TryGetValue(string key, out object value) => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}