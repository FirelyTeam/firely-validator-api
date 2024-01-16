/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Language;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal class ValueElementNode : IScopedNode
    {
        private readonly IScopedNode _wrapped;

        public ValueElementNode(IScopedNode wrapped)
        {
            _wrapped = wrapped;
        }

        public string Name => "value";

        public string InstanceType => TypeSpecifier.ForNativeType(_wrapped.Value.GetType()).FullName;

        public object Value => _wrapped.Value;

        public IEnumerable<IScopedNode> Children(string? name = null) => Enumerable.Empty<IScopedNode>();
    }
}
