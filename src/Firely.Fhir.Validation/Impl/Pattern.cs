using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts that the value of an element matches a given pattern value.
    /// </summary>
    /// <remarks>The rules of whether an instance matches a pattern are laid down in 
    /// the description of <c>ElementDefinition</c>'s 
    /// <a href="http://hl7.org/fhir/elementdefinition-definitions.html#ElementDefinition.pattern_x_</remarks>">pattern element</a>
    /// in the FHIR specification.</remarks>
    [DataContract]
    public class Pattern : SimpleAssertion
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public ITypedElement PatternValue { get; private set; }
#else
        [DataMember]
        public ITypedElement PatternValue { get; private set; }
#endif

        public Pattern(ITypedElement patternValue)
        {
            PatternValue = patternValue ?? throw new ArgumentNullException(nameof(patternValue));
        }

        public Pattern(object patternPrimitive) : this(ElementNode.ForPrimitive(patternPrimitive)) { }

        public override string Key => "pattern[x]";

        public override object Value => PatternValue;

        public override Task<Assertions> Validate(ITypedElement input, ValidationContext vc)
        {
            var trace = new Trace($"Validate with pattern {PatternValue.ToJson()}");
            return !input.Matches(PatternValue)
                ? Task.FromResult(new Assertions(trace, ResultAssertion.CreateFailure(new IssueAssertion(Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, input.Location, $"Value does not match pattern '{PatternValue.ToJson()}"))))
                : Task.FromResult(Assertions.SUCCESS + trace);
        }


        public override JToken ToJson()
        {
            return new JProperty(Key, PatternValue.ToJObject());
        }
    }
}
