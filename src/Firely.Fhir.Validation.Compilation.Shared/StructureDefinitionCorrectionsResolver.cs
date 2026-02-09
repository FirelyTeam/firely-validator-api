/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#if STU3
using ConstraintSeverity = Hl7.Fhir.Model.ElementDefinition.ConstraintSeverity;
#endif

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
        private const string FHIR_TYPE_EXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-fhir-type";
        private static readonly HashSet<string> TYPES_TO_CORRECT_CONSTRAINTS = ["StructureDefinition", "ElementDefinition", "Reference", "Questionnaire", "Bundle", "CareTeam", "OperationDefinition", "Observation", "Coverage"];
        private readonly ModelInspector _inspector;

#pragma warning disable CS0618 // Type or member is obsolete
        /// <summary>
        /// Constructs a new correcting resolver.
        /// </summary>
        /// <param name="inspector"></param>
        /// <param name="nested"></param>
        public StructureDefinitionCorrectionsResolver(ModelInspector inspector, ISyncOrAsyncResourceResolver nested)
        {
            _inspector = inspector;
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

        private Resource? correctStructureDefinition(Resource? result)
        {
            // If this is not a StructureDefinition, just pass it on without doing anything to it.
            if (result is not StructureDefinition sd) return result;

            if (sd.Kind == StructureDefinition.StructureDefinitionKind.Resource)
            {
                correctIdElement(sd.Differential); correctIdElement(sd.Snapshot);

                if (_inspector.FhirRelease == FhirRelease.R5 && sd.Type == "ImagingSelection")
                {
                    correctImagingSelection(sd.Differential); correctImagingSelection(sd.Snapshot);
                }
            }

            if (sd.Type == "string")
            {
                correctStringTextRegex("string", sd.Differential); correctStringTextRegex("string", sd.Snapshot);
            }

            if (sd.Type == "markdown")
            {
                correctStringTextRegex("markdown", sd.Differential); correctStringTextRegex("markdown", sd.Snapshot);
            }

            if (TYPES_TO_CORRECT_CONSTRAINTS.Contains(sd.Type))
            {
                correctConstraints(sd.Differential); correctConstraints(sd.Snapshot);
            }

            if (sd.Type == "Bundle")
            {
                addBundleConstraints(sd.Snapshot);
            }

            return sd;

            static void correctIdElement(IElementList elements)
            {
                if (elements is null) return;

                var idElements = elements.Element.Where(e => Regex.IsMatch(e.Path!, @"^[a-zA-Z]+\.id$"));
                if (idElements.Count() == 1 && idElements.First().Type is { Count: 1 } singleTypeRef && singleTypeRef[0].Code != "id")
                {
                    singleTypeRef[0].Code = "id";

                    // Update fhir type extension if it is present
                    var fhirTypeExtensions = singleTypeRef[0].Extension.Where(e => e.Url == FHIR_TYPE_EXTENSION);

                    foreach (var extension in fhirTypeExtensions)
                    {
                        if (extension.Value is IValue<string> { Value: "string" } stringValue)
                            stringValue.Value = "id";
                    }
                }
            }

            static void correctImagingSelection(IElementList elements)
            {
                if (elements is null) return;

                var sopClassElements = elements.Element.Where(e => e.Path == "ImagingSelection.instance.sopClass");

                foreach (var sopClassElement in sopClassElements.Where(sce => sce.Type.Count == 1 && sce.Type[0].Code != "id"))
                    sopClassElement.Type[0].Code = "id";
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

            void correctConstraints(IElementList elements)
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

#if R4_AND_LATER        // correct opd-3:
                        { Key: "opd-3", Expression: @"targetProfile.exists() implies (type = 'Reference' or type = 'canonical')", }
                                                 => _inspector.FhirRelease switch
                                                 {
                                                     FhirRelease.R4 or FhirRelease.R4B => @"targetProfile.exists() implies (type = 'Reference' or type = 'canonical' or type.memberOf('http://hl7.org/fhir/ValueSet/resource-types'))",
                                                     _ => constraintElement.Expression
                                                 },

                        { Key: "opd-3", Expression: @"targetProfile.exists() implies (type = 'Reference' or type = 'canonical' or type.memberOf('http://hl7.org/fhir/ValueSet/resource-types'))" }
                                                 => _inspector.FhirRelease switch
                                                 {
                                                     FhirRelease.R5 => @"targetProfile.exists() implies (type = 'Reference' or type = 'canonical' or type.memberOf('http://hl7.org/fhir/ValueSet/all-resource-types'))",
                                                     _ => constraintElement.Expression
                                                 },
#endif
                        // correct vital-signs-vs1:
                        { Key: "vs-1", Expression: @"($this as dateTime).toString().length() >= 8" }
                                                => @"$this is dateTime implies $this.toString().length() >= 10",

                        { Key: "bdl-8", Expression: "fullUrl.contains('/_history/').not()" } 
                                                 => "fullUrl.exists() implies fullUrl.contains('/_history/').not()",

                        { Key: "ctm-1", Expression: "onBehalfOf.exists() implies (member.resolve().iif(empty(), true, ofType(Practitioner).exists()))" } 
                                                 => "onBehalfOf.exists() implies (member.resolve() is Practitioner)",

                        _ => constraintElement.Expression
                    };
                }
            }

            // See https://github.com/FirelyTeam/firely-validator-api/issues/152
            static void addBundleConstraints(IElementList elements)
            {
                if (elements is null) return;

#if R4_AND_LATER
                string[] toBeAdded = ["bdl-3a", "bdl-3b", "bdl-3c", "bdl-3d", "bdl-15"];
#else
                string[] toBeAdded = ["bdl-3a", "bdl-3b", "bdl-3c", "bdl-3d", "bdl-15", "bdl-10", "bdl-11", "bdl-12"];
#endif

                var bundleConstraintList = elements.Element.Where(ed => ed.Path == "Bundle").Select(c => c.Constraint).Single();

                bundleConstraintList.AddRange(toBeAdded.Select(getBundleConstraintByKey));
            }

            static ElementDefinition.ConstraintComponent getBundleConstraintByKey(string key)
            {
                return key switch
                {
                    "bdl-3a" => new ElementDefinition.ConstraintComponent
                    {
                        Severity = ConstraintSeverity.Error,
                        Key = key,
                        Human = "For collections of type document, message, searchset or collection, all entries must contain resources, and not have request or response element",
                        Expression = "type in ('document' | 'message' | 'searchset' | 'collection') implies entry.all(resource.exists() and request.empty() and response.empty())"
                    },
                    "bdl-3b" => new ElementDefinition.ConstraintComponent
                    {
                        Severity = ConstraintSeverity.Error,
                        Key = key,
                        Human = "For collections of type history, all entries must contain request or response elements, and resources if the method is POST, PUT or PATCH",
                        Expression = "type = 'history' implies entry.all(request.exists() and response.exists() and ((request.method in ('POST' | 'PATCH' | 'PUT')) = resource.exists()))"
                    },
                    "bdl-3c" => new ElementDefinition.ConstraintComponent
                    {
                        Severity = ConstraintSeverity.Error,
                        Key = key,
                        Human = "For collections of type transaction or batch, all entries must contain request elements, and resources if the method is POST, PUT or PATCH",
                        Expression = "type in ('transaction' | 'batch') implies entry.all(request.method.exists() and ((request.method in ('POST' | 'PATCH' | 'PUT')) = resource.exists()))"
                    },
                    "bdl-3d" => new ElementDefinition.ConstraintComponent
                    {
                        Severity = ConstraintSeverity.Error,
                        Key = key,
                        Human = "For collections of type transaction-response or batch-response, all entries must contain response elements",
                        Expression = "type in ('transaction-response' | 'batch-response') implies entry.all(response.exists())"
                    },
                    "bdl-10" => new ElementDefinition.ConstraintComponent
                    {
                        Severity = ConstraintSeverity.Error,
                        Key = key,
                        Human = "A document must have a date",
                        Expression = "type = 'document' implies (timestamp.hasValue())"
                    },
                    "bdl-11" => new ElementDefinition.ConstraintComponent
                    {
                        Severity = ConstraintSeverity.Error,
                        Key = key,
                        Human = "A document must have a Composition as the first resource",
                        Expression = "type = 'document' implies entry.first().resource.is(Composition)"
                    },
                    "bdl-12" => new ElementDefinition.ConstraintComponent
                    {
                        Severity = ConstraintSeverity.Error,
                        Key = key,
                        Human = "A message must have a MessageHeader as the first resource",
                        Expression = "type = 'message' implies entry.first().resource.is(MessageHeader)"
                    },
                    "bdl-15" => new ElementDefinition.ConstraintComponent
                    {
                        Severity = ConstraintSeverity.Error,
                        Key = key,
                        Human = "Bundle resources where type is not transaction, transaction-response, batch, or batch-response or when the request is a POST SHALL have Bundle.entry.fullUrl populated",
                        Expression = "type='transaction' or type='transaction-response' or type='batch' or type='batch-response' or entry.all(fullUrl.exists() or request.method='POST')"
                    },
                    _ => throw new InvalidOperationException("unknown key")
                };
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