/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Firely.Fhir.Validation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Validation.Compilation
{
    internal class SchemaConverter
    {
        public readonly IAsyncResourceResolver Source;

        public SchemaConverter(IAsyncResourceResolver source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public IElementSchema Convert(StructureDefinition definition)
        {
            var nav = ElementDefinitionNavigator.ForSnapshot(definition);
            return Convert(nav);
        }

        public IElementSchema Convert(ElementDefinitionNavigator nav)
        {
            bool hasContent = nav.MoveToFirstChild();

            var id = new Uri(nav.StructureDefinition.Url, UriKind.Absolute);

            if (!hasContent)
                return new ElementSchema(id);
            else
            {
                // Note how the root element (first element of an SD) is integrated within
                // the schema representing the SD as a whole by including just the members
                // of the schema generated from the first ElementDefinition.
                return new ElementSchema(id, harvest(nav).Members);
            }
        }

        private IElementSchema harvest(ElementDefinitionNavigator nav)
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
                var sliceAssertion = createSliceAssertion(nav);
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
                var childSchema = harvest(childNav);

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


        private IAssertion createSliceAssertion(ElementDefinitionNavigator root)
        {
            var slicing = root.Current.Slicing;
            var sliceList = new List<SliceAssertion.Slice>();
            IAssertion? defaultSlice = null;

            while (root.MoveToNextSlice())
            {
                var sliceName = root.Current.SliceName;

                if (sliceName == "@default" && slicing.Rules == SlicingRules.Closed)
                {
                    // special case: set of rules that apply to all of the remaining content that is not in one of the 
                    // defined slices. 
                    defaultSlice = harvest(root);
                }
                else
                {
                    var condition = slicing.Discriminator.Any() ?
                         new AllAssertion(slicing.Discriminator.Select(d => DiscriminatorFactory.Build(root, d, Source)))
                         : harvest(root) as IAssertion; // Discriminator-less matching

                    sliceList.Add(new SliceAssertion.Slice(sliceName ?? root.Current.ElementId, condition, harvest(root)));
                }
            }

            defaultSlice ??= createDefaultSlice(slicing);
            var sliceAssertion = new SliceAssertion(slicing.Ordered ?? false, defaultSlice, sliceList);

            return new ElementSchema(new Uri($"#{root.Path}", UriKind.Relative), new[] { sliceAssertion });

            IAssertion createDefaultSlice(SlicingComponent slicing) =>
                slicing.Rules == SlicingRules.Closed ?
                    ResultAssertion.CreateFailure(
                        new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, "TODO: location?", "Element does not match any slice and the group is closed."))
                    : ResultAssertion.SUCCESS;
        }
    }
}
