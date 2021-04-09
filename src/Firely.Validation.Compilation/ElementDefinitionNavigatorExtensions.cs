/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Linq;

namespace Firely.Validation.Compilation
{
    //TODO: After the April 2021 version of the SDK is published, this code can be removed and replaced
    //by the equivalent functions in the SDK.
    internal static class ElementDefinitionNavigatorExtensions
    {
        internal static bool IsResourcePlaceholder(this ElementDefinition ed)
            => ed.Type is not null && ed.Type.Any(t => t.Code == "Resource" || t.Code == "DomainResource");

        public static bool IsSlicing(this ElementDefinitionNavigator nav) => nav.Current.Slicing != null;
    }
}
