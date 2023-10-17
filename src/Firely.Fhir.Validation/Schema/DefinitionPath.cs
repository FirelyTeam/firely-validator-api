﻿/* 
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

        public string RenderInstanceLocation()
        {
            return _current is not null ? render(_current) : string.Empty;

            static string render(DefinitionPathEvent e) =>
                 e.Previous switch
                 {
                     InvokeProfileEvent ie when ie.Previous is null => $"{ie.Render()}{e.RenderForInstance()}",
                     not null => $"{render(e.Previous)}{e.RenderForInstance()}",
                     _ => e.RenderForInstance()
                 };
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
        /// Start a new DefinitionPath.
        /// </summary>
        public static DefinitionPath Start() => new(null);

        /// <summary>
        /// Update the path to include a move to a child element.
        /// </summary>
        public DefinitionPath ToChild(string name) => new(new ChildNavEvent(_current, name));

        /// <summary>
        /// Update the path to include an index of a child element.
        /// </summary>
        public DefinitionPath ToIndex(int index) => new(new IndexNavEvent(_current, index));

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

            protected internal abstract string RenderForInstance();

        }

        private class ChildNavEvent : DefinitionPathEvent
        {
            public ChildNavEvent(DefinitionPathEvent? previous, string childName) : base(previous)
            {
                ChildName = childName;
            }

            public string ChildName { get; }

            protected internal override string Render() => $".{ChildName}";
            protected internal override string RenderForInstance() => $".{ChildName}";

            public override string ToString() => $"ChildNavEvent: {ChildName}";
        }

        private class IndexNavEvent : DefinitionPathEvent
        {
            public IndexNavEvent(DefinitionPathEvent? previous, int index) : base(previous)
            {
                Index = index;
            }

            public int Index { get; }

            protected internal override string Render() => string.Empty;
            protected internal override string RenderForInstance() => $"[{Index}]";

            public override string ToString() => $"IndexNavEvent: {Index}";
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

            protected internal override string RenderForInstance() => "";//$"{(Schema as FhirSchema)?.StructureDefinition.DataType}";

            public bool IsProfiledFhirType => Schema is FhirSchema fs && fs.StructureDefinition.Derivation == StructureDefinitionInformation.TypeDerivationRule.Constraint;

            public override string ToString() => $"InvokeProfileEvent: {Schema.Id}";
        }

        private class CheckSliceEvent : DefinitionPathEvent
        {
            public CheckSliceEvent(DefinitionPathEvent? previous, string sliceName) : base(previous)
            {
                SliceName = sliceName;
            }

            public string SliceName { get; }

            protected internal override string Render() => $"[{SliceName}]";
            protected internal override string RenderForInstance() => string.Empty;
        }
    }
}