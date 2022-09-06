/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    public class DefinitionPath
    {
        private DefinitionPath(DefinitionPathEvent? current) => _current = current;

        private readonly DefinitionPathEvent? _current;

        public override string ToString()
        {
            return render(_current);

            static string render(DefinitionPathEvent? e) =>
                e is not null ?
                  combine(render(e.Previous), e.Render())
                  : "";

            static string combine(string left, string right) => left + right;
        }

        public static DefinitionPath Start() => new(null);

        public DefinitionPath ToChild(string name) => new(new ChildNavEvent(_current, name));

        public DefinitionPath InvokeSchema(ElementSchema schema) => new(new InvokeProfileEvent(_current, schema));

        public DefinitionPath CheckSlice(string sliceName) => new(new CheckSliceEvent(_current, sliceName));


        private abstract class DefinitionPathEvent
        {
            public DefinitionPathEvent(DefinitionPathEvent? previous) => Previous = previous;

            public DefinitionPathEvent? Previous { get; }

            protected internal abstract string Render();

        }

        private class ChildNavEvent : DefinitionPathEvent
        {
            public ChildNavEvent(DefinitionPathEvent? previous, string childName) : base(previous)
            {
                ChildName = childName;
            }

            public string ChildName { get; }

            protected internal override string Render() => $".{ChildName}";
        }

        private class InvokeProfileEvent : DefinitionPathEvent
        {
            public InvokeProfileEvent(DefinitionPathEvent? previous, ElementSchema schema) : base(previous)
            {
                Schema = schema;
            }

            public ElementSchema Schema { get; }

            protected internal override string Render()
            {
                return Schema switch
                {
                    FhirSchema fs =>
                        fs.StructureDefinition.Derivation == StructureDefinitionInformation.TypeDerivationRule.Constraint
                        ? $"->{fs.StructureDefinition.DataType}({fs.Id})"
                        : $"->{fs.StructureDefinition.DataType}",
                    _ => $"->{Schema.Id}"
                };
            }
        }

        private class CheckSliceEvent : DefinitionPathEvent
        {
            public CheckSliceEvent(DefinitionPathEvent? previous, string sliceName) : base(previous)
            {
                SliceName = sliceName;
            }

            public string SliceName { get; }

            protected internal override string Render() => $"[{SliceName}]";
        }

    }
}