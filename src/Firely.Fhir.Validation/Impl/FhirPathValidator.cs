﻿/* 
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
using System.Threading.Tasks;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An assertion expressed using FhirPath.
    /// </summary>
    [DataContract]
    public class FhirPathValidator : BasicValidator
    {
        private readonly string _key;

#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public override string Key => _key;

        [DataMember(Order = 1)]
        public string Expression { get; private set; }
     
        [DataMember(Order = 2)]
        public string? HumanDescription { get; private set; }

        [DataMember(Order = 3)]
        public IssueSeverity? Severity { get; private set; }

        [DataMember(Order = 4)]
        public bool BestPractice { get; private set; }
#else
        [DataMember]
        public override string Key => _key;

        [DataMember]
        public string Expression { get; private set; }


        [DataMember]
        public string? HumanDescription { get; private set; }

        [DataMember]
        public IssueSeverity? Severity { get; private set; }

        [DataMember]
        public bool BestPractice { get; private set; }
#endif

        private readonly Lazy<CompiledExpression> _defaultCompiledExpression;

        public override object Value => Expression;

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


        public FhirPathValidator(string key, string expression, string? humanDescription, IssueSeverity? severity = IssueSeverity.Error,
            bool bestPractice = false, bool precompile = true)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            HumanDescription = humanDescription;
            Severity = severity ?? throw new ArgumentNullException(nameof(severity));
            BestPractice = bestPractice;

            _defaultCompiledExpression = precompile ?
                (new(getDefaultCompiledExpression(Expression)))
                : (new(() => getDefaultCompiledExpression(Expression)));
        }

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

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState _)
        {
            var result = Assertions.EMPTY;

            var node = input as ScopedNode ?? new ScopedNode(input);
            var context = node.ResourceContext;

            if (BestPractice)
            {
                switch (vc.ConstraintBestPractices)
                {
                    case ValidateBestPractices.Ignore:
                        return Task.FromResult(Assertions.SUCCESS);
                    case ValidateBestPractices.Enabled:
                        Severity = IssueSeverity.Error;
                        break;
                    case ValidateBestPractices.Disabled:
                        Severity = IssueSeverity.Warning;
                        break;
                    default:
                        break;
                }
            }

            bool success = false;
            try
            {
                success = predicate(input, new EvaluationContext(context), vc);
            }
            catch (Exception e)
            {
                result += new TraceAssertion($"Evaluation of FhirPath for constraint '{Key}' failed: {e.Message}");
            }

            if (!success)
            {
                result += ResultAssertion.CreateFailure(
                    new IssueAssertion(Severity == IssueSeverity.Error ?
                        Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT :
                        Issue.CONTENT_ELEMENT_FAILS_WARNING_CONSTRAINT,
                        input.Location, $"Instance failed constraint {getDescription()}"));
                return Task.FromResult(result);
            }

            return Task.FromResult(Assertions.SUCCESS);
        }

        private string getDescription()
        {
            var desc = Key;

            if (!string.IsNullOrEmpty(HumanDescription))
                desc += " \"" + HumanDescription + "\"";

            return desc;
        }

        private static readonly SymbolTable FHIRFPSYMBOLS;

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
            var compiledExpression = (vc?.FhirPathCompiler == null)
                ? _defaultCompiledExpression.Value : vc?.FhirPathCompiler.Compile(Expression);

            return compiledExpression.Predicate(input, context);
        }
    }
}