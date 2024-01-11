/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal static class PatternValidationHelpers
    {
        public static bool Matches(this ITypedElement value, ITypedElement pattern)
        {
            if (value == null && pattern == null) return true;
            if (value == null || pattern == null) return false;

            if (!ValueEquality(value.Value, pattern.Value)) return false;

            // Compare the children.
            var valueChildren = value.Children();
            var patternChildren = pattern.Children();

            return patternChildren.All(patternChild => valueChildren.Any(valueChild =>
                  patternChild.Name == valueChild.Name && valueChild.Matches(patternChild)));

        }

        public static bool ValueEquality<T1, T2>(T1 val1, T2 val2)
        {
            // Compare the value
            if (val1 == null && val2 == null) return true;
            if (val1 == null || val2 == null) return false;

            try
            {
                // convert val2 to type of val1.
                T1 boxed2 = (T1)Convert.ChangeType(val2, typeof(T1));

                // compare now that same type.
                return val1.Equals(boxed2);
            }
            catch
            {
                return false;
            }
        }
    }
}
