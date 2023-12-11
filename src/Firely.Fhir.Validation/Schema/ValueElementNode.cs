/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
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

        public IScopedNode? Parent => _wrapped.Parent;

        public string Name => "value";

        public string InstanceType => TypeSpecifier.ForNativeType(_wrapped.Value.GetType()).FullName;

        public object Value => _wrapped.Value;

        public IEnumerable<IScopedNode> Children(string? name = null) => Enumerable.Empty<IScopedNode>();
    }
}
