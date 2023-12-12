using Hl7.Fhir.ElementModel;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal class TypedElementToIScopedNodeToAdapter : IScopedNode
    {
        private readonly ScopedNode _adaptee;

        public TypedElementToIScopedNodeToAdapter(ScopedNode adaptee)
        {
            _adaptee = adaptee;
        }

        public ScopedNode ScopedNode => _adaptee;

        public string Name => _adaptee.Name;

        public string InstanceType => _adaptee.InstanceType;

        public object Value => _adaptee.Value;

        IEnumerable<IScopedNode> IBaseElementNavigator<IScopedNode>.Children(string? name) =>
            _adaptee.Children(name).Cast<ScopedNode>().Select(n => new TypedElementToIScopedNodeToAdapter(n));
    }
}
