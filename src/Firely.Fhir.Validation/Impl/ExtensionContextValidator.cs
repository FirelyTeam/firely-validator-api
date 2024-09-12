using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Support;
using Hl7.FhirPath;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
public class ExtensionContextValidator : BasicValidator
{
    /// <summary>
    /// Creates a new ExtensionContextValidator with the given allowed contexts and invariants.
    /// </summary>
    /// <param name="contexts"></param>
    /// <param name="invariants"></param>
    public ExtensionContextValidator(IEnumerable<(ContextType?, string)> contexts, IEnumerable<string> invariants)
    {
        Contexts = contexts.ToList();
        Invariants = invariants.ToList();
    }

    internal List<(ContextType?, string)> Contexts { get; }

    internal List<string> Invariants { get; }

    internal override ResultReport BasicValidate(IScopedNode input, ValidationSettings vc, ValidationState state)
    {
        if (Contexts.Any(c => c.Item1 == null))
        {
            return new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INCORRECT,
                    "Extension context type was not set, but a context was defined. Skipping non-invariant context validation")
                .AsResult(state);
        }

        if (Contexts.TakeWhile(context => !validateContext(input, context, state)).Any())
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

    private bool validateContext(IScopedNode input, (ContextType?, string) context, ValidationState state)
    {
        var contextNode = input.ToScopedNode(); // TODO add parent once we update sdk to 5.10
        return context.Item1 switch
        {
            ContextType.RESOURCE => contextNode.Location.EndsWith(context.Item2),
            ContextType.DATATYPE => contextNode.InstanceType == context.Item2,
            ContextType.EXTENSION => contextNode.InstanceUri == context.Item2, // TODO this is wrong
            ContextType.FHIRPATH => contextNode.IsTrue(context.Item2),
            ContextType.ELEMENT => contextNode.Definition?.ElementName == context.Item2,
            _ => throw new System.InvalidOperationException($"Unknown context type {context.Item1}")
        };
    }

    private static InvariantValidator.InvariantResult runContextInvariant(IScopedNode input, string invariant, ValidationSettings vc, ValidationState state)
    {
        var fhirPathValidator = new FhirPathValidator("ctx-inv", invariant);
        return fhirPathValidator.RunInvariant(input, vc, state);
    }

    private string RenderExpectedContexts => string.Join(", ", Contexts.Select(c => $"{{{c.Item1},{c.Item2}}}"));

    private string RenderExpectedInvariants => string.Join(" || ", Invariants);

    /// <inheritdoc/>
    protected override string Key => "context";

    /// <inheritdoc/>
    protected override object Value => new JObject();

    /// <inheritdoc />
    public override JToken ToJson() => throw new System.NotImplementedException();

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