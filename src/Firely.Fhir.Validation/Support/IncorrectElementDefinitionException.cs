/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;

namespace Firely.Fhir.Validation
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
