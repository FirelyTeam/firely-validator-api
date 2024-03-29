﻿/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

#nullable enable

using Hl7.Fhir.ElementModel;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An element within a tree of typed FHIR data with also a parent element.
    /// </summary>
    /// <remarks>
    /// This interface represents FHIR data as a tree of elements, including type information either present in 
    /// the instance or derived from fully aware of the FHIR definitions and types
    /// </remarks>
#pragma warning disable CS0618 // Type or member is obsolete
    public interface IScopedNode : IBaseElementNavigator<IScopedNode>
#pragma warning restore CS0618 // Type or member is obsolete
    {
        /* 
         * 
        /// <summary>
        /// The parent node of this node, or null if this is the root node.
        /// </summary>
        IScopedNode? Parent { get; }   // We don't need this probably.
        */
    }
}

#nullable restore