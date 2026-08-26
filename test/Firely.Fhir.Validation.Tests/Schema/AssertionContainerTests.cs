/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class AssertionContainerTests
    {
        private static readonly IAssertion LEAF = new FhirTypeLabelValidator("Patient");
        private static readonly IAssertion REPLACEMENT = new FhirTypeLabelValidator("Observation");
        private static readonly IAssertion OTHERLEAF = new MaxLengthValidator(10);

        private static readonly StructureDefinitionInformation SDINFO =
            new("http://test.org/patientschema", null, "Patient", null, false);

        /// <summary>
        /// Rewrites <paramref name="container"/>, replacing <see cref="LEAF"/> by <see cref="REPLACEMENT"/>
        /// and leaving everything else alone.
        /// </summary>
        private static IAssertion replaceLeaf(IAssertionContainer container) =>
            container.WithChildren((_, child) => ReferenceEquals(child, LEAF) ? REPLACEMENT : child);

        private static IAssertion identity(IAssertionContainer container) =>
            container.WithChildren((_, child) => child);

        /// <summary>
        /// Rewrites the first child of <paramref name="container"/> into something else that still fits its
        /// slot, leaving the other children alone.
        /// </summary>
        private static IAssertion replaceFirstChild(IAssertionContainer container)
        {
            var replaced = false;

            return container.WithChildren((_, child) =>
            {
                if (replaced) return child;
                replaced = true;

                // subschema slots only take schemas, so replace a schema's members instead of the schema
                return child is ElementSchema schema ? schema.WithMembers([REPLACEMENT]) : REPLACEMENT;
            });
        }

        #region the guard: no container may be forgotten

        /// <summary>
        /// Every assertion that keeps nested assertions as part of its (serialized) state must implement
        /// <see cref="IAssertionContainer"/>, otherwise a schema rewrite would silently not descend into it.
        /// </summary>
        /// <remarks>This covers the assertions in the core assembly - the version-specific and compilation
        /// assemblies do not define assertions of their own.</remarks>
        [TestMethod]
        public void EveryAssertionWithNestedAssertionsIsAContainer()
        {
            var forgotten = typeof(IAssertion).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IAssertion).IsAssignableFrom(t))
                .Where(holdsNestedAssertions)
                .Where(t => !typeof(IAssertionContainer).IsAssignableFrom(t))
                .Select(t => t.Name)
                .ToList();

            Assert.AreEqual(0, forgotten.Count,
                $"These assertions hold nested assertions but do not implement {nameof(IAssertionContainer)}, " +
                $"so a schema rewrite cannot descend into them: {string.Join(", ", forgotten)}.");
        }

        /// <summary>
        /// The containers we know about today, as a canary: if this list needs updating, the guard above
        /// needs a look as well.
        /// </summary>
        [TestMethod]
        public void KnownContainersAreTheContainersWeExpect()
        {
            var containers = typeof(IAssertion).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IAssertionContainer).IsAssignableFrom(t))
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToList();

            CollectionAssert.AreEqual(new[]
            {
                nameof(AllValidator),
                nameof(AnyValidator),
                nameof(ChildrenValidator),
                nameof(DatatypeSchema),
                nameof(DefinitionsAssertion),
                nameof(ElementSchema),
                nameof(ExtensionSchema),
                nameof(KeyedObjectValidator),
                nameof(LogicalModelSchema),
                nameof(PathSelectorValidator),
                nameof(ReferencedInstanceValidator),
                nameof(ResourceSchema),
                nameof(SliceValidator),
            }, containers.ToArray(), string.Join(", ", containers));
        }

        private static bool holdsNestedAssertions(Type type) => involvesAssertions(type, []);

        /// <summary>
        /// Whether <paramref name="type"/> has serialized state that (directly or through a helper type
        /// like <see cref="SliceValidator.SliceCase"/>) holds assertions.
        /// </summary>
        private static bool involvesAssertions(Type type, HashSet<Type> visited)
        {
            if (!visited.Add(type)) return false;

            return dataMembers(type).Any(memberType => referencesAssertions(memberType, visited));

            static IEnumerable<Type> dataMembers(Type type) =>
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(p => p.GetCustomAttribute<DataMemberAttribute>() is not null)
                    .Select(p => p.PropertyType)
                    .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(f => f.GetCustomAttribute<DataMemberAttribute>() is not null)
                        .Select(f => f.FieldType));
        }

        private static bool referencesAssertions(Type type, HashSet<Type> visited) =>
            typeof(IAssertion).IsAssignableFrom(type)
                || (type.IsArray && referencesAssertions(type.GetElementType()!, visited))
                || (type.IsGenericType && type.GetGenericArguments().Any(a => referencesAssertions(a, visited)))
                // A helper type of our own (SliceCase, TargetCase) can hold the assertions instead.
                || (type.Assembly == typeof(IAssertion).Assembly && !type.IsEnum && involvesAssertions(type, visited));

        #endregion

        #region steps describe the edges

        [TestMethod]
        public void SchemaMembersAreNeutralSteps()
        {
            var schema = new ElementSchema("#test", LEAF, OTHERLEAF);

            var children = ((IAssertionContainer)schema).Children();

            Assert.AreEqual(2, children.Count);
            Assert.IsTrue(children.All(c => c.Step == AssertionStep.Member()));
            Assert.AreSame(LEAF, children[0].Child);
            Assert.AreSame(OTHERLEAF, children[1].Child);
        }

        [TestMethod]
        public void ChildrenAreNamedSteps()
        {
            var children = new ChildrenValidator(false, ("value[x]", LEAF));

            var step = ((IAssertionContainer)children).Children().Single();

            Assert.AreEqual(AssertionStepKind.Child, step.Step.Kind);
            Assert.AreEqual("value[x]", step.Step.ElementName);
            Assert.IsNull(step.Step.SliceName);
            Assert.AreEqual(".value[x]", step.Step.ToString());
        }

        [TestMethod]
        public void SlicesAreNamedStepsAndDiscriminatorsAreNotVisited()
        {
            var discriminator = new PathSelectorValidator("system", LEAF);
            var slices = new SliceValidator(false, false, OTHERLEAF,
                new SliceValidator.SliceCase("theSlice", discriminator, LEAF));

            var children = ((IAssertionContainer)slices).Children();

            Assert.AreEqual(2, children.Count);
            Assert.AreEqual(AssertionStepKind.Slice, children[0].Step.Kind);
            Assert.AreEqual("theSlice", children[0].Step.SliceName);
            Assert.AreSame(LEAF, children[0].Child);

            // the default is a neutral member, the discriminator is not a child at all
            Assert.AreEqual(AssertionStep.Member(), children[1].Step);
            Assert.AreSame(OTHERLEAF, children[1].Child);
            Assert.IsFalse(children.Any(c => ReferenceEquals(c.Child, discriminator)));
        }

        [TestMethod]
        public void SubschemasAreAnchoredSteps()
        {
            var definitions = new DefinitionsAssertion(new ElementSchema("#Observation.component", LEAF));

            var step = ((IAssertionContainer)definitions).Children().Single().Step;

            Assert.AreEqual(AssertionStepKind.Subschema, step.Kind);
            Assert.AreEqual("Observation.component", step.Anchor);
            Assert.AreEqual("#Observation.component", step.ToString());
        }

        [TestMethod]
        public void ReferenceTargetsCarryTheirType()
        {
            var typed = new ReferencedInstanceValidator([new ReferencedInstanceValidator.TargetCase("Patient", LEAF)]);
            var untyped = new ReferencedInstanceValidator(LEAF);

            var typedStep = ((IAssertionContainer)typed).Children().Single().Step;
            Assert.AreEqual(AssertionStepKind.ReferenceTarget, typedStep.Kind);
            Assert.AreEqual("Patient", typedStep.TargetType);
            Assert.AreEqual("->Patient", typedStep.ToString());

            var untypedStep = ((IAssertionContainer)untyped).Children().Single().Step;
            Assert.AreEqual(AssertionStepKind.ReferenceTarget, untypedStep.Kind);
            Assert.IsNull(untypedStep.TargetType);
            Assert.AreEqual("->*", untypedStep.ToString());
        }

        #endregion

        #region an unchanged container keeps its identity

        [TestMethod]
        public void RewritingNothingReturnsTheContainerItself()
        {
            foreach (var container in allContainers())
                Assert.AreSame((object)container, identity(container), container.GetType().Name);
        }

        [TestMethod]
        public void RewritingAChildProducesACopy()
        {
            foreach (var container in allContainers())
            {
                var name = container.GetType().Name;
                var original = container.Children();
                var rewritten = replaceFirstChild(container);

                Assert.AreNotSame((object)container, rewritten, name);
                Assert.AreEqual(container.GetType(), rewritten.GetType(), name);

                var children = ((IAssertionContainer)rewritten).Children();
                Assert.AreEqual(original.Count, children.Count, name);
                Assert.AreEqual(original[0].Step, children[0].Step, name);
                Assert.AreNotSame(original[0].Child, children[0].Child, name);

                // the children that were handed back unchanged keep their identity
                for (var index = 1; index < original.Count; index++)
                    Assert.AreSame(original[index].Child, children[index].Child, name);
            }
        }

        private static IEnumerable<IAssertionContainer> allContainers()
        {
            yield return new ElementSchema("#test", LEAF);
            yield return new ResourceSchema(SDINFO, LEAF);
            yield return new DatatypeSchema(SDINFO, LEAF);
            yield return new ExtensionSchema(SDINFO, LEAF);
            yield return new LogicalModelSchema(SDINFO, LEAF);
            yield return new ChildrenValidator(false, ("child", LEAF));
            yield return new SliceValidator(false, false, OTHERLEAF, new SliceValidator.SliceCase("s", OTHERLEAF, LEAF));
            yield return new AllValidator([LEAF, OTHERLEAF]);
            yield return new AnyValidator([LEAF, OTHERLEAF]);
            yield return new DefinitionsAssertion(new ElementSchema("#sub", LEAF));
            yield return new ReferencedInstanceValidator(LEAF);
            yield return new ReferencedInstanceValidator([new ReferencedInstanceValidator.TargetCase("Patient", LEAF)]);
            yield return new KeyedObjectValidator(LEAF);
            yield return new PathSelectorValidator("system", LEAF);
        }

        #endregion

        #region a copy preserves everything but the children

        [TestMethod]
        public void SchemaCopiesKeepTheirTypeAndId()
        {
            var schema = new ResourceSchema(SDINFO, LEAF, OTHERLEAF);

            var rewritten = (ResourceSchema)replaceLeaf(schema);

            Assert.AreSame(SDINFO, rewritten.StructureDefinition);
            Assert.AreEqual(schema.Id, rewritten.Id);
            Assert.AreSame(OTHERLEAF, rewritten.Members.Last());
        }

        [TestMethod]
        public void ChildrenValidatorCopiesKeepTheirOpenness()
        {
            var children = new ChildrenValidator(true, ("a", LEAF), ("b", OTHERLEAF));

            var rewritten = (ChildrenValidator)replaceLeaf(children);

            Assert.IsTrue(rewritten.AllowAdditionalChildren);
            Assert.AreSame(REPLACEMENT, rewritten.Lookup("a"));
            Assert.AreSame(OTHERLEAF, rewritten.Lookup("b"));
        }

        [TestMethod]
        public void SliceCopiesKeepTheirSlicingRules()
        {
            var discriminator = new PathSelectorValidator("system", OTHERLEAF);
            var slices = new SliceValidator(ordered: true, defaultAtEnd: true, @default: OTHERLEAF,
                slices: [new SliceValidator.SliceCase("theSlice", discriminator, LEAF, required: true)],
                multiCase: true);

            var rewritten = (SliceValidator)replaceLeaf(slices);

            Assert.IsTrue(rewritten.Ordered);
            Assert.IsTrue(rewritten.DefaultAtEnd);
            Assert.IsTrue(rewritten.MultiCase);
            Assert.AreSame(OTHERLEAF, rewritten.Default);

            var slice = rewritten.Slices.Single();
            Assert.AreEqual("theSlice", slice.Name);
            Assert.IsTrue(slice.Required);
            Assert.AreSame(discriminator, slice.Condition);
            Assert.AreSame(REPLACEMENT, slice.Assertion);
        }

        [TestMethod]
        public void AllAndAnyCopiesKeepTheirSettings()
        {
            var all = new AllValidator([LEAF], shortcircuitEvaluation: true);
            var summaryError = new IssueAssertion(Issue.CONTENT_ELEMENT_MUST_MATCH_TYPE, "no match");
            var any = new AnyValidator([LEAF], summaryError);

            Assert.IsTrue(((AllValidator)replaceLeaf(all)).ShortcircuitEvaluation);
            Assert.AreSame(summaryError, ((AnyValidator)replaceLeaf(any)).SummaryError);

            // the summary error is not a child, so it is not offered for rewriting either
            Assert.IsFalse(((IAssertionContainer)any).Children().Any(c => ReferenceEquals(c.Child, summaryError)));
        }

        [TestMethod]
        public void ReferenceCopiesKeepTheirChecksAndRules()
        {
            var riv = new ReferencedInstanceValidator(
                [new ReferencedInstanceValidator.TargetCase("Patient", LEAF)],
                aggregationRules: [AggregationMode.Bundled],
                versioningRules: ReferenceVersionRules.Independent,
                checks: ReferenceChecks.Exists | ReferenceChecks.TargetType);

            var rewritten = (ReferencedInstanceValidator)replaceLeaf(riv);

            Assert.AreEqual(ReferenceChecks.Exists | ReferenceChecks.TargetType, rewritten.Checks);
            Assert.AreEqual(ReferenceVersionRules.Independent, rewritten.VersioningRules);
            CollectionAssert.AreEqual(new[] { AggregationMode.Bundled }, rewritten.AggregationRules!.ToArray());
            Assert.AreEqual("Patient", rewritten.TargetCases!.Single().Type);
            Assert.AreSame(REPLACEMENT, rewritten.TargetCases!.Single().Schema);
        }

        [TestMethod]
        public void KeyedObjectCopiesKeepTheirCardinality()
        {
            var keyed = new KeyedObjectValidator(LEAF, min: 1, max: 3);

            var rewritten = (KeyedObjectValidator)replaceLeaf(keyed);

            Assert.AreEqual(1, rewritten.Min);
            Assert.AreEqual(3, rewritten.Max);
            Assert.AreSame(REPLACEMENT, rewritten.EntryAssertion);
        }

        #endregion

        [TestMethod]
        public void ASubschemaMustBeRewrittenToASchema()
        {
            var definitions = new DefinitionsAssertion(new ElementSchema("#sub", LEAF));

            // returning a non-schema for a subschema slot would break resolution by anchor
            var error = Assert.ThrowsException<InvalidOperationException>(() =>
                ((IAssertionContainer)definitions).WithChildren((_, _) => OTHERLEAF));

            Assert.IsTrue(error.Message.Contains("#sub"), error.Message);
        }

        [TestMethod]
        public void ARewriteReachesEveryNestedAssertion()
        {
            // Patient.identifier[theSlice].value, plus a subschema and a reference target
            var tree = new ResourceSchema(SDINFO,
                new ChildrenValidator(false,
                    ("identifier", new ElementSchema("#Patient.identifier",
                        new SliceValidator(false, false, ResultAssertion.SUCCESS,
                            new SliceValidator.SliceCase("theSlice", ResultAssertion.SUCCESS,
                                new ChildrenValidator(false, ("value", new ElementSchema("#value", LEAF))))))),
                    ("managingOrganization", new ElementSchema("#Patient.managingOrganization",
                        new ReferencedInstanceValidator([new ReferencedInstanceValidator.TargetCase("Organization", LEAF)])))),
                new DefinitionsAssertion(new ElementSchema("#Patient.contact", LEAF)));

            var visited = new List<string>();
            var rewritten = rewrite(tree);

            Assert.AreEqual(3, visited.Count(v => v == "leaf"), string.Join(", ", visited));
            Assert.AreNotSame(tree, rewritten);
            Assert.IsInstanceOfType(rewritten, typeof(ResourceSchema));

            IAssertion rewrite(IAssertion assertion)
            {
                if (ReferenceEquals(assertion, LEAF))
                {
                    visited.Add("leaf");
                    return REPLACEMENT;
                }

                return assertion is IAssertionContainer container
                    ? container.WithChildren((_, child) => rewrite(child))
                    : assertion;
            }
        }
    }
}
