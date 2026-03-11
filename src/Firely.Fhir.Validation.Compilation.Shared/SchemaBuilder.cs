/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

/// <summary>
/// Converts the constraints in a <see cref="Hl7.Fhir.Model.StructureDefinition"/> to an
/// <see cref="ElementSchema"/>, which can then be used for validation.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
[System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
public class SchemaBuilder : ISchemaBuilder
{
    /// <summary>
    /// The resolver to use when the <see cref="Hl7.Fhir.Model.StructureDefinition"/> under conversion
    /// refers to other StructureDefinitions.
    /// </summary>
    public readonly IAsyncResourceResolver Source;
    private readonly IEnumerable<ISchemaBuilder> _schemaBuilders;

    /// <summary>
    /// Initializes a new SchemaBuilder with a given <see cref="IAsyncResourceResolver"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="schemaBuilders"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SchemaBuilder(IAsyncResourceResolver source, IEnumerable<ISchemaBuilder>? schemaBuilders = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _schemaBuilders = schemaBuilders?.ToList() ?? new();
    }

    /// <inheritdoc/>
    public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
    {
        try
        {
            if (!nav.MoveToFirstChild())
            {
                // verify that current snapshot is intended to be empty, or did we fail to generate snapshot due to missing base profiles
                // if any of base profiles is missing an InvalidOperationException will be thrown and caught below
                _ = getBaseProfiles(nav.StructureDefinition);
                return new[] { new ElementSchema(nav.StructureDefinition.Url!) };
            }

            var subschemaCollector = new SubschemaCollector(nav);
                
            var converted = ConvertElement(nav, subschemaCollector);

            if (subschemaCollector.FoundSubschemas)
                converted.Add(subschemaCollector.BuildDefinitionAssertion());

            // Generate the right subclass of ElementSchema for the kind of SD
            return new[] { generateFhirSchema(nav.StructureDefinition, converted) };
        }
        catch (Exception e) when (e is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to convert ElementDefinition at " +
                                                $"{nav.Current.ElementId ?? nav.Current.Path} in profile {nav.StructureDefinition.Url}: {e.Message}", e);
        }
    }

    private FhirSchema generateFhirSchema(StructureDefinition sd, List<IAssertion> members)
    {
        var bases = getBaseProfiles(sd);
        var imposeProfiles = getImposeProfiles(sd);
        var sdi = new StructureDefinitionInformation(
            sd.Url ?? throw new ArgumentException(nameof(sd.Url)),
            bases.ToArray(),
            sd.Type ?? throw new ArgumentException(nameof(sd.Type)),
            (StructureDefinitionInformation.TypeDerivationRule?)sd.Derivation,
            sd.Abstract ?? throw new ArgumentException(nameof(sd.Abstract)),
            imposeProfiles.Any() ? imposeProfiles.ToArray() : null);

        // Add "fhir type label"
        if (sd.Abstract == false)
            members.Insert(0, new FhirTypeLabelValidator(sd.Type));

        return sd.Kind switch
        {
            StructureDefinition.StructureDefinitionKind.Resource => new ResourceSchema(sdi, members),
            StructureDefinition.StructureDefinitionKind.ComplexType when sd.Type == "Extension" => new ExtensionSchema(sdi, members),
            StructureDefinition.StructureDefinitionKind.PrimitiveType or
                StructureDefinition.StructureDefinitionKind.ComplexType => new DatatypeSchema(sdi, members),
            _ => throw new NotSupportedException($"Compiler cannot handle SD {sd.Url}, which is of kind {sd.Kind}.")
        };
    }

    private List<Canonical> getBaseProfiles(StructureDefinition sd)
    {
        return getBaseProfiles(new(), sd, Source);

        static List<Canonical> getBaseProfiles(List<Canonical> result, StructureDefinition sd, IAsyncResourceResolver resolver)
        {
            var myBase = sd.BaseDefinition;
            if (myBase is null) return result;

            result.Add(myBase);

            var baseSd = TaskHelper.Await(() => resolver.FindStructureDefinitionAsync(myBase));

            return baseSd is not null
                ? getBaseProfiles(result, baseSd, resolver)
                : throw new InvalidOperationException($"StructureDefinition '{sd.Url}' mentions profile '{myBase}' as its base, but it cannot be resolved and is thus not available to the compiler.");
        }
    }

    private List<Canonical> getImposeProfiles(StructureDefinition sd)
    {
        const string imposeProfileUrl = "http://hl7.org/fhir/StructureDefinition/structuredefinition-imposeProfile";
        var result = new List<Canonical>();
        
        if (sd.Extension is null) return result;

        foreach (var extension in sd.Extension.Where(e => e.Url == imposeProfileUrl))
        {
            // The extension must always contain a valueCanonical as per the extension definition
            // This is represented as FhirUri in the FHIR model
            if (extension.Value is FhirUri uri && !string.IsNullOrEmpty(uri.Value))
            {
                result.Add(new Canonical(uri.Value));
            }
        }

        return result;
    }

    /// <summary>
    /// Converts the current <see cref="ElementDefinition"/> inside an <see cref="ElementDefinitionNavigator"/>
    /// to an ElementSchema.
    /// </summary>
    /// <remarks>Conversion will also include the children of the current ElementDefinition and any
    /// sibling slice elements, if the current element is a slice intro.</remarks>
    private ElementSchema convertElementToSchema(Canonical schemaId, ElementDefinitionNavigator nav, SubschemaCollector? subschemas = null)
    {
        var schemaMembers = ConvertElement(nav, subschemas);
        //  var id = "#" + nav.Current.ElementId ?? nav.Current.Path;
        return new ElementSchema(schemaId, schemaMembers);
    }

    /// <summary>
    /// Converts the current <see cref="ElementDefinition"/> inside an <see cref="ElementDefinitionNavigator"/>
    /// to an ElementSchema.
    /// </summary>
    /// <remarks>Conversion will also include the children of the current ElementDefinition and any
    /// sibling slice elements, if the current element is a slice intro.</remarks>
    internal List<IAssertion> ConvertElement(ElementDefinitionNavigator nav, SubschemaCollector? subschemas = null)
    {
        try
        {
            // We will generate a separate schema for backbones in resource/type definitions, so
            // a contentReference can reference it. Note: contentReference always refers to the
            // unconstrained base type, not the constraints in this profile. See
            // https://chat.fhir.org/#narrow/stream/179252-IG-creation/topic/Clarification.20on.20contentReference
            bool generateBackbone = nav.Current.IsBackboneElement()
                                    && nav.StructureDefinition.Derivation != StructureDefinition.TypeDerivationRule.Constraint
                                    && subschemas?.NeedsSchemaFor("#" + nav.Current.Path) == true;

            // This will generate most of the assertions for the current ElementDefinition,
            // except for the Children and slicing assertions (done below). The exact set of
            // assertions generated depend on whether this is going to be the schema
            // for a normal element or for a subschema representing a Backbone element.
            var conversionMode = generateBackbone ?
                ElementConversionMode.BackboneType :
                ElementConversionMode.Full;

            var schemaMembers = convert(nav, conversionMode);

            // Children need special treatment since the definition of this assertion does not
            // depend on the current ElementNode, but on its descendants in the ElementDefNavigator.
            if (nav.HasChildren)
            {
                var childrenAssertion = createChildrenAssertion(nav, subschemas, out var valueAssertion, out var requiredAssertion);
                if (valueAssertion is not null)
                    schemaMembers.Add(stripGroupLevelCardinality(valueAssertion));
                if(requiredAssertion is not null)
                    schemaMembers.Add(requiredAssertion);
                schemaMembers.Add(childrenAssertion);
                    
                // This is a temporary hack for the issue where snapshot generator won't copy the invariants from base when pulling all children into the ElementDefinitionNavigator.
                // Extra details in this issue https://github.com/FirelyTeam/firely-validator-api/issues/491#issuecomment-2897145768
                // Should be cleaned up once https://github.com/FirelyTeam/firely-net-sdk/issues/3156 is solved
                // Type we're working with was pulled into definition, and we're not in a slice
                // so we need to validate any invariant rules we might find as well, but we shouldn't pull them twice.
                // We can skip backbone elements though, as there's nothing to copy.
                if (schemaMembers.OfType<BaseType>().Any() && !schemaMembers.OfType<FhirPathValidator>().Any()
                                                           && string.IsNullOrEmpty(nav.Current.SliceName) && !nav.Current.IsBackboneElement()) 
                {
                    schemaMembers.Add(new BaseTypeInvariantConstraintsValidator());
                }
            }

            // Slicing also needs to navigate to its sibling ElementDefinitions,
            // so we are dealing with it here separately.
            if (nav.Current.Slicing != null)
            {
                var sliceAssertion = CreateSliceValidator(nav);
                if (!sliceAssertion.IsAlways(ValidationResult.Success))
                    schemaMembers.Add(sliceAssertion);
            }

            if (generateBackbone)
            {
                // If the schema generated is to be a subschema, put it in the
                // list of subschemas we're creating.
                var anchor = "#" + nav.Current.Path;

                subschemas?.AddSchema(new ElementSchema(anchor, schemaMembers));

                // Then represent the current backbone element exactly the
                // way we would do for elements with a contentReference (without
                // the contentReference itself, this backbone won't have one) + add
                // a reference to the schema we just generated for the element.
                schemaMembers = convert(nav, ElementConversionMode.ContentReference);
                schemaMembers.Add(new SchemaReferenceValidator(nav.StructureDefinition.Url + anchor));
            }

            return schemaMembers;
        }
        catch (Exception e) when (e is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to convert ElementDefinition at " +
                                                $"{nav.Current.ElementId ?? nav.Current.Path} in profile {nav.StructureDefinition.Url}: {e.Message}",
                e);
        }
    }

    private List<IAssertion> convert(
        ElementDefinitionNavigator nav,
        ElementConversionMode? conversionMode = ElementConversionMode.Full)
    {
        return
            _schemaBuilders
                .SelectMany(ext => ext.Build(nav.ShallowCopy(), conversionMode))
                .ToList();
    }

    //// This corrects for the mistake where the author has a smaller root cardinality for a slice group than the minimum enforced by the
    //// individual slices. The old validator handled this gracefully, we need to actually generate a corrected cardinality for the 
    //// root.
    //private ElementSchema EnsureMinimumCardinality(ElementSchema schema, IAssertion sliceAssertion, int? min)
    //{
    //    // When the minimum of the slices is 0, we don't need to change the lower boundary
    //    if (min is null || min.Value == 0) return schema;

    //    schema = schema switch
    //    {
    //        { CardinalityValidators.Count: 0 } => schema.WithMembers(sliceAssertion),
    //        { CardinalityValidators.Count: 1 } cv => ensureMinimumCardinality(schema, min).WithMembers(sliceAssertion),
    //        _ => throw new NotSupportedException("Did not expect a slice to have more than one cardinality validator."),
    //    };
    //    return schema;

    //    static ElementSchema ensureMinimumCardinality(ElementSchema original, int? min)
    //    {
    //        var oldCardMax = original.CardinalityValidators.Single().Max;
    //        var newMax = (oldCardMax, min) switch
    //        {
    //            (null, _) => null,
    //            (var mx, null) => mx,
    //            (var mx, var mi) => Math.Max(mx.Value, mi.Value)
    //        };

    //        var membersWithoutCard = original.Members.Where(m => m != original.CardinalityValidators.Single());
    //        return original.WithNewMembers(membersWithoutCard.Prepend(new CardinalityValidator(min, newMax)));
    //    }

    //}

    private IAssertion createChildrenAssertion(
        ElementDefinitionNavigator parent,
        SubschemaCollector? subschemas,
        out IAssertion? valueAssertion, out IAssertion? requiredAssertion)
    {
        // Recurse into children, make sure we do that on a (shallow) copy of
        // the navigator.
        var childNav = parent.ShallowCopy();
        var parentElementDef = parent.Current;

        // The children assertion may or may not allow children beyond those that we find below
        // the current ElementDefinition. There are two cases when this is allowed:
        // * This is an abstract type: the schema's for the concrete type deriving from this
        //   schema will *not* allow additional children, but since we are the super type, it is
        //   perfectly normal to encounter children that have not yet been defined in this abstract
        //   type. Question: should we generate schema's for abstract types at all, or will the
        //   contents of these types always be included in concrete types by way of the snap gen?
        // * We are "inside" a type and we find that we are on an element of an abstract type
        //   having in-place constraints on its children. Question: shouldn't this include BackboneElement?
        //   Wouldn't this list always be complete because the snapgen adds all children in this
        //   case?
        bool atTypeRoot = parentElementDef.IsRootElement();
        bool allowAdditionalChildren = (!atTypeRoot && parentElementDef.IsResourcePlaceholder()) ||
                                       (atTypeRoot && parent.StructureDefinition.Abstract == true);

        return new ChildrenValidator(harvestChildren(childNav, subschemas, out valueAssertion, out requiredAssertion), allowAdditionalChildren);
    }

    private IReadOnlyDictionary<string, IAssertion> harvestChildren(
        ElementDefinitionNavigator childNav,
        SubschemaCollector? subschemas,
        out IAssertion? valueAssertion,
        out IAssertion? requiredAssertion
    )
    {
        var children = new Dictionary<string, IAssertion>();
        var requiredChildren = new List<string>();
        valueAssertion = null;
            
        childNav.MoveToFirstChild();

        do
        {
            var childPath = childNav.Current?.Base?.Path is { } basePath
                ? trimPath(basePath)
                : trimPath(childNav.Path);
                
            if (childNav.Current?.Min is > 0 && !childNav.Current.IsPrimitiveValueConstraint())
            {
                // If the element is required, we need to add it to the list of required elements.
                requiredChildren.Add(childPath);
            }
                
            var childAssertions = ConvertElement(childNav, subschemas);

            if (children.ContainsKey(childPath))
            {
                // After we're done processing the previous child, our next elment still appears to have the same path...
                // This means the previous element was sliced, without us being able to correctly parse the slice. We rather fail than
                // produce incorrect schemas here....
                throw new IncorrectElementDefinitionException($"Encountered an invalid or incomplete slice at element '{childNav.Path}', which cannot be understood by the validation.");
            }

            // Don't add empty schemas (i.e. empty ElementDefs in a differential)
            if (childAssertions.Count != 0)
            {
                var childSchema = new ElementSchema("#" + childNav.Path, childAssertions);
                if (childNav.Current?.IsPrimitiveValueConstraint() is true)
                {
                    valueAssertion = childSchema;
                    continue;
                }
                children.Add(childPath, childSchema);
            }
        }
        while (childNav.MoveToNext());

        requiredAssertion = requiredChildren.Count != 0 ? new RequiredValidator(requiredChildren) : null;;

        return children;

        string trimPath(string s)
        {
            var start = s.LastIndexOf('.') + 1;
            var end = s.EndsWith("[x]") ? s.Length - 3 : s.Length;
                
            if (start < 0 || end <= start)
            {
                // No path to trim, return the original.
                return s;
            }
            else
            {
                // Trim the path to the last dot, or the end of the string.
                return s[start..end];
            }
        }
    }

    /// <summary>
    /// Generates a <see cref="SliceValidator"/> based on the slices for the slice
    /// intro that is the current element in <paramref name="root"/>.
    /// </summary>
    internal IAssertion CreateSliceValidator(ElementDefinitionNavigator root)
    {
        if (root.Current.Slicing is null)
            throw new IncorrectElementDefinitionException($"Encountered an ElementDefinition {root.Current.ElementId} that has no slicing, but is expected to be a slice intro.");
        var slicing = root.Current.Slicing!;
        var sliceList = new List<SliceValidator.SliceCase>();
        var discriminatorless = !slicing.Discriminator.Any();
        IAssertion? defaultSlice = null;

        var memberslices = findMemberSlices(root).ToList();

        foreach (var slice in memberslices)
        {
            root.ReturnToBookmark(slice);

            var sliceName = root.Current.SliceName;
            var schemaId = "#" + root.Current.ElementId;
            if (sliceName == "@default")
            {
                // special case: set of rules that apply to all of the remaining content that is not in one of the 
                // defined slices. 
                defaultSlice = convertElementToSchema(schemaId, root);
            }
            else
            {
                // no discriminator leads to (expensive) "discriminator-less matching", which
                // means whether you are part of a slice is determined by whether you match all the
                // constraints of the slice, so the condition for this slice is all of the constraints
                // of the slice
                var condition = discriminatorless ?
                    convertElementToSchema(schemaId + ":" + "condition", root)
                    : buildDiscriminatorCondition(slicing, root);

                // Check for always true/false cases.
                if (condition is IFixedResult ra)
                    throw new IncorrectElementDefinitionException($"Encountered an ElementDefinition {root.Current.ElementId} that always" +
                                                                  $"results in {ra.FixedResult} for its discriminator(s) and therefore cannot be used as a slicing discriminator.");

                // If this is a normal slice, the constraints for the case to run are the constraints under this node.
                // In the case of a discriminator-less match, the case condition itself was a full validation of all
                // the constraints for the case, so a match means the result is a success (and failure will end up in the
                // default).
                IAssertion caseConstraints = discriminatorless ? ResultAssertion.SUCCESS : convertElementToSchema(schemaId, root);

                sliceList.Add(new SliceValidator.SliceCase(sliceName ?? root.Current.ElementId!, condition, caseConstraints, root.Current.Min > 0));
            }
        }

        // Always make sure there is a default slice. Either an explicit one (@default above), or a slice that
        // allows elements to be in the default slice, depending on whether the slice is closed.
        defaultSlice ??= createDefaultSlice(slicing);

        // And we're done.
        // One optimization: if there are no slices, we can immediately assume the default case.
        return sliceList.Count == 0
            ? defaultSlice
            : new SliceValidator(slicing.Ordered ?? false, slicing.Rules == SlicingRules.OpenAtEnd, defaultSlice, sliceList);
    }

    /// <summary>
    /// Enumerate any succeeding direct child slices of the current slice intro.
    /// Skip any intermediate child elements and re-slice elements.
    /// When finished, the navigator will be at the next sibling member of the slice intro
    /// that is not part of the slice.
    /// </summary>
    /// <param name="intro"></param>
    /// <returns>A sequence of <see cref="Bookmark"/> instances for the positions of the child slices.</returns>
    /// <remarks>This is an improved version of <see cref="ElementNavigatorSlicingExtensions.FindMemberSlices(ElementDefinitionNavigator)"/> that
    /// will throw an Exception when it encounters a slice without a slicename. The SDK version will just stop enumerating slices in that case.</remarks>
    private static IEnumerable<Bookmark> findMemberSlices(ElementDefinitionNavigator intro)
    {
        if (intro.Current.Slicing is null) throw new ArgumentException("Member slices can only be found relative to an intro slice.");

        var pathName = intro.PathName;
        var introSliceName = intro.Current.SliceName;

        while (intro.MoveToNext(pathName))
        {
            var currentSliceName = intro.Current.SliceName ??
                                   throw new IncorrectElementDefinitionException($"Encountered a slice that has no slice name.");

            if (ElementDefinitionNavigator.IsDirectSliceOf(currentSliceName, introSliceName))
            {
                yield return intro.Bookmark();
            }
        }
    }

    private static IAssertion createDefaultSlice(SlicingComponent slicing) =>
        slicing.Rules == SlicingRules.Closed ?
            new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, "Element does not match any slice and the group is closed.")
            : ResultAssertion.SUCCESS;

    private IAssertion buildDiscriminatorCondition(SlicingComponent slicing, ElementDefinitionNavigator slice)
    {
        var sliceAssertions = slicing.Discriminator
            .Select(d => DiscriminatorFactory.Build(slice, d, Source))
            .ToArray();
            
        if (sliceAssertions.All(sa => sa is null))
        {
            var paths = string.Join(',', slicing.Discriminator.Select(d => d.Path));
            throw new IncorrectElementDefinitionException($"None of the discriminating paths ({paths}) at slice {slice.CanonicalPath()} navigate to an " +
                                                          $"ElementDefinition with constraints for that discriminator.");
        }

        return sliceAssertions.Where(sa => sa is not null)!.GroupAll();
    }

    /// <summary>
    /// Strips <see cref="CardinalityValidator"/> from the members of the primitive value pseudo-child
    /// assertion before adding it as a direct schema member.
    /// The primitive value element (e.g. <c>boolean.value</c>) always has exactly 0..1 occurrences
    /// per instance. When the parent element occurs multiple times the value assertion is invoked with
    /// the whole group, causing the <see cref="CardinalityValidator"/> to count all parent instances
    /// instead of counting the children of a single instance — producing a false cardinality violation.
    /// Removing the cardinality is safe: a primitive always has at most one value per instance.
    /// </summary>
    private static IAssertion stripGroupLevelCardinality(IAssertion valueAssertion)
    {
        if (valueAssertion is not ElementSchema es) return valueAssertion;
        var membersWithoutCardinality = es.Members.Where(m => m is not CardinalityValidator).ToList();
        return membersWithoutCardinality.Count == es.Members.Count
            ? valueAssertion  // nothing to strip
            : new ElementSchema(es.Id, membersWithoutCardinality);
    }


}

