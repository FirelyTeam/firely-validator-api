/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Firely.Fhir.Validation;
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
    public class SchemaConverter
    {
        public readonly IAsyncResourceResolver Source;

        public SchemaConverter(IAsyncResourceResolver source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public ElementSchema Convert(StructureDefinition definition)
        {
            var nav = ElementDefinitionNavigator.ForSnapshot(definition);
            return Convert(nav);
        }

        public ElementSchema Convert(ElementDefinitionNavigator nav)
        {
            //Enable this when you need a snapshot of a test SD written out in your %TEMP%/testprofiles dir.
            //string p = Path.Combine(Path.GetTempPath(), "testprofiles", nav.StructureDefinition.Id + ".snap");
            //File.WriteAllText(p, nav.StructureDefinition.ToXml());
            bool hasContent = nav.MoveToFirstChild();

            var id = new Uri(nav.StructureDefinition.Url, UriKind.Absolute);

            if (!hasContent)
                return new ElementSchema(id);
            else
            {
                try
                {
                    // Note how the root element (first element of an SD) is integrated within
                    // the schema representing the SD as a whole by including just the members
                    // of the schema generated from the first ElementDefinition.
                    return new ElementSchema(id, ConvertElement(nav).Members);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Failed to convert ElementDefinition at " +
                        $"{nav.Current.ElementId ?? nav.Current.Path} in profile {nav.StructureDefinition.Url}: {e.Message}",
                        e);
                }
            }
        }

        public ElementSchema ConvertElement(ElementDefinitionNavigator nav)
        {
            // This will generate most of the assertions for the current ElementDefinition,
            // except for the Children and slicing assertions (done below).
            var schema = nav.Current.Convert();

            // Children need special treatment since the definition of this assertion does not
            // depend on the current ElementNode, but on its descendants in the ElementDefNavigator.
            if (nav.HasChildren)
            {
                var childrenAssertion = createChildrenAssertion(nav);
                schema = schema.With(childrenAssertion);
            }

            // Slicing also needs to navigate to its sibling ElementDefinitions,
            // so we are dealing with it here separately.
            if (nav.IsSlicing())
            {
                var sliceAssertion = CreateSliceAssertion(nav);
                schema = schema.With(sliceAssertion);
            }

            return schema;
        }


        private IAssertion createChildrenAssertion(ElementDefinitionNavigator parent)
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

            return new Children(harvestChildren(childNav), allowAdditionalChildren);
        }

        private IReadOnlyDictionary<string, IAssertion> harvestChildren(ElementDefinitionNavigator childNav)
        {
            var children = new Dictionary<string, IAssertion>();

            childNav.MoveToFirstChild();
            var xmlOrder = 0;

            do
            {
                xmlOrder += 10;
                var childSchema = ConvertElement(childNav);

                // Don't add empty schemas (i.e. empty ElementDefs in a differential)
                if (!childSchema.IsEmpty())
                {
                    var schemaWithOrder = childSchema.With(new XmlOrder(xmlOrder));
                    children.Add(childNav.PathName, schemaWithOrder);
                }
            }
            while (childNav.MoveToNext());

            return children;
        }


        public IAssertion CreateSliceAssertion(ElementDefinitionNavigator root)
        {
            var slicing = root.Current.Slicing;
            var sliceList = new List<SliceAssertion.Slice>();
            var discriminatorless = !slicing.Discriminator.Any();
            IAssertion? defaultSlice = null;

            var memberslices = root.FindMemberSlices().ToList();

            foreach (var slice in memberslices)
            {
                root.ReturnToBookmark(slice);

                var sliceName = root.Current.SliceName;

                if (sliceName == "@default")
                {
                    // special case: set of rules that apply to all of the remaining content that is not in one of the 
                    // defined slices. 
                    defaultSlice = ConvertElement(root);
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

                    sliceList.Add(new SliceAssertion.Slice(sliceName ?? root.Current.ElementId, condition, caseConstraints));
                }
            }

            // Always make sure there is a default slice. Either an explicit one (@default above), or a slice that
            // allows elements to be in the default slice, depending on whether the slice is closed.
            defaultSlice ??= createDefaultSlice(slicing);

            // And we're done.
            // One optimization: if there are no slices, we can immediately assume the default case.
            return sliceList.Count == 0
                ? defaultSlice
                : new SliceAssertion(slicing.Ordered ?? false, slicing.Rules == SlicingRules.OpenAtEnd, defaultSlice, sliceList);
        }

        private static IAssertion createDefaultSlice(SlicingComponent slicing) =>
            slicing.Rules == SlicingRules.Closed ?
                ResultAssertion.CreateFailure(
                    new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, "TODO: location?", "Element does not match any slice and the group is closed."))
            : ResultAssertion.SUCCESS;

    }
}
