/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Utility;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A class representing a reference to a definition of an element. Aspects of the path are the 
    /// full series of profiles invoked, the element names and slices navigated into.
    /// This class is also used to track the location of an instance.
    /// 
    /// An example skeleton path is: "Resource(canonical).childA.childB[sliceB1].childC -> Datatype(canonical).childA"
    /// </summary>
    internal abstract class PathStack
    {
        /// <summary>
        /// Construct a new PathStack and place the current event on top of the stack.
        /// </summary>
        /// <param name="current"></param>
        protected PathStack(PathStackEvent? current)
        {
            Current = current;
        }

        /// <summary>
        /// The current event in the path.
        /// </summary>
        public PathStackEvent? Current { get; }
    }

    /// <summary>
    /// A linked list of events that represent a path.
    /// </summary>
    internal abstract class PathStackEvent
    {
        /// <summary>
        /// Add a new event to the path.
        /// </summary>
        /// <param name="previous"></param>
        public PathStackEvent(PathStackEvent? previous) => Previous = previous;

        /// <summary>
        /// The previous event in the path.
        /// </summary>
        public PathStackEvent? Previous { get; }

        /// <summary>
        /// Render the event as a string.
        /// </summary>
        /// <returns></returns>
        protected internal abstract string Render();
    }


    internal class ChildNavEvent : PathStackEvent
    {
        public ChildNavEvent(PathStackEvent? previous, string childName, string? choiceType) : base(previous)
        {
            ChildName = childName;
            ChoiceType = choiceType;
        }

        public string ChildName { get; }
        public string? ChoiceType { get; }

        protected internal override string Render() =>
            ChoiceType is not null ? $".{ChildName[..^3]}{ChoiceType.Capitalize()}" : $".{ChildName}";

        public override string ToString() => $"ChildNavEvent: ChildName: {ChildName}, ChoiceType: {ChoiceType}";
    }

    internal class IndexNavEvent : PathStackEvent
    {
        public IndexNavEvent(PathStackEvent? previous, int index) : base(previous)
        {
            Index = index;
        }

        public int Index { get; }

        protected internal override string Render() =>
            Previous is OriginalIndicesNavEvent originalIndices
                ? $"[{originalIndices.OriginalIndices[Index]}]"
                : $"[{Index}]";

        public override string ToString() => $"IndexNavEvent: Index: {Index}";
    }

    internal class OriginalIndicesNavEvent : PathStackEvent
    {
        public OriginalIndicesNavEvent(PathStackEvent? previous, IEnumerable<int> originalIndices) : base(previous)
        {
            OriginalIndices = originalIndices.ToArray();
        }

        public int[] OriginalIndices { get; }

        protected internal override string Render() => string.Empty;

        public override string ToString() => $"OriginalIndicesNavEvent: Indices: {string.Join(',', OriginalIndices)}";
    }

    internal class InternalReferenceNavEvent : PathStackEvent
    {
        public InternalReferenceNavEvent(PathStackEvent? previous, string location) : base(previous)
        {
            Location = location.EndsWith(']') ? removeIndexPart(location) : location; // remove last index


            static string removeIndexPart(string location)
            {
                var indexStart = location.LastIndexOf('[');
                return indexStart > 0 ? location[..indexStart] : location;
            }
        }

        public string Location { get; }

        protected internal override string Render() => Location;
    }

    internal class InvokeProfileEvent : PathStackEvent
    {
        public InvokeProfileEvent(PathStackEvent? previous, ElementSchema schema) : base(previous)
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

        public override string ToString() => $"InvokeProfileEvent: {Schema.Id}";
    }

    internal class CheckSliceEvent : PathStackEvent
    {
        public CheckSliceEvent(PathStackEvent? previous, string sliceName) : base(previous)
        {
            SliceName = sliceName;
        }

        public string SliceName { get; }

        protected internal override string Render() => $"[{SliceName}]";
    }

    internal class StartResourceNavEvent : PathStackEvent
    {
        public StartResourceNavEvent(PathStackEvent? previous, string resource) : base(previous)
        {
            Resource = resource;
        }

        public string Resource { get; }

        protected internal override string Render() => $"{Resource}";
    }
}
