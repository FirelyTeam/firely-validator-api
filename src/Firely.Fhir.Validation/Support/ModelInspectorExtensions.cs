/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The validator-wide strategy for obtaining a <see cref="ModelInspector"/> during validation.
    /// </summary>
    internal static class ModelInspectorExtensions
    {
        /// <summary>
        /// Returns the <see cref="ModelInspector"/> to use for inspecting FHIR model metadata:
        /// the one configured on the settings when present, otherwise one derived from the type
        /// of the given context node's POCO.
        /// </summary>
        /// <remarks>We introduced <see cref="ValidationSettings.ModelInspector"/> to be able to
        /// express the model to validate against properly, but for legacy call sites we fall back
        /// to the obsoleted <c>ModelInspector.ForType()</c> to retrieve the inspector for the POCO
        /// itself. That might end up being <c>ModelInspector.Base</c> for custom types, which can
        /// misjudge e.g. choice-type membership - see the remarks (and the unit test referenced
        /// there) in <c>ExtensionContextValidator.validateElementContext</c>.</remarks>
        public static ModelInspector GetModelInspector(this ValidationSettings vc, PocoNode context)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return vc.ModelInspector ?? ModelInspector.ForType(context.Poco.GetType());
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
