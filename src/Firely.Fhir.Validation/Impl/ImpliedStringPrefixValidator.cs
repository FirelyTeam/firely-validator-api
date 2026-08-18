/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Records that the wire value of a string-like element omits a prefix that the underlying
    /// FHIR type would otherwise require, as recorded by the implied-string-prefix extension.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class ImpliedStringPrefixValidator : BasicValidator
    {
        /// <summary>
        /// The prefix that is implied but omitted from the wire value.
        /// </summary>
        [DataMember]
        public string Prefix { get; private set; }

        /// <summary>
        /// The declared type this element would normally be validated against (suppressed for this
        /// element by TypeReferenceBuilder, since the wire value is missing <see cref="Prefix"/>).
        /// </summary>
        [DataMember]
        public Canonical? SchemaUri { get; private set; }

        /// <summary>
        /// Initializes a new ImpliedStringPrefixValidator given a prefix.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="schemaUri">The declared type's schema, to re-run against "prefix + value".</param>
        public ImpliedStringPrefixValidator(string prefix, Canonical? schemaUri = null)
        {
            Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
            SchemaUri = schemaUri;
        }

        /// <inheritdoc />
        protected override string Key => "implied-string-prefix";

        /// <inheritdoc />
        protected override object Value => Prefix;

        /// <inheritdoc />
        internal override ResultReport BasicValidate(PocoNode input, ValidationSettings vc, ValidationState s)
        {
            // No declared type to fall back on (e.g. couldn't be determined at compile time), so
            // there's nothing to re-check beyond the prefix having been recorded.
            if (SchemaUri is not { } schemaUri) return ResultReport.SUCCESS;

            // Re-run the declared type's own schema (e.g. FHIR's uuid regex) against a value with the
            // implied prefix restored, instead of against the wire value directly - reusing the exact
            // same type schema TypeReferenceBuilder would otherwise have referenced.
            if (input.Poco is not PrimitiveType primitive || primitive.JsonValue is null) return ResultReport.SUCCESS;

            var prefixedValue = Prefix + PrimitiveTypeConverter.ConvertTo<string>(primitive.JsonValue);
            var clone = (PrimitiveType)primitive.DeepCopy();
            clone.JsonValue = prefixedValue;

            // Keep the original node's parent/name/index (for correct issue locations) and only swap
            // in the prefixed clone as its Poco.
            var syntheticNode = input with { Poco = clone };
            return new SchemaReferenceValidator(schemaUri).ValidateOne(syntheticNode, vc, s);
        }
    }
}
