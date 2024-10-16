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
        
        if (Contexts.Any(c => c.Type == null))
        {
            throw new IncorrectElementDefinitionException("Extension context type was not set, but a context was defined.");
        }
        
        Invariants = invariants.ToList();
    }

    [DataMember] internal IReadOnlyCollection<TypedContext> Contexts { get; }

    [DataMember] internal IReadOnlyCollection<string> Invariants { get; }

    /// <summary>
    /// Validate input against the expected context and invariants.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="vc"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state)
    { 
        if (Contexts.Count > 0 && !Contexts.Any(context => validateContext(input, context, state)))
        {
            return new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE,
                    $"Extension used outside of appropriate contexts. Expected context to be one of: {RenderExpectedContexts}")
                .AsResult(state);
        }

        var invariantResults = Invariants
            .Select(inv => runContextInvariant(input, inv, vc, state))
            .ToList();

        // fast path for if all invariants are successful
        if (invariantResults.All(r => r.Success))
            return ResultReport.SUCCESS;
        
        return ResultReport.Combine(
            invariantResults.Select<InvariantValidator.InvariantResult, ResultReport>(res =>
                (res.Success, res.Report) switch
                {
                    // If eval to false, throw an error
                    (false, null) =>
                        new IssueAssertion(
                            Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT,
                            $"Extension context failed invariant constraint {res.Invariant}").AsResult(state),
                    // If evalutation threw an exception, return that exception
                    (_, { } report) => report,
                    // Otherwise return success
                    _ => ResultReport.SUCCESS
                }
            ).ToList()
        );
    }

    private static bool validateContext(IScopedNode input, TypedContext context, ValidationState state)
    {
        var contextNode = input.ToScopedNode().Parent ??
                          throw new InvalidOperationException("No context found while validating the context of an extension.");
        return context.Type switch
        {
            ContextType.DATATYPE => contextNode.InstanceType == context.Expression,
            ContextType.EXTENSION => contextNode.Parent?.InstanceType == "Extension" && (contextNode.Parent?.Children("url").SingleOrDefault()?.Value as string) == context.Expression,
            ContextType.FHIRPATH => contextNode.ResourceContext.IsTrue(context.Expression),
            ContextType.ELEMENT => validateElementContext(context.Expression, state),
            ContextType.RESOURCE => context.Expression == "*" || validateElementContext(context.Expression, state),
            _ => throw new InvalidOperationException($"Unknown context type {context.Expression}")
        };
    }

    private static bool validateElementContext(string contextExpression, ValidationState state)
    {
        var defPath = state.Location.DefinitionPath;

        return defPath.MatchesContext(contextExpression);
    }

    private static InvariantValidator.InvariantResult runContextInvariant(IScopedNode input, string invariant, ValidationSettings vc, ValidationState state)
    {
        // our invariant is defined with %extension, but the FhirPathValidator expects %%extension because that is our syntax for environment variables
        // TODO investigate changing this in the SDK
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
    public record TypedContext(ContextType? Type, string Expression);

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