using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.FhirPath;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
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
    public ResultReport Validate(PocoNode input, ValidationSettings vc, ValidationState state)
    {
        if (Contexts.Count > 0 && !Contexts.Any(context => validateContext(input, context, vc)))
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

    private bool validateContext(PocoNode input, TypedContext context, ValidationSettings vc)
    {
        var contextNode = input.Parent ??
                          throw new InvalidOperationException("No context found while validating the context of an extension.");
        return context.Type switch
        {
            ContextType.DATATYPE => context.Expression == "Any" || validateElementContext(context.Expression, contextNode),
            ContextType.EXTENSION => validateExtensionContext(context.Expression, contextNode),
            ContextType.FHIRPATH => validateFhirPathContext(context.Expression, contextNode, vc),
            ContextType.ELEMENT => validateElementContext(context.Expression, contextNode),
            ContextType.RESOURCE => context.Expression == "*" || validateElementContext(context.Expression, contextNode),
            _ => throw new InvalidOperationException($"Unknown context type {context.Expression}")
        };
    }

    private static bool validateElementContext(string contextExpression, PocoNode instance)
    {
        if (contextExpression == "Element") return true;

        // Element ids can be profile-qualified ([url]#[elementid]). We cannot verify conformance against
        // the profile here, so we match on the element id part only
        var hash = contextExpression.IndexOf('#');
        if (hash >= 0 && contextExpression.IndexOf('/') is var slash and >= 0 && slash < hash)
            contextExpression = contextExpression[(hash + 1)..];

        // Element ids can contain slicing identifiers (e.g. Observation.category:vscat.coding). We cannot
        // determine slice membership here, so we match on the path with slice names removed
        var expressionSegments = contextExpression.Split('.').Select(s => s.Split(':')[0]).ToArray();

        var root = instance;
        while (root.Parent is not null) root = root.Parent;
#pragma warning disable CS0618 // Type or member is obsolete
        var modelInspector = ModelInspector.ForType(root.Poco.GetType());
#pragma warning restore CS0618 // Type or member is obsolete

        // a single-segment expression may name a (possibly abstract) base type of the element itself,
        // e.g. "BackboneElement" should match an element of type "Patient.contact"
        if (expressionSegments.Length == 1 && modelInspector.IsInstanceTypeFor(expressionSegments[0], instance.Poco.TypeName))
            return true;

        return logicalPaths(instance).Any(path => pathMatchesExpression(path, expressionSegments, modelInspector));
    }

    /// <summary>
    /// The set of definition paths this element is reachable by, mirroring the "logical paths" of the
    /// reference validator. These are the element's path within its resource, plus paths rooted in each
    /// ancestor type. Since the <see cref="Base.TypeName"/> of a backbone component is the definition path of
    /// its first occurrence (the contentReference target), recursive and aliased elements
    /// (e.g. Questionnaire.item.item) contract to the path their constraints are defined on.
    /// </summary>
    private static IReadOnlyCollection<string> logicalPaths(PocoNode node)
    {
        // resources (re)set the path root, so contained and bundled resources match resource-rooted contexts
        if (node.Parent is null || node.Poco is Resource)
            return [node.Poco.TypeName];

        var result = new HashSet<string>();

        foreach (var parentPath in logicalPaths(node.Parent))
            foreach (var name in nameVariants(node))
                result.Add(parentPath + "." + name);

        result.Add(node.Poco.TypeName);
        return result;
    }

    /// <summary>
    /// The names under which an element can be referenced in an element id: choice elements can be referenced
    /// both by their definition name ("value[x]") and their suffixed instance name ("valueBoolean").
    /// </summary>
    private static IEnumerable<string> nameVariants(PocoNode node)
    {
        yield return node.Name;

        if (node.Parent is not { } parent || node.Poco is not DataType dt) yield break;

#pragma warning disable CS0618 // Type or member is obsolete
        var parentMapping = ModelInspector.ForType(parent.Poco.GetType()).FindClassMapping(parent.Poco.GetType());
#pragma warning restore CS0618 // Type or member is obsolete

        if (parentMapping?.FindMappedElementByName(node.Name) is { Choice: ChoiceType.DatatypeChoice })
        {
            yield return node.Name + dt.TypeName.Capitalize();
            yield return node.Name + "[x]";
        }
    }

    private static bool pathMatchesExpression(string path, string[] expressionSegments, ModelInspector modelInspector)
    {
        var pathSegments = path.Split('.');
        if (pathSegments.Length != expressionSegments.Length) return false;

        for (var i = 1; i < pathSegments.Length; i++)
        {
            if (pathSegments[i] != expressionSegments[i]) return false;
        }

        if (pathSegments[0] == expressionSegments[0]) return true;

        // the root of the expression may be a base type of the root of the path (e.g. "Resource.active"
        // should match "Patient.active")
        return modelInspector.IsInstanceTypeFor(expressionSegments[0], pathSegments[0]);
    }

    private static bool validateExtensionContext(string contextExpression, PocoNode contextNode)
    {
        // Spec: "Another extension. The canonical URL of the extension, optionally followed by #code
        // for extensions that appear within a complex extension."
        // https://hl7.org/fhir/R4/defining-extensions.html#context
        //
        // contextNode is the element the validated extension is placed on. The host extension is:
        //   - contextNode itself, when the extension is a direct child of a complex extension
        //     (Extension.extension[*])
        //   - contextNode.Parent, when the extension is placed on the value[x] of a complex extension
        //     (Extension.value[x].extension[*]) — not explicit in the spec but consistent with how
        //     extensions on data type elements work; the logical host is still the enclosing extension
        var hostExtension = grabInContextExtensionNode(contextNode);

        // If neither branch matched, the extension is not inside any extension at all — never valid.
        if (hostExtension?.Poco is not Extension host) return false;

        // Simple case: the host extension's URL matches the context expression directly.
        if (host.Url == contextExpression) return true;

        // [canonical]#[code]: the extension appears within the sub-extension named [code] of the
        // complex extension identified by [canonical]. The host must match [code] and its own enclosing
        // extension must match [canonical]. The same value[x] indirection is applied when resolving the
        // enclosing extension.
        var hash = contextExpression.LastIndexOf('#');
        if (hash > 0 && host.Url == contextExpression[(hash + 1)..])
        {
            var enclosing = grabInContextExtensionNode(hostExtension.Parent);

            return enclosing?.Poco is Extension enclosingExtension && enclosingExtension.Url == contextExpression[..hash];
        }

        return false;
    }
    
    private static PocoNode? grabInContextExtensionNode(PocoNode? node) => node switch
    {
        { Poco: Extension } => node,
        { Parent.Poco: Extension } => node.Parent,
        _ => null
    };

    private FhirPathCompiler? _lastUsedCompiler;
    private ConcurrentDictionary<string, CompiledExpression> _compiledContextExpressionsCache = new();

    private bool validateFhirPathContext(string contextExpression, PocoNode contextNode, ValidationSettings vc)
    {
        var resource = contextNode.Poco is Resource ? contextNode : contextNode.GetParentResource();
        if (resource is null)
        {
            // detached (non-resource) root: fall back to evaluating from the outermost node
            resource = contextNode;
            while (resource.Parent is not null) resource = resource.Parent;
        }

        var compiler = vc.FhirPathCompiler ?? FhirPathValidator.DefaultCompiler;
        if (!ReferenceEquals(compiler, _lastUsedCompiler))
        {
            _compiledContextExpressionsCache = new();
            _lastUsedCompiler = compiler;
        }

        var compiledExpression = _compiledContextExpressionsCache.GetOrAdd(contextExpression, compiler.Compile);
        var evalContext = new FhirEvaluationContext { Environment = new Dictionary<string, IEnumerable<PocoNode>> { ["resource"] = [resource] } };
        var selected = compiledExpression(resource, evalContext).ToList();

        // The expression selects the set of elements on which the extension can appear.
        if (selected.Any(node => ReferenceEquals(node.Poco, contextNode.Poco))) return true;

        // Some published extensions phrase their context as a predicate over the resource instead;
        // accept those when they evaluate to true.
        return selected is [{ } single] && single.GetValue() is true;
    }

    private static InvariantValidator.InvariantResult runContextInvariant(PocoNode input, string invariant, ValidationSettings vc, ValidationState state)
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
                new JProperty("type", c.Type?.ToString()),
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