/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.Model;
using System.Linq;

namespace Firely.Validation.Compilation
{
    internal static class TypeRefExtensions
    {
        public static string[] GetDeclaredProfiles(this ElementDefinition.TypeRefComponent typeRef)
        {
            // back to what DSTU2 had ;)
            if (typeRef.ProfileElement.Any())
                return typeRef.Profile.ToArray();

            // logical model urls in Code will be used verbatim
            return typeRef.Code.StartsWith("http://") ?
                (new[] { typeRef.Code })
                : (new[] { canonicalUriForFhirCoreType(typeRef.Code).Value });

            static Canonical canonicalUriForFhirCoreType(string typename) => new Canonical("http://hl7.org/fhir/StructureDefinition/" + typename);
        }
    }
}