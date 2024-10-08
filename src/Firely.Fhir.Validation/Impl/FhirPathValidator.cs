/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion expressed using FhirPath.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
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
        internal override InvariantResult RunInvariant(IScopedNode input, ValidationSettings vc, ValidationState s) =>
            RunInvariant(input.ToScopedNode(), vc, s);
        
        internal InvariantResult RunInvariant(ScopedNode input, ValidationSettings vc, ValidationState s, params (string key, IEnumerable<ITypedElement> value)[] env) =>
            runInvariantInternal(input, vc, s, env);

        private InvariantResult runInvariantInternal(ScopedNode input, ValidationSettings vc, ValidationState s, params (string key, IEnumerable<ITypedElement> value)[] env)
        {
            try
            {
                var context = new FhirEvaluationContext
                {
                    TerminologyService = new ValidateCodeServiceToTerminologyServiceAdapter(vc.ValidateCodeService),
                    Environment = new Dictionary<string, IEnumerable<ITypedElement>>(env.Select(kvp => new KeyValuePair<string, IEnumerable<ITypedElement>>(kvp.key, kvp.value)))
                };
                
                var success = predicate(input, context, vc);
                return new(success, null);
            }
            catch (Exception e)
            {
                return new(false, new IssueAssertion(Issue.PROFILE_ELEMENTDEF_INVALID_FHIRPATH_EXPRESSION,
                        $"Evaluation of FhirPath for constraint '{Key}' failed: {e.Message}")
                    .AsResult(s));
            }
        }

        /// <summary>
        /// The compiler used to process the <see cref="FhirPathValidator.Expression"/> and validate the data,
        /// unless overridden by <see cref="ValidationSettings.FhirPathCompiler"/>.
        /// </summary>
        public static FhirPathCompiler DefaultCompiler { get; private set; }

        static FhirPathValidator()
        {
            SymbolTable defaultFpSymbolTable = new();
            defaultFpSymbolTable.AddStandardFP();
            defaultFpSymbolTable.AddFhirExtensions();

            // Until this method is included in a future release of the SDK
            // we need to add it ourselves.
            defaultFpSymbolTable.Add("conformsTo", (Func<object, string, bool>)conformsTo, doNullProp: false);

            static bool conformsTo(object focus, string valueset) => throw new NotImplementedException("The conformsTo() function is not supported in the .NET FhirPath engine.");

            DefaultCompiler = new FhirPathCompiler(defaultFpSymbolTable);
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

        private bool predicate(ScopedNode input, EvaluationContext context, ValidationSettings vc)
        {
            var compiler = vc?.FhirPathCompiler ?? DefaultCompiler;
            var compiledExpression = getDefaultCompiledExpression(compiler);

            return compiledExpression.IsTrue(input, context);
        }

        /// <summary>
        /// An adapter between the <see cref="ICodeValidationTerminologyService"/> and <see cref="ITerminologyService"/>. Be careful to use
        /// this adapter, because it does only implement the <see cref="ICodeValidationTerminologyService"/> methods. The other methods, like those
        /// from <see cref="IMappingTerminologyService"/> are not implemented will raise an exception.
        /// </summary>
        private class ValidateCodeServiceToTerminologyServiceAdapter : ITerminologyService
        {
            private readonly ICodeValidationTerminologyService _service;

            public ValidateCodeServiceToTerminologyServiceAdapter(ICodeValidationTerminologyService service)
            {
                _service = service;
            }

            public Task<Resource> Closure(Parameters parameters, bool useGet = false) => throw new NotImplementedException();
            public Task<Parameters> CodeSystemValidateCode(Parameters parameters, string? id = null, bool useGet = false) => throw new NotImplementedException();
            public Task<Resource> Expand(Parameters parameters, string? id = null, bool useGet = false) => throw new NotImplementedException();
            public Task<Parameters> Lookup(Parameters parameters, bool useGet = false) => throw new NotImplementedException();
            public Task<Parameters> Subsumes(Parameters parameters, string? id = null, bool useGet = false) => _service.Subsumes(parameters, id, useGet);
            public Task<Parameters> Translate(Parameters parameters, string? id = null, bool useGet = false) => throw new NotImplementedException();
            public Task<Parameters> ValueSetValidateCode(Parameters parameters, string? id = null, bool useGet = false) => _service.ValueSetValidateCode(parameters, id, useGet);
        }
    }
}