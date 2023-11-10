/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A class representing a reference to a definition of an element. Aspects of the path are the 
    /// full series of profiles invoked, the element names and slices navigated into.
    /// 
    /// An example skeleton path is: "Resource(canonical).childA.childB[sliceB1].childC -> Datatype(canonical).childA"
    /// </summary>
    public class DefinitionPath
    {
        private DefinitionPath(DefinitionPathEvent? current) => _current = current;

        private readonly DefinitionPathEvent? _current;

        /// <inheritdoc/>
        public override string ToString()
        {
            return _current is not null ? render(_current) : string.Empty;

            static string render(DefinitionPathEvent e) =>
                e.Previous is not null ?
                  combine(render(e.Previous), e)
                  : e.Render();

            static string combine(string left, DefinitionPathEvent right) =>
                right is InvokeProfileEvent ipe ? left + "->" + ipe.Render() : left + right.Render();
        }

        /// <summary>
        /// Whether the path contains information that cannot be derived from the instance path.
        /// </summary>
        public bool HasDefinitionChoiceInformation
        {
            get
            {
                var scan = _current;

                while (scan is not null)
                {
                    if (scan is InvokeProfileEvent s && s.IsProfiledFhirType) return true;
                    if (scan is CheckSliceEvent) return true;
                    scan = scan.Previous;
                }

                return false;
            }
        }

        /// <summary>
        /// Whether the path contains slice information that cannot be derived from the instance path.
        /// </summary>
        public bool HasSliceInformation
        {
            get
            {
                var scan = _current;

                while (scan is not null)
                {
                    if (scan is CheckSliceEvent) return true;
                    scan = scan.Previous;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the slice name of the last slice in the path, or an empty string if there is no slice information.
        /// </summary>
        /// <returns>The slice name of the last slice in the path, or an empty string if there is no slice information.</returns>
        public string GetSliceInfo()
        {
            var scan = _current;
            var sliceName = string.Empty;

            while (scan is not null)
            {
                if (scan is CheckSliceEvent cse)
                {
                    sliceName = sliceName == string.Empty ? cse.SliceName : $"{cse.SliceName}, subslice {sliceName}";
                }
                else if (sliceName != string.Empty)
                {
                    return sliceName;
                }

                scan = scan.Previous;
            }

            return string.Empty;
        }

        /// <summary>
        /// Start a new DefinitionPath.
        /// </summary>
        public static DefinitionPath Start() => new(null);

        /// <summary>
        /// Update the path to include a move to a child element.
        /// </summary>
        public DefinitionPath ToChild(string name) => new(new ChildNavEvent(_current, name));

        /// <summary>
        /// Update the path to include an invocation of a (nested) profile.
        /// </summary>
        public DefinitionPath InvokeSchema(ElementSchema schema) => new(new InvokeProfileEvent(_current, schema));

        /// <summary>
        /// Update the path to include a move into a specific slice.
        /// </summary>
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
                        ? $"{fs.StructureDefinition.DataType}({fs.Id})"
                        : $"{fs.StructureDefinition.DataType}",
                    _ => $"{Schema.Id}"
                };
            }

            public bool IsProfiledFhirType => Schema is FhirSchema fs && fs.StructureDefinition.Derivation == StructureDefinitionInformation.TypeDerivationRule.Constraint;
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