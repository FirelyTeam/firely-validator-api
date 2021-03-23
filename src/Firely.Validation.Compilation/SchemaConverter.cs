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
using Hl7.Fhir.Support;
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
            var schema = nav.Current.Convert();

            if (nav.HasChildren)
            {
                var childNav = nav.ShallowCopy();   // make sure closure is to a clone, not the original argument

                bool isInlineChildren = !nav.Current.IsRootElement();
                bool allowAdditionalChildren = (isInlineChildren && nav.Current.IsResourcePlaceholder()) ||
                                     (!isInlineChildren && nav.StructureDefinition.Abstract == true);

                var childAssertion = new Children(harvestChildren(childNav), allowAdditionalChildren);
                schema = schema.With(childAssertion);
            }

            if (nav.IsSlicing())
            {
                var sliceAssertion = createSliceAssertion(nav);
                schema = schema.With(sliceAssertion);
            }

            return schema;
        }


        private IAssertion createDefaultSlice(SlicingComponent slicing)
            => slicing.Rules == SlicingRules.Closed ?
                ResultAssertion.CreateFailure(
                            new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, "TODO: location?", "Element does not match any slice and the group is closed."))
                : ResultAssertion.SUCCESS;

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
    }
}
