/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// The schema builder for the <see cref="ImpliedStringPrefixValidator"/>.
    /// </summary>
    internal class ImpliedStringPrefixBuilder : ISchemaBuilder
    {
        /// <inheritdoc/>
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            if (conversionMode == ElementConversionMode.ContentReference) yield break;

            var def = nav.Current;

            // Uses the same parsing as ElementDefinition.HasImpliedStringPrefix(), which
            // TypeReferenceBuilder consults to decide whether to suppress the declared type reference -
            // so a marker without a usable prefix never silently removes type validation there.
            var prefix = def.GetImpliedStringPrefix();
            if (prefix is null) yield break;

            // The same core type TypeReferenceBuilder would otherwise have referenced directly - by
            // handing it to ImpliedStringPrefixValidator instead, that type's full schema (format regex
            // included) still gets enforced, just against "prefix + value" rather than the bare wire
            // value.
            var schemaUri = def.Type.SingleOrDefault()?.Code is { } typeCode ? Canonical.ForCoreType(typeCode) : (Canonical?)null;

            yield return new ImpliedStringPrefixValidator(prefix, schemaUri);
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
