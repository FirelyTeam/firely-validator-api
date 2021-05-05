/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
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
