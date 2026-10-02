/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The schema builder for the <see cref="UniqueContainedIdValidator"/>, which checks that the ids of contained
    /// resources are unique. Can be removed when the FHIR specification gets an invariant for this
    /// (see https://github.com/FirelyTeam/firely-validator-api/issues/675).
    /// </summary>
    internal class UniqueContainedIdBuilder : ISchemaBuilder
    {
        private const string DOMAINRESOURCE_CONTAINED = "DomainResource.contained";

        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (conversionMode is ElementConversionMode.BackboneType or ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            // A slice of 'contained' only sees part of the contained resources, so ids cannot be compared there.
            if (!string.IsNullOrEmpty(def.SliceName)) yield break;

            if (def.Path == DOMAINRESOURCE_CONTAINED || def.Base?.Path == DOMAINRESOURCE_CONTAINED)
                yield return new UniqueContainedIdValidator();
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
