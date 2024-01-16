/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// This specialized resolver contains corrections for the R3/R4 FHIR specification and
    /// applies them to the resolved StructureDefinitions. It should be used as a wrapper around resolvers for the
    /// core specification, and serve as input for a <see cref="SnapshotSource" />, before being cached.
    /// </summary>
    /// <remarks>This class is marked public since it is useful across the Firely products and 
    /// we recommend only using it if you are aware of the kind of corrections done by this resolver.</remarks>
    public class StructureDefinitionCorrectionsResolver : IAsyncResourceResolver, IResourceResolver
    {
#pragma warning disable CS0618 // Type or member is obsolete
        /// <summary>
        /// Constructs a new correcting resolver.
        /// </summary>
        /// <param name="nested"></param>
        public StructureDefinitionCorrectionsResolver(ISyncOrAsyncResourceResolver nested)
        {
            Nested = nested.AsAsync();
        }
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// The resolver for which the StructureDefinitions will be corrected.
        /// </summary>
        public IAsyncResourceResolver Nested { get; }

        /// <inheritdoc />
        public Resource? ResolveByCanonicalUri(string uri) => TaskHelper.Await(() => ResolveByCanonicalUriAsync(uri));

        /// <inheritdoc />
        public async Task<Resource?> ResolveByCanonicalUriAsync(string uri)
        {
            var result = await Nested.ResolveByCanonicalUriAsync(uri).ConfigureAwait(false);
            return correctStructureDefinition(result);
        }

        private static Resource? correctStructureDefinition(Resource? result)
        {
            // If this is not a StructureDefinition, just pass it on without doing anything to it.
            if (result is not StructureDefinition sd) return result;

            if (sd.Kind == StructureDefinition.StructureDefinitionKind.Resource)
            {
                correctIdElement(sd.Differential); correctIdElement(sd.Snapshot);
            }


            if (sd.Type == "string")
            {
                correctStringTextRegex("string", sd.Differential); correctStringTextRegex("string", sd.Snapshot);
            }

            if (sd.Type == "markdown")
            {
                correctStringTextRegex("markdown", sd.Differential); correctStringTextRegex("markdown", sd.Snapshot);
            }

            if (new[] { "StructureDefinition", "ElementDefinition", "Reference", "Questionnaire" }.Contains(sd.Type))
            {
                correctConstraints(sd.Differential); correctConstraints(sd.Snapshot);
            }

            return sd;

            static void correctIdElement(IElementList elements)
            {
                if (elements is null) return;

                var idElements = elements.Element.Where(e => Regex.IsMatch(e.Path, @"^[a-zA-Z]+\.id$"));
                if (idElements.Count() == 1 && idElements.Single().Type.Count == 1)
                {
                    idElements.Single().Type = new() { new ElementDefinition.TypeRefComponent { Code = "id" } };
                }
            }

            static void correctStringTextRegex(string datatype, IElementList elements)
            {
                if (elements is null) return;

                var valueElement = elements.Element.Where(e => e.Path == $"{datatype}.value");
                if (valueElement.Count() == 1 && valueElement.Single().Type.Count == 1)
                {
                    valueElement.Single().Type.Single().
                        SetStringExtension("http://hl7.org/fhir/StructureDefinition/regex", @"[\r\n\t\u0020-\uFFFF]*");
                }
            }

            static void correctConstraints(IElementList elements)
            {
                if (elements is null) return;

                foreach (var constraintElement in elements.Element.SelectMany(e => e.Constraint))
                {
                    constraintElement.Expression = constraintElement switch
                    {
                        {
                            Key: "ref-1", Expression: @"reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids'))" or
                                                      @"reference.startsWith('#').not() or (reference.substring(1).trace('url') in %resource.contained.id.trace('ids'))" or
                                                      @"reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids')) or (reference='#' and %rootResource!=%resource)"
                        }
                                                   => @"reference.exists() implies (reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids')) or (reference='#' and %rootResource!=%resource))",

                        //Double quotes should be single quotes
                        {
                            Key: "eld-11", Expression: @"binding.empty() or type.code.empty() or type.code.contains(\"":\"") or type.select((code = 'code') or (code = 'Coding') or (code='CodeableConcept') or (code = 'Quantity') or (code = 'string') or (code = 'uri') or (code = 'Duration')).exists()"
                        }
                            => @"binding.empty() or type.code.empty() or type.code.contains(':') or type.select((code = 'code') or (code = 'Coding') or (code='CodeableConcept') or (code = 'Quantity') or (code = 'string') or (code = 'uri') or (code = 'Duration')).exists()",


                        // matches should be applied on the whole string:
                        { Key: "eld-19", Expression: @"path.matches('[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\.[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\[x\\])?(\\:[^\\s\\.]+)?)*')" }
                                                  => @"path.matches('^[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\.[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\[x\\])?(\\:[^\\s\\.]+)?)*$')",
                        { Key: "eld-20", Expression: @"path.matches('[A-Za-z][A-Za-z0-9]*(\\.[a-z][A-Za-z0-9]*(\\[x])?)*')" }
                                                  => @"path.matches('^[A-Za-z][A-Za-z0-9]*(\\.[a-z][A-Za-z0-9]*(\\[x])?)*$')",
                        {
                            Key: "sdf-0", Expression: @"name.matches('[A-Z]([A-Za-z0-9_]){0,254}')" or
                                                      @"name.exists() implies name.matches('[A-Z]([A-Za-z0-9_]){0,254}')"
                        }
                                                   => @"name.exists() implies name.matches('^[A-Z]([A-Za-z0-9_]){0,254}$')",

                        // do not use $this (see https://jira.hl7.org/browse/FHIR-37761)
                        { Key: "sdf-24", Expression: @"element.where(type.code='Reference' and id.endsWith('.reference') and type.targetProfile.exists() and id.substring(0,$this.length()-10) in %context.element.where(type.code='CodeableReference').id).exists().not()" }
                                                  => @"element.where(type.code='Reference' and id.endsWith('.reference') and type.targetProfile.exists() and id.substring(0,$this.id.length()-10) in %context.element.where(type.code='CodeableReference').id).exists().not()",
                        { Key: "sdf-25", Expression: @"element.where(type.code='CodeableConcept' and id.endsWith('.concept') and binding.exists() and id.substring(0,$this.length()-8) in %context.element.where(type.code='CodeableReference').id).exists().not()" }
                                                   => @"element.where(type.code='CodeableConcept' and id.endsWith('.concept') and binding.exists() and id.substring(0,$this.id.length()-8) in %context.element.where(type.code='CodeableReference').id).exists().not()",

                        //sdf-29, syntax error, 'specialization' and 'derivation' are reversed
                        { Key: "sdf-29", Expression: @"((kind in 'resource' | 'complex-type') and (specialization = 'derivation')) implies differential.element.where((min != 0 and min != 1) or (max != '1' and max != '*')).empty()" }
                                                   => @"((kind in 'resource' | 'complex-type') and (derivation= 'specialization')) implies differential.element.where((min != 0 and min != 1) or (max != '1' and max != '*')).empty()",

                        // correct datatype in expression:
                        { Key: "que-0", Expression: @"name.matches('[A-Z]([A-Za-z0-9_]){0,254}')" }
                                                 => @"name.exists() implies name.matches('[A-Z]([A-Za-z0-9_]){0,254}')",
                        { Key: "que-7", Expression: @"operator = 'exists' implies (answer is Boolean)" }
                                                 => @"operator = 'exists' implies (answer is boolean)",
                        var ce => ce.Expression
                    };
                }

            }
        }

        /// <inheritdoc />
        public Resource? ResolveByUri(string uri) => TaskHelper.Await(() => ResolveByUriAsync(uri));

        /// <inheritdoc />
        public async Task<Resource?> ResolveByUriAsync(string uri)
        {
            var result = await Nested.ResolveByUriAsync(uri).ConfigureAwait(false);
            return correctStructureDefinition(result);
        }
    }
}