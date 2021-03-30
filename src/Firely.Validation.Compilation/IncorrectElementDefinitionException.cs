﻿/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */

using System;

namespace Firely.Validation.Compilation
{
    public class IncorrectElementDefinitionException : Exception
    {
        public IncorrectElementDefinitionException(string message) : base(message)
        {
        }

        public IncorrectElementDefinitionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
