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
    public class SchemaConverter
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
                return new ElementSchema(id, ConvertElement(nav).Members);
            }
        }

        public IElementSchema ConvertElement(ElementDefinitionNavigator nav)
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
                var sliceAssertion = CreateSliceAssertion(nav);
                schema = schema.With(sliceAssertion);
            }

            return schema;
        }


        public IAssertion CreateSliceAssertion(ElementDefinitionNavigator root)
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
                    defaultSlice = ConvertElement(root);
                }
                else
                {
                    var condition = slicing.Discriminator switch
                    {
                        // no discriminator leads to (expensive) "discriminator-less matching", which
                        // means whether you are part of a slice is determined by whether you match all the
                        // constraints of the slice, so the condition for this slice is all of the constraints
                        // of the slice
                        { Count: 0 } => ConvertElement(root),

                        // A single discriminator (very common), build a special condition assertion based
                        // on the discriminator.
                        { Count: 1 } => DiscriminatorFactory.Build(root, slicing.Discriminator.Single(), Source),

                        // Multiple discriminators, the extended case of above, but now all discriminators must
                        // hold, so we'll wrap an All around them.
                        _ => new AllAssertion(slicing.Discriminator.Select(d => DiscriminatorFactory.Build(root, d, Source)))
                    };

                    // If this is a normal slice, the constraints for the case to run are the constraints under this node.
                    // In the case of a discriminator-less match, the case condition itself was a full validation of all
                    // the constraints for the case, so a match means the result is a success (and failure will end up in the
                    // default).
                    IAssertion caseConstraints = slicing.Discriminator.Any() ? ConvertElement(root) : ResultAssertion.SUCCESS;

                    sliceList.Add(new SliceAssertion.Slice(sliceName ?? root.Current.ElementId, condition, caseConstraints));
                }
            }

            // Always make sure there is a default slice. Either an explicit one (@default above), or a slice that
            // allows elements to be in the default slice, depending on whether the slice is closed.
            defaultSlice ??= createDefaultSlice(slicing);

            // And we're done.
            return new SliceAssertion(slicing.Ordered ?? false, defaultSlice, sliceList);
        }

        private IAssertion createDefaultSlice(SlicingComponent slicing) =>
            slicing.Rules == SlicingRules.Closed ?
                ResultAssertion.CreateFailure(
                    new IssueAssertion(Issue.CONTENT_ELEMENT_FAILS_SLICING_RULE, "TODO: location?", "Element does not match any slice and the group is closed."))
            : ResultAssertion.SUCCESS;


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
    }
}
