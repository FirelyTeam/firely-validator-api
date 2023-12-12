using Hl7.Fhir.ElementModel;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Support
{
    internal class ScopedNodeExtended : ScopedNode, IScopedNode
    {
        private readonly ScopedNodeExtended? _parent;

        public ScopedNodeExtended(ITypedElement wrapped, string? instanceUri = null) : base(wrapped, instanceUri)
        {
        }

        protected ScopedNodeExtended(ScopedNodeExtended parentNode, ScopedNode? parentResource, ITypedElement wrapped, string? fullUrl) : base(parentNode, parentResource, wrapped, fullUrl)
        {
            _parent = parentNode;
        }

        IScopedNode? IScopedNode.Parent => _parent;

        IEnumerable<IScopedNode> IBaseElementNavigator<IScopedNode>.Children(string? name) =>
            childrenInternal(name);
    }
}
