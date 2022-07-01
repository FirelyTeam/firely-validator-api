/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Support;
using Hl7.Fhir.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    /// Converts the constraints in a <see cref="StructureDefinition"/> to an
    /// <see cref="ElementSchema"/>, which can then be used for validation.
    /// </summary>
    public class SchemaConverter
    {
        /// <summary>
        /// The resolver to use when the <see cref="StructureDefinition"/> under conversion
        /// refers to other StructureDefinitions.
        /// </summary>
        public readonly IAsyncResourceResolver Source;

        /// <summary>
        /// Initializes a new SchemaConverter with a given <see cref="IAsyncResourceResolver"/>.
        /// </summary>
        public SchemaConverter(IAsyncResourceResolver source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Converts a <see cref="StructureDefinition"/> to an <see cref="ElementSchema"/>.
        /// </summary>
        public ElementSchema Convert(StructureDefinition definition)
        {
            var nav = ElementDefinitionNavigator.ForSnapshot(definition);
            return Convert(nav);
        }

        /// <summary>
        /// Converts a <see cref="StructureDefinition"/> loaded into an 
        /// <see cref="ElementDefinitionNavigator"/> to an <see cref="ElementSchema"/>.
        /// </summary>
        public ElementSchema Convert(ElementDefinitionNavigator nav)
        {
            //Enable this when you need a snapshot of a test SD written out in your %TEMP%/testprofiles dir.
            //string p = Path.Combine(Path.GetTempPath(), "testprofiles", nav.StructureDefinition.Id + ".snap");
            //File.WriteAllText(p, nav.StructureDefinition.ToXml());

            if (!nav.MoveToFirstChild()) return new ElementSchema(nav.StructureDefinition.Url);

            var subschemaCollector = new SubschemaCollector(nav);

            try
            {
                var converted = ConvertElement(nav, subschemaCollector);

                if (subschemaCollector.FoundSubschemas)
                    converted = converted.WithMembers(subschemaCollector.BuildDefinitionAssertion());

                return converted;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to convert ElementDefinition at " +
                    $"{nav.Current.ElementId ?? nav.Current.Path} in profile {nav.StructureDefinition.Url}: {e.Message}",
                    e);
            }
        }

        /// <summary>
        /// Converts the current <see cref="ElementDefinition"/> inside an <see cref="ElementDefinitionNavigator"/>
        /// to an ElementSchema.
        /// </summary>
        /// <remarks>Conversion will also include the children of the current ElementDefinition and any
        /// sibling slice elements, if the current element is a slice intro.</remarks>
        internal ElementSchema ConvertElement(ElementDefinitionNavigator nav, SubschemaCollector? subschemas = null)
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
            var isUnconstrainedElement = !nav.HasChildren;

            var schema = nav.Current.Convert(nav.StructureDefinition, isUnconstrainedElement, conversionMode);

            // Children need special treatment since the definition of this assertion does not
            // depend on the current ElementNode, but on its descendants in the ElementDefNavigator.
            if (nav.HasChildren)
            {
                var childrenAssertion = createChildrenAssertion(nav, subschemas);
                schema = schema.WithMembers(childrenAssertion);
            }

            // Slicing also needs to navigate to its sibling ElementDefinitions,
            // so we are dealing with it here separately.
            if (nav.IsSlicing())
            {
                var sliceAssertion = CreateSliceValidator(nav);
                if (sliceAssertion != ResultAssertion.SUCCESS)
                    schema = schema.WithMembers(sliceAssertion);
            }

            if (generateBackbone)
            {
                // If the schema generated is to be a subschema, put it in the
                // list of subschemas we're creating.
                var anchor = "#" + nav.Current.Path;
                subschemas?.AddSchema(schema.WithId(anchor));

                // Then represent the current backbone element exactly the
                // way we would do for elements with a contentReference (without
                // the contentReference itself, this backbone won't have one) + add
                // a reference to the schema we just generated for the element.
                schema = nav.Current.Convert(nav.StructureDefinition, isUnconstrainedElement, ElementConversionMode.ContentReference);
                schema = schema.WithMembers(
                    new SchemaReferenceValidator(nav.StructureDefinition.Url, subschema: anchor));
            }

            // When at the root, overwrite our Id with the url of the converted StructureDefinition
            if (string.IsNullOrEmpty(nav.ParentPath)) schema = schema.WithId(nav.StructureDefinition.Url);

            return schema;
        }

        private IAssertion createChildrenAssertion(
            ElementDefinitionNavigator parent,
            SubschemaCollector? subschemas)
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

            return new ChildrenValidator(harvestChildren(childNav, subschemas), allowAdditionalChildren);
        }

        private IEnumerable<ChildConstraints> harvestChildren(
            ElementDefinitionNavigator childNav,
            SubschemaCollector? subschemas
            )
        {
            var children = new List<ChildConstraints>();

            childNav.MoveToFirstChild();
            var xmlOrder = 0;

            do
            {
                xmlOrder += 10;
                var cardinality = childNav.Current.BuildCardinality();
                var childSchema = ConvertElement(childNav, subschemas);

                // Don't add empty schemas (i.e. empty ElementDefs in a differential)
                if (!childSchema.IsEmpty())
                {
                    var schemaWithOrder = childSchema.WithMembers(new XmlOrderValidator(xmlOrder));
                    children.Add(new ChildConstraints(childNav.PathName, cardinality, schemaWithOrder));
                }
            }
            while (childNav.MoveToNext());

            return children;
        }

        /// <summary>
        /// Generates a <see cref="SliceValidator"/> based on the slices for the slice
        /// intro that is the current element in <paramref name="root"/>.
        /// </summary>
        internal IAssertion CreateSliceValidator(ElementDefinitionNavigator root)
        {
            var slicing = root.Current.Slicing;
            var sliceList = new List<SliceValidator.SliceCase>();
            var discriminatorless = !slicing.Discriminator.Any();
            SliceValidator.SliceCase? defaultSlice = null;

            var memberslices = root.FindMemberSlices().ToList();

            foreach (var slice in memberslices)
            {
                root.ReturnToBookmark(slice);

                var sliceName = root.Current.SliceName;
                var cardinality = root.Current.BuildCardinality();

                if (sliceName == "@default")
                {
                    // special case: set of rules that apply to all of the remaining content that is not in one of the 
                    // defined slices. 
                    defaultSlice = SliceValidator.SliceCase.MakeDefault(ConvertElement(root), cardinality);
                }
                else
                {
                    // no discriminator leads to (expensive) "discriminator-less matching", which
                    // means whether you are part of a slice is determined by whether you match all the
                    // constraints of the slice, so the condition for this slice is all of the constraints
                    // of the slice
                    var condition = discriminatorless ?
                        ConvertElement(root)
                        : slicing.Discriminator.Select(d => DiscriminatorFactory.Build(root, d, Source)).GroupAll();

                    // Check for always true/false cases.
                    if (condition is ResultAssertion ra)
                        throw new IncorrectElementDefinitionException($"Encountered an ElementDefinition {root.Current.ElementId} that always" +
                            $"results in {ra.Result} for its discriminator(s) and therefore cannot be used as a slicing discriminator.");

                    // If this is a normal slice, the constraints for the case to run are the constraints under this node.
                    // In the case of a discriminator-less match, the case condition itself was a full validation of all
                    // the constraints for the case, so a match means the result is a success (and failure will end up in the
                    // default).
                    IAssertion caseConstraints = discriminatorless ? ResultAssertion.SUCCESS : ConvertElement(root);

                    sliceList.Add(new SliceValidator.SliceCase(sliceName ?? root.Current.ElementId, condition, cardinality, caseConstraints));
                }
            }

            // Always make sure there is a default slice. Either an explicit one (@default above), or a slice that
            // allows elements to be in the default slice, depending on whether the slice is closed.
            defaultSlice ??= createDefaultSlice(slicing);

            // And we're done.
            // One optimization: if there are no slices, we can immediately assume the default case.
            return sliceList.Count == 0
                ? defaultSlice.Assertion
                : new SliceValidator(slicing.Ordered ?? false, slicing.Rules == SlicingRules.OpenAtEnd, defaultSlice, sliceList);
        }

        private static SliceValidator.SliceCase createDefaultSlice(SlicingComponent slicing)
        {
            var assertion = slicing.Rules == SlicingRules.Closed ?
                ResultAssertion.FromEvidence(
                   new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, "TODO: location?", "Element does not match any slice and the group is closed."))
                : ResultAssertion.SUCCESS;

            // Since this is the "default" slice, there is no condition for it (a SUCCESS condition, which is always true).
            return SliceValidator.SliceCase.MakeDefault(assertion);
        }

    }
}
