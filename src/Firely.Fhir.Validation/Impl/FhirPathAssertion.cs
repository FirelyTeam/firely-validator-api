﻿/* 
 * Copyright (c) 2020, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Utility;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    [DataContract]
    public class FhirPathAssertion : SimpleAssertion
    {
        private readonly string _key;

        [DataMember(Order = 0)]
        public override string Key => _key;

        [DataMember(Order = 1)]
        public string Expression { get; private set; }

        public override object Value => Expression;

        [DataMember(Order = 2)]
        public string? HumanDescription { get; private set; }

        [DataMember(Order = 3)]
        public IssueSeverity? Severity { get; private set; }

        [DataMember(Order = 4)]
        public bool BestPractice { get; private set; }

        private readonly CompiledExpression _defaultCompiledExpression;

        public FhirPathAssertion(string key, string expression) : this(key, expression, null) { }


        public FhirPathAssertion(string key, string expression, string? humanDescription, IssueSeverity? severity = IssueSeverity.Error, bool bestPractice = false)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            HumanDescription = humanDescription;
            Severity = severity ?? throw new ArgumentNullException(nameof(severity));
            BestPractice = bestPractice;

            _defaultCompiledExpression = getDefaultCompiledExpression(expression);
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

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
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
                result += new Trace($"Evaluation of FhirPath for constraint '{Key}' failed: {e.Message}");
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

        private static CompiledExpression getDefaultCompiledExpression(string expression)
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddStandardFP();
            symbolTable.AddFhirExtensions();

            try
            {
                var compiler = new FhirPathCompiler(symbolTable);
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
                ? _defaultCompiledExpression : vc?.FhirPathCompiler.Compile(Expression);

            return compiledExpression.Predicate(input, context);
        }

    }

    internal static class FPExtensions
    {
        public static SymbolTable AddFhirExtensions(this SymbolTable t)
        {
            t.Add("hasValue", (ITypedElement f) => HasValue(f), doNullProp: false);

            // Pre-normative this function was called htmlchecks, normative is htmlChecks
            // lets keep both to keep everyone happy.
            t.Add("htmlchecks", (ITypedElement f) => HtmlChecks(f), doNullProp: false);
            t.Add("htmlChecks", (ITypedElement f) => HtmlChecks(f), doNullProp: false);

            return t;
        }

        public static bool HasValue(ITypedElement focus)
        {
            return focus?.Value is not null;
        }

        /// <summary>
        /// Check if the node has a value, and not just extensions.
        /// </summary>
        /// <param name="focus"></param>
        /// <returns></returns>
        public static bool HtmlChecks(ITypedElement focus)
        {
            if (focus == null)
                return false;
            if (focus.Value == null)
                return false;
            // Perform the checking of the content for valid html content
            var html = focus.Value.ToString();
            // TODO: Perform the checking
            return true;
        }
    }
}