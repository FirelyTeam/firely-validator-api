/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

namespace Firely.Reflection.Emit
{
    /// <summary>
    /// Represents the code for StructureDefinition.Kind
    /// </summary>
    internal enum StructureDefinitionKind
    {
        /// <summary>
        /// A primitive type that has a value and an extension. 
        /// These can be used throughout complex datatype, Resource and extension definitions. 
        /// Only the base specification can define primitive types.
        /// </summary>
        Primitive,

        /// <summary>
        /// A type that is defined by a backbone element. Cannot be reused, except by using
        /// ElementDefinition.contentRef.
        /// </summary>
        Backbone,

        /// <summary>
        /// 
        /// </summary>
        Complex,

        /// <summary>
        /// 
        /// </summary>
        Resource,

        /// <summary>
        /// 
        /// </summary>
        Logical
    };

}
