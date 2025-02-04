/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Language;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Firely.Fhir.Validation
{
    internal class ValueElementNode : IScopedNode
    {
        private readonly IScopedNode _wrapped;

        public ValueElementNode(IScopedNode wrapped)
        {
            _wrapped = wrapped;
        }


        public IEnumerable<IScopedNode> Children(string? name = null) => [];

        public bool TryResolveBundleEntry(string fullUrl, [NotNullWhen(true)] out IScopedNode? result)
        {
            result = null;
            return false;
        }

        public bool TryResolveContainedEntry(string id, [NotNullWhen(true)] out IScopedNode? result)
        {
            result = null;
            return false;
        }

        public IScopedNode? Parent { get; }

        public NodeType Type => NodeType.Primitive;

        IEnumerable<ITypedElement> ITypedElement.Children(string? name) => Children(name);

        public string Name => "value";

        public string? InstanceType => (_wrapped.Value is not null) ? TypeSpecifier.ForNativeType(_wrapped.Value.GetType()).FullName : null;

        public object? Value => _wrapped.Value;
        
        public string Location => _wrapped.Location;
        
        public IElementDefinitionSummary? Definition { get; }

        public string ShortPath => _wrapped.ShortPath;
    }
}
