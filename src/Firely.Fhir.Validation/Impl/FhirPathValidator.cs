/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.Fhir.Validation;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion expressed using FhirPath.
    /// </summary>
    [DataContract]
    public class FhirPathValidator : InvariantValidator
    {
        /// <inheritdoc />
        [DataMember]
        public override string Key => _key;

        /// <summary>
        /// The FhirPath statement containing the invariant to validate.
        /// </summary>
        [DataMember]
        public string Expression { get; private set; }

        /// <inheritdoc />
        [DataMember]
        public override string? HumanDescription => _humanDescription;

        /// <inheritdoc />
        [DataMember]
        public override IssueSeverity? Severity => _severity;

        /// <inheritdoc />
        [DataMember]
        public override bool BestPractice => _bestPractice;

        private static readonly SymbolTable FHIRFPSYMBOLS;
        private readonly string _key;
        private readonly string? _humanDescription;
        private readonly IssueSeverity? _severity;
        private readonly bool _bestPractice;
        private readonly Lazy<CompiledExpression> _defaultCompiledExpression;

        /// <summary>
        /// Initializes a FhirPathValidator instance with the given FhirPath expression and identifying key.
        /// </summary>
        public FhirPathValidator(string key, string expression) : this(key, expression, null, severity: IssueSeverity.Error) { }

        // Constructor for exclusive use by the deserializer: this constructor will not compile the FP constraint, but delay
        // compilation to the first use. The deserializer prefer to use this constructor overthe public one, as the public
        // constructor has an extra argument (even though it's optional) that is not reflected in the public properties of this class.
#pragma warning disable IDE0051 // Suppressed: used by the deserializer using reflection
        private FhirPathValidator(string key, string expression, string? humanDescription, IssueSeverity? severity, bool bestPractice)
            : this(key, expression, humanDescription, severity, bestPractice, precompile: false)
#pragma warning restore IDE0051 // Remove unused private members           
        {
            // nothing
        }

        /// <summary>
        /// Initializes a FhirPathValidator instance with the given FhirPath expression, identifying key and other
        /// properties.
        /// </summary>
        public FhirPathValidator(string key, string expression, string? humanDescription, IssueSeverity? severity = IssueSeverity.Error,
            bool bestPractice = false, bool precompile = true)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _humanDescription = humanDescription;
            _severity = severity ?? throw new ArgumentNullException(nameof(severity));
            _bestPractice = bestPractice;

            _defaultCompiledExpression = precompile ?
                (new(getDefaultCompiledExpression(Expression)))
                : (new(() => getDefaultCompiledExpression(Expression)));
        }

        /// <inheritdoc />
        public override JToken ToJson()
        {
            var props = new JObject(
                     new JProperty("key", Key),
                     new JProperty("expression", Expression),
                     new JProperty("severity", Severity?.GetLiteral()),
                     new JProperty("bestPractice", BestPractice)
                    );
            if (HumanDescription != null)
                props.Add(new JProperty("humanDescription", HumanDescription));
            return new JProperty($"fhirPath-{Key}", props);
        }

        /// <inheritdoc/>
        protected override (bool, ResultAssertion?) RunInvariant(ITypedElement input, ValidationContext vc)
        {
            try
            {
                var node = input as ScopedNode ?? new ScopedNode(input);
                return (predicate(input, new EvaluationContext(node.ResourceContext), vc), null);
            }
            catch (Exception e)
            {
                return (false, ResultAssertion.FromEvidence(new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INVALID_FHIRPATH_EXPRESSION,
                    input.Location, $"Evaluation of FhirPath for constraint '{Key}' failed: {e.Message}")));
            }
        }


        static FhirPathValidator()
        {
            FHIRFPSYMBOLS = new SymbolTable();
            FHIRFPSYMBOLS.AddStandardFP();
            FHIRFPSYMBOLS.AddFhirExtensions();

            // Until this method is included in the 3.x release of the SDK
            // we need to add it ourselves.
            FHIRFPSYMBOLS.Add("conformsTo", (Func<object, string, bool>)conformsTo, doNullProp: false);

            static bool conformsTo(object focus, string valueset) => throw new NotImplementedException("The conformsTo() function is not supported in the .NET FhirPath engine.");
        }

        private static CompiledExpression getDefaultCompiledExpression(string expression)
        {
            try
            {
                var compiler = new FhirPathCompiler(FHIRFPSYMBOLS);
                return compiler.Compile(expression);
            }
            catch (Exception ex)
            {
                throw new IncorrectElementDefinitionException($"Error during compilation expression ({expression})", ex);
            }
        }

        private bool predicate(ITypedElement input, EvaluationContext context, ValidationContext vc)
        {
            //TODO: this will compile the statement every time if an external fhirpath compiler is set!!
            var compiledExpression = (vc?.FhirPathCompiler == null)
                ? _defaultCompiledExpression.Value : vc?.FhirPathCompiler.Compile(Expression);

            return compiledExpression.Predicate(input, context);
        }
    }
}