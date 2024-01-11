using Hl7.Fhir.ElementModel;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal class TypedElementToIScopedNodeToAdapter : IScopedNode
    {
        private readonly ITypedElement _adaptee;

        public TypedElementToIScopedNodeToAdapter(ScopedNode adaptee) : this(adaptee as ITypedElement)
        {
        }

        private TypedElementToIScopedNodeToAdapter(ITypedElement adaptee)
        {
            _adaptee = adaptee;
        }

        public ScopedNode ScopedNode => (ScopedNode)_adaptee; // we know that this is always a ScopedNode

        public string Name => _adaptee.Name;

        public string InstanceType => _adaptee.InstanceType;

        public object Value => _adaptee.Value;

        IEnumerable<IScopedNode> IBaseElementNavigator<IScopedNode>.Children(string? name) =>
            _adaptee.Children(name).Select(n => new TypedElementToIScopedNodeToAdapter(n));
    }
}
