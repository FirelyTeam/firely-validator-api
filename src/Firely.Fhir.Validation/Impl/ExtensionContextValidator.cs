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

    /// <summary>
    /// The expected contexts in which the extension should be used.
    /// </summary>
    [DataMember]
    public IReadOnlyList<TypedContext> Contexts { get; }

    /// <summary>
    /// The invariants that must be satisfied for this context to be valid.
    /// </summary>
    [DataMember]
    public IReadOnlyList<string> Invariants { get; }

    /// <summary>
    /// Validate input against the expected context and invariants.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="vc"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public ResultReport Validate(ITypedElement input, ValidationSettings vc, ValidationState state)
    {
        if (Contexts.Count > 0 && !Contexts.Any(context => validateContext(input, context, state)))
        {
            return new IssueAssertion(Issue.CONTENT_INCORRECT_OCCURRENCE,
                    $"Extension used outside of appropriate contexts. Expected context to be one of: {RenderExpectedContexts}")
                .AsResult(state, input, nameof(ExtensionContextValidator));
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
                            $"Extension context failed invariant constraint {res.Invariant}").AsResult(state, input, nameof(ExtensionContextValidator)),
                    // If evalutation threw an exception, return that exception
                    (_, { } report) => report,
                    // Otherwise return success
                    _ => ResultReport.SUCCESS
                }
            ).ToList()
        );
    }

    private static bool validateContext(ITypedElement input, TypedContext context, ValidationState state)
    {
        var contextNode = input.ToPocoNode().Parent ??
                          throw new InvalidOperationException("No context found while validating the context of an extension.");
        return context.Type switch
        {
            ContextType.DATATYPE => ((ITypedElement)contextNode).InstanceType == context.Expression,
            ContextType.EXTENSION => (contextNode.Parent as ITypedElement)?.InstanceType == "Extension" && (contextNode.Parent?.Child("url")?.SingleOrDefault()?.GetValue() as string) == context.Expression,
            ContextType.FHIRPATH => contextNode.IsTrue("%resource." + context.Expression),
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

    private static InvariantValidator.InvariantResult runContextInvariant(ITypedElement input, string invariant, ValidationSettings vc, ValidationState state)
    {
        // our invariant is defined with %extension, but the FhirPathValidator expects %%extension because that is our syntax for environment variables
        // TODO investigate changing this in the SDK
        var fhirPathValidator = new FhirPathValidator("ctx-inv", invariant.Replace("%extension", "%%extension"));
        return fhirPathValidator.RunInvariant(input.ToPocoNode().Parent!, vc, state, ("extension", [input.ToPocoNode()]));
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
    /// The expected context in which the extension should be used.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="expression"></param>
    [DataContract]
    public class TypedContext(ContextType? type, string expression)
    {
        /// <summary>
        /// Specific type an extension should be used in.
        /// </summary>
        [DataMember]
        public ContextType? Type { get; } = type;
        /// <summary>
        /// Specific expression the extension should adhere to.
        /// </summary>
        [DataMember]
        public string Expression { get; } = expression;
    }

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