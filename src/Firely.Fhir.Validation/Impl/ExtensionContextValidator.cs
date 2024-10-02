using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.FhirPath;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation;

/// <summary>
/// An assertion which validates the context in which the extension is used against the expected context.
/// </summary>
[DataContract]
[EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
[System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
public class ExtensionContextValidator : IValidatable
{
    /// <summary>
    /// Creates a new ExtensionContextValidator with the given allowed contexts and invariants.
    /// </summary>
    /// <param name="contexts"></param>
    /// <param name="invariants"></param>
    public ExtensionContextValidator(IEnumerable<TypedContext> contexts, IEnumerable<string> invariants)
    {
        Contexts = contexts.ToList();
        Invariants = invariants.ToList();
    }

    internal List<TypedContext> Contexts { get; }

    internal List<string> Invariants { get; }

    /// <summary>
    /// Validate input against the expected context and invariants.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="vc"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state)
    {
        if (Contexts.Any(c => c.Type == null))
        {
            return new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INCORRECT,
                    "Extension context type was not set, but a context was defined. Skipping non-invariant context validation")
                .AsResult(state);
        }

        if (Contexts.Count > 0 && !Contexts.Any(context => validateContext(input, context, vc, state)))
        {
            return new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE,
                    $"Extension used outside of appropriate contexts. Expected context to be one of: {RenderExpectedContexts}")
                .AsResult(state);
        }

        var failedInvariantResults = Invariants
            .Select(inv => runContextInvariant(input, inv, vc, state))
            .Where(res => !res.Success)
            .ToList();

        if (failedInvariantResults.Count != 0)
        {
            return ResultReport.Combine(
                failedInvariantResults
                    .Where(fr => fr.Report is not null)
                    .Select(fr => fr.Report!)
                    .Concat(
                        failedInvariantResults
                            .Where(fr => fr.Report is null && fr.Success == false)
                            .Select<InvariantValidator.InvariantResult, ResultReport>(fr =>
                                new IssueAssertion(
                                    Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT,
                                    $"Extension context failed invariant constraint {fr.Invariant}"
                                ).AsResult(state))
                    ).ToList()
            );
        }

        return ResultReport.SUCCESS;
    }

    private static bool validateContext(IScopedNode input, TypedContext context, ValidationSettings settings, ValidationState state)
    {
        var contextNode = input.ToScopedNode().Parent ??
                          throw new InvalidOperationException("No context found while validating the context of an extension. Is your scoped node correct?");
        return context.Type switch
        {
            ContextType.DATATYPE => contextNode.InstanceType == context.Expression,
            ContextType.EXTENSION => contextNode.InstanceType == "Extension" && (contextNode.Parent?.Children("url").SingleOrDefault()?.Value as string ?? "None") == context.Expression,
            ContextType.FHIRPATH => contextNode.ResourceContext.IsTrue(context.Expression),
            ContextType.ELEMENT => validateElementContext(context.Expression, state),
            ContextType.RESOURCE => context.Expression == "*" || validateElementContext(context.Expression, state),
            _ => throw new System.InvalidOperationException($"Unknown context type {context.Expression}")
        };
    }

    private static bool validateElementContext(string contextExpression, ValidationState state)
    {
        var defPath = state.Location.DefinitionPath;
        var needsElementAdded = defPath.Current?.Previous is not InvokeProfileEvent;
        
        // if we have a slicing identifier, this is a bit tougher, since the slicing identifier actually defines the element itself, not its context. consider:
        // context without slicing: Address
        // context with slicing: Address.extension:simpleExtension
        var exprHasSlicingIdentifier = contextExpression.Contains(':');

        return defPath.MatchesContext(contextExpression);
    }

    private static InvariantValidator.InvariantResult runContextInvariant(IScopedNode input, string invariant, ValidationSettings vc, ValidationState state)
    {
        var fhirPathValidator = new FhirPathValidator("ctx-inv", invariant.Replace("%extension", "%%extension"));
        return fhirPathValidator.RunInvariant(input.ToScopedNode().Parent!, vc, state, ("extension", [input.ToScopedNode()]));
    }

    private string RenderExpectedContexts => string.Join(", ", Contexts.Select(c => $"{{{c.Type},{c.Expression}}}"));

    private static string Key => "context";

    private object Value =>
        new JObject(
            new JProperty("context", new JArray(Contexts.Select(c => new JObject(
                new JProperty("type", c.Expression),
                new JProperty("expression", c.Expression)
            )))),
            new JProperty("invariants", new JArray(Invariants))
        );

    /// <inheritdoc />
    public JToken ToJson() => new JProperty(Key, Value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Type"></param>
    /// <param name="Expression"></param>
#pragma warning disable RS0016
    public record TypedContext(ContextType? Type, string Expression); // PR TODO: ADD THESE TO PUBLIC API
#pragma warning restore RS0016

    /// <summary>
    /// The context in which the extension should be used.
    /// </summary>
    public enum ContextType
    {
        /// <summary>
        /// The context is all elements matching a particular resource element path.
        /// </summary>
        RESOURCE, // STU3

        /// <summary>
        /// The context is all nodes matching a particular data type element path (root or repeating element)
        /// or all elements referencing aparticular primitive data type (expressed as the datatype name).
        /// </summary>
        DATATYPE, // STU3

        /// <summary>
        /// The context is a particular extension from a particular profile, a uri that identifies the extension definition.
        /// </summary>
        EXTENSION, // STU3+

        /// <summary>
        /// The context is all elements that match the FHIRPath query found in the expression.
        /// </summary>
        FHIRPATH, // R4+

        /// <summary>
        /// The context is any element that has an ElementDefinition.id that matches that found in the expression.
        /// This includes ElementDefinition Ids that have slicing identifiers.
        /// The full path for the element is [url]#[elementid]. If there is no #, the Element id is one defined in the base specification.
        /// </summary>
        ELEMENT, // R4+
    }
}