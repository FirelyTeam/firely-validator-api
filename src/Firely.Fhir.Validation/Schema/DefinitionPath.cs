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
    /// This class is also used to track the location of an instance.
    /// </summary>
    /// <example>An example skeleton path is: "Resource(canonical).childA.childB[sliceB1].childC -> Datatype(canonical).childA"</example>
    public class DefinitionPath : PathStack
    {
        private DefinitionPath(PathStackEvent? current) : base(current)
        {
        }

        /// <summary>
        /// Whether the path contains information that cannot be derived from the instance path.
        /// </summary>
        public bool HasDefinitionChoiceInformation
        {
            get
            {
                var scan = Current;

                while (scan is not null)
                {
                    if (scan is InvokeProfileEvent s && s.IsProfiledFhirType) return true;
                    if (scan is CheckSliceEvent) return true;
                    scan = scan.Previous;
                }

                return false;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Current is not null ? render(Current) : string.Empty;

            static string render(PathStackEvent e) =>
                e.Previous is not null ?
                  combine(render(e.Previous), e)
                  : e.Render();

            static string combine(string left, PathStackEvent right) =>
                right is InvokeProfileEvent ipe ? left + "->" + ipe.Render() : left + right.Render();
        }

        /// <summary>
        /// Whether the path contains slice information that cannot be derived from the instance path.
        /// </summary>
        public bool HasSliceInformation
        {
            get
            {
                var scan = Current;

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
            var scan = Current;
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
        public DefinitionPath ToChild(string name) => new(new ChildNavEvent(Current, name, null));

        /// <summary>
        /// Update the path to include an invocation of a (nested) profile.
        /// </summary>
        public DefinitionPath InvokeSchema(ElementSchema schema) => new(new InvokeProfileEvent(Current, schema));

        /// <summary>
        /// Update the path to include a move into a specific slice.
        /// </summary>
        public DefinitionPath CheckSlice(string sliceName) => new(new CheckSliceEvent(Current, sliceName));
    }
}
