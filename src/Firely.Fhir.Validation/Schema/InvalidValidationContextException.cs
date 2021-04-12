/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System;

namespace Firely.Fhir.Validation
{
    public class InvalidValidationContextException : Exception
    {
        public InvalidValidationContextException(string message) : base(message)
        {
        }

        public InvalidValidationContextException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
