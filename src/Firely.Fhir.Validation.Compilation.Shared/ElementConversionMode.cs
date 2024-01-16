/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// Determines which kind of schema we want to generate from the
    /// element.
    /// </summary>
    internal enum ElementConversionMode
    {
        /// <summary>
        /// Generate a schema which includes all constraints represented
        /// by the <see cref="ElementDefinition"/>.
        /// </summary>
        Full,

        /// <summary>
        /// Generate a schema which includes only those constraints that
        /// are part of the type defined inline by the backbone.
        /// </summary>
        /// <remarks>According to constraint eld-5, the ElementDefinition
        /// members type, defaultValue, fixed, pattern, example, minValue, 
        /// maxValue, maxLength, or binding cannot appear in a 
        /// <see cref="ElementDefinition.ContentReference"/>, so these are
        /// generated part of the inline-defined backbone type, not as part
        /// of the element refering to the backbone type.</remarks>
        BackboneType,

        /// <summary>
        /// Generate a schema for an element that uses a backbone type.
        /// </summary>
        /// <remarks>Note: in our schema's there is no difference in treatment
        /// between the element that defines a backbone, and those that refer
        /// to a backbone using <see cref="ElementDefinition.ContentReference"/>.
        /// The type defined inline by the backbone is extracted and both elements
        /// will refer to it, as if both had a content reference."/></remarks>
        ContentReference
    }
}
