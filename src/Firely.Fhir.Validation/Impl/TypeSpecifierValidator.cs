/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.FhirPath;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A single condition/type pair of a <see cref="TypeSpecifierValidator"/> - if <see cref="Condition"/>
    /// evaluates to true (against the containing resource), the element's actual type is <see cref="Type"/>.
    /// </summary>
    public record TypeSpecifierCase(string Condition, Canonical Type);

    /// <summary>
    /// Validates a logical-model element whose actual type is picked by evaluating a FHIRPath condition
    /// against the containing resource, rather than by a fixed <c>type[x]</c>-suffix or type list - e.g.
    /// CDS Hooks' <c>CDSHooksRequest.context</c>, whose type depends on <c>%resource.hook</c>.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public class TypeSpecifierValidator : IValidatable
    {
        /// <summary>
        /// The condition/type cases, evaluated in order - the first matching condition wins.
        /// </summary>
        [DataMember]
        public IReadOnlyList<TypeSpecifierCase> Cases { get; private set; }

        /// <summary>
        /// The type to use when none of the <see cref="Cases"/> match, if any.
        /// </summary>
        [DataMember]
        public Canonical? DefaultType { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="TypeSpecifierValidator"/>.
        /// </summary>
        public TypeSpecifierValidator(IEnumerable<TypeSpecifierCase> cases, Canonical? defaultType = null)
        {
            Cases = cases?.ToList() ?? throw new ArgumentNullException(nameof(cases));
            DefaultType = defaultType;
        }

        /// <inheritdoc />
        public JToken ToJson() =>
            new JProperty("type-specifier", new JObject(
                new JProperty("cases", new JArray(Cases.Select(c => new JObject(
                    new JProperty("condition", c.Condition),
                    new JProperty("type", c.Type.ToString()))))),
                new JProperty("default", DefaultType?.ToString())));

        /// <inheritdoc />
        ResultReport IValidatable.Validate(PocoNode input, ValidationSettings vc, ValidationState state)
        {
            var compiler = vc.FhirPathCompiler ?? FhirPathValidator.DefaultCompiler;

            foreach (var @case in Cases)
            {
                if (evaluate(@case.Condition, input, compiler))
                    return new SchemaReferenceValidator(@case.Type).ValidateOne(input, vc, state);
            }

            if (DefaultType is { } defaultType)
                return new SchemaReferenceValidator(defaultType).ValidateOne(input, vc, state);

            return new IssueAssertion(Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE,
                    "None of the type-specifier conditions matched, and no default type is declared.")
                .AsResult(state, input, nameof(TypeSpecifierValidator), this);
        }

        private static bool evaluate(string condition, PocoNode input, FhirPathCompiler compiler)
        {
            CompiledExpression compiled;
            try
            {
                compiled = compiler.Compile(condition);
            }
            catch (Exception ex)
            {
                throw new IncorrectElementDefinitionException($"Error during compilation of type-specifier condition ({condition})", ex);
            }

            // type-specifier conditions are simple equality checks against the containing resource
            // (e.g. %resource.hook = 'order-sign'), so no terminology service is wired up here.
            var root = input;
            while (root.Parent is { } parent) root = parent;

            var context = new FhirEvaluationContext { Resource = root, RootResource = root };

            return compiled.IsTrue(input, context);
        }
    }
}
