/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;
using System.Collections.Generic;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The kind of step that leads from a container to one of its nested assertions.
    /// </summary>
    /// <remarks>These are the definition-side counterparts of the navigation events tracked in
    /// <see cref="PathStack"/> during validation. Only steps that are visible in the compiled schema
    /// exist here: the instance-side events (indexes, internal references, resource starts) have no
    /// meaning while rewriting a schema.</remarks>
    internal enum AssertionStepKind
    {
        /// <summary>
        /// A member of the container that does not change the position within the instance: a member of an
        /// <see cref="ElementSchema"/>, <see cref="AllValidator"/> or <see cref="AnyValidator"/>, the default
        /// of a <see cref="SliceValidator"/>, or one of the branches of a conditional container.
        /// </summary>
        Member,

        /// <summary>
        /// The assertions for a named child element, i.e. an entry in a <see cref="ChildrenValidator"/>.
        /// </summary>
        /// <remarks>The name is the name used in the instance, so it can be a choice element name
        /// (<c>value[x]</c>), or - for logical models - a renamed json property.</remarks>
        Child,

        /// <summary>
        /// The assertions for a named slice of a <see cref="SliceValidator"/>. Note that a slice does not
        /// change the position within the instance, but it does add to the definition path.
        /// </summary>
        /// <remarks>The name comes from one of two vocabularies: it is either a slice name authored in the
        /// profile, or - for the slicing a choice element compiles into, discriminated on the type label of
        /// the element itself - a FHIR type code. So the name is not necessarily an authored slice name, and
        /// does not necessarily match the <c>:sliceName</c> in the element's <c>ElementDefinition.id</c>
        /// (FHIR spells the type slices of <c>value[x]</c> as <c>:valueQuantity</c>, not <c>:Quantity</c>).</remarks>
        Slice,

        /// <summary>
        /// A subschema in a <see cref="DefinitionsAssertion"/>, identified by its anchor.
        /// </summary>
        Subschema,

        /// <summary>
        /// The schema that the target of a reference is validated against, for targets of the type named
        /// by the step (or for targets of any type, when the step has no name).
        /// </summary>
        ReferenceTarget
    }

    /// <summary>
    /// A single step from a container to one of its nested assertions - the label on the edge between them.
    /// </summary>
    /// <remarks>The step describes how the nested assertion is reached, not what it is: identity (canonical,
    /// version, schema id) is carried by the schemas encountered along the way, not by the steps.</remarks>
    internal readonly record struct AssertionStep(AssertionStepKind Kind, string? Name = null)
    {
        /// <summary>
        /// A step to a member that does not change the position within the instance.
        /// </summary>
        public static AssertionStep Member { get; } = new(AssertionStepKind.Member);

        /// <summary>
        /// A step to the assertions for the child element with the given name.
        /// </summary>
        public static AssertionStep Child(string elementName) => new(AssertionStepKind.Child, elementName);

        /// <summary>
        /// A step to the assertions for the slice with the given name.
        /// </summary>
        public static AssertionStep Slice(string sliceName) => new(AssertionStepKind.Slice, sliceName);

        /// <summary>
        /// A step to the subschema with the given anchor.
        /// </summary>
        public static AssertionStep Subschema(string anchor) => new(AssertionStepKind.Subschema, anchor);

        /// <summary>
        /// A step to the schema for the targets of a reference of the given type, or for the targets of
        /// any type when <paramref name="targetType"/> is <c>null</c>.
        /// </summary>
        public static AssertionStep ReferenceTarget(string? targetType) => new(AssertionStepKind.ReferenceTarget, targetType);

        /// <summary>
        /// The name of the child element, when this is a <see cref="AssertionStepKind.Child"/> step.
        /// </summary>
        public string? ElementName => Kind == AssertionStepKind.Child ? Name : null;

        /// <summary>
        /// The name of the slice, when this is a <see cref="AssertionStepKind.Slice"/> step.
        /// </summary>
        public string? SliceName => Kind == AssertionStepKind.Slice ? Name : null;

        /// <summary>
        /// The anchor of the subschema, when this is a <see cref="AssertionStepKind.Subschema"/> step.
        /// </summary>
        public string? Anchor => Kind == AssertionStepKind.Subschema ? Name : null;

        /// <summary>
        /// The type of the reference target, when this is a <see cref="AssertionStepKind.ReferenceTarget"/>
        /// step. <c>null</c> means targets of any type.
        /// </summary>
        public string? TargetType => Kind == AssertionStepKind.ReferenceTarget ? Name : null;

        /// <summary>
        /// Renders the step the way <see cref="PathStack"/> renders its navigation events, so paths built
        /// while rewriting a schema read like the definition paths reported on issues.
        /// </summary>
        public override string ToString() => Kind switch
        {
            AssertionStepKind.Child => $".{Name}",
            AssertionStepKind.Slice => $"[{Name}]",
            AssertionStepKind.Subschema => $"#{Name}",
            AssertionStepKind.ReferenceTarget => $"->{Name ?? "*"}",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Implemented by assertions that delegate validation to nested assertions, so a schema tree can be
    /// walked and rewritten without knowing the concrete container types.
    /// </summary>
    /// <remarks>Every <see cref="IAssertion"/> that keeps nested assertions as part of its state must
    /// implement this interface - <c>AssertionContainerTests</c> enforces that for this assembly. Implement
    /// it explicitly: it is an implementation detail of schema rewriting, not part of the assertion's
    /// public surface.</remarks>
    internal interface IAssertionContainer
    {
        /// <summary>
        /// Returns a copy of this container in which every nested assertion is replaced by the result of
        /// <paramref name="rewrite"/>, or this container itself when nothing changed.
        /// </summary>
        /// <remarks>Implementations must:
        /// <list type="bullet">
        /// <item>invoke <paramref name="rewrite"/> exactly once for each nested assertion, eagerly and in
        /// document order;</item>
        /// <item>return <c>this</c> when every call returned the very assertion it was given, so an
        /// unchanged subtree keeps its identity (which keeps rewriting cheap and cache-friendly);</item>
        /// <item>preserve all of their other state in the copy.</item>
        /// </list>
        /// Nested assertions that are structural machinery rather than validation members are not visited -
        /// see <see cref="SliceValidator"/>, whose discriminators are not rewritable. A rewrite must return
        /// an assertion that fits the slot it was called for: only
        /// <see cref="AssertionStepKind.Subschema"/> slots are narrower than <see cref="IAssertion"/>, they
        /// require an <see cref="ElementSchema"/>.</remarks>
        IAssertion WithChildren(Func<AssertionStep, IAssertion, IAssertion> rewrite);
    }

    internal static class AssertionContainerExtensions
    {
        /// <summary>
        /// Enumerates the nested assertions of a container, with the step leading to each of them.
        /// </summary>
        /// <remarks>Implemented on top of <see cref="IAssertionContainer.WithChildren"/> - which is required
        /// to visit every nested assertion eagerly - so a container cannot have a set of children that
        /// disagrees with what it rewrites.</remarks>
        public static IReadOnlyList<(AssertionStep Step, IAssertion Child)> Children(this IAssertionContainer container)
        {
            var children = new List<(AssertionStep, IAssertion)>();

            container.WithChildren((step, child) =>
            {
                children.Add((step, child));
                return child;
            });

            return children;
        }

        /// <summary>
        /// Applies <paramref name="rewrite"/> to each member, returning <c>null</c> when every member was
        /// returned unchanged, so the caller can hand out itself instead of a copy.
        /// </summary>
        public static IAssertion[]? TryRewriteMembers(
            this IReadOnlyCollection<IAssertion> members,
            AssertionStep step,
            Func<AssertionStep, IAssertion, IAssertion> rewrite)
        {
            IAssertion[]? updated = null;
            var index = 0;

            foreach (var member in members)
            {
                var rewritten = rewrite(step, member);

                if (!ReferenceEquals(rewritten, member))
                {
                    // Only start copying once we actually have a change to record.
                    updated ??= [.. members];
                    updated[index] = rewritten;
                }

                index += 1;
            }

            return updated;
        }
    }
}
