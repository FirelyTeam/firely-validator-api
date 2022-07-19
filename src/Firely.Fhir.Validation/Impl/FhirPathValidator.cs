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

        private static readonly SymbolTable DefaultFpSymbolTable;
        private readonly string _key;
        private readonly string? _humanDescription;
        private readonly IssueSeverity? _severity;
        private readonly bool _bestPractice;

        // Used for caching the Expression for this validator. Is *not* readonly, since we need
        // to cache this expression for different compilers (since the compiler is a setting
        // that is provided at runtime).
        private CompiledExpression? _compiledExpression;
        private FhirPathCompiler? _lastUsedCompiler;

        /// <summary>
        /// Initializes a FhirPathValidator instance with the given FhirPath expression and identifying key.
        /// </summary>
        public FhirPathValidator(string key, string expression) : this(key, expression, null, severity: IssueSeverity.Error) { }

        /// <summary>
        /// Initializes a FhirPathValidator instance with the given FhirPath expression, identifying key and other
        /// properties.
        /// </summary>
        public FhirPathValidator(string key, string expression, string? humanDescription, IssueSeverity? severity = IssueSeverity.Error,
            bool bestPractice = false)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _humanDescription = humanDescription;
            _severity = severity ?? throw new ArgumentNullException(nameof(severity));
            _bestPractice = bestPractice;
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
        protected override (bool, ResultReport?) RunInvariant(ITypedElement input, ValidationContext vc)
        {
            try
            {
                var node = input as ScopedNode ?? new ScopedNode(input);
                return (predicate(input, new EvaluationContext(node.ResourceContext), vc), null);
            }
            catch (Exception e)
            {
                return (false, new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INVALID_FHIRPATH_EXPRESSION,
                    input.Location, $"Evaluation of FhirPath for constraint '{Key}' failed: {e.Message}").AsResult());
            }
        }

        /// <summary>
        /// The compiler used to process the <see cref="FhirPathValidator.Expression"/> and validate the data,
        /// unless overridden by <see cref="ValidationContext.FhirPathCompiler"/>.
        /// </summary>
        public static FhirPathCompiler DefaultCompiler { get; private set; }

        static FhirPathValidator()
        {
            DefaultFpSymbolTable = new SymbolTable();
            DefaultFpSymbolTable.AddStandardFP();
            DefaultFpSymbolTable.AddFhirExtensions();

            // TODO: Until this method is included in the 3.x release of the SDK
            // we need to add it ourselves.
            DefaultFpSymbolTable.Add("conformsTo", (Func<object, string, bool>)conformsTo, doNullProp: false);

            static bool conformsTo(object focus, string valueset) => throw new NotImplementedException("The conformsTo() function is not supported in the .NET FhirPath engine.");

            DefaultCompiler = new FhirPathCompiler(DefaultFpSymbolTable);
        }

        private CompiledExpression getDefaultCompiledExpression(FhirPathCompiler compiler)
        {
            if (compiler == _lastUsedCompiler && _compiledExpression is not null) return _compiledExpression;

            try
            {
                _lastUsedCompiler = compiler;
                return _compiledExpression = compiler.Compile(Expression);
            }
            catch (Exception ex)
            {
                throw new IncorrectElementDefinitionException($"Error during compilation expression ({Expression})", ex);
            }
        }

        private bool predicate(ITypedElement input, EvaluationContext context, ValidationContext vc)
        {
            //TODO: this will compile the statement every time if an external fhirpath compiler is set!!
            var compiler = vc?.FhirPathCompiler ?? DefaultCompiler;
            var compiledExpression = getDefaultCompiledExpression(compiler);

            return compiledExpression.Predicate(input, context);
        }
    }
}