using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public class Pattern : SimpleAssertion
    {
        private readonly ITypedElement _pattern;

        public Pattern(ITypedElement patternValue)
        {
            this._pattern = patternValue ?? throw new ArgumentNullException(nameof(patternValue));
        }

        public Pattern(object fixedValue) : this(ElementNode.ForPrimitive(fixedValue)) { }

        public override string Key => "pattern[x]";

        public override object Value => _pattern;

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var result = Assertions.EMPTY + new Trace($"Validate with pattern {_pattern.ToJson()}");
            return !input.Matches(_pattern)
                ? Task.FromResult(result + ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, input.Location, $"Value does not match pattern '{_pattern.ToJson()}")))
                : Task.FromResult(result + Assertions.SUCCESS);
        }


        public override JToken ToJson()
        {
            return new JProperty(Key, _pattern.ToJObject());
        }
    }
}
