/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using System;

namespace Hl7.Fhir.Validation
{
    /// <summary>
    /// Exception about an error encountered in an ElementDefinitions.
    /// </summary>
    internal class IncorrectElementDefinitionException : Exception
    {
        /// <summary>
        /// Exception about an error encountered in an ElementDefinitions.
        /// </summary>
        /// <param name="message">Error message</param>
        public IncorrectElementDefinitionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Exception about an error encountered in an ElementDefinitions.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">inner exception</param>
        public IncorrectElementDefinitionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
