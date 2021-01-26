using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using System.Collections.Generic;
using System.Linq;
using P = Hl7.Fhir.ElementModel.Types;

namespace Firely.Fhir.Validation
{
    public static class PrimitiveExtensions
    {
        public static object? ParseBindable(this ITypedElement instance)
        {
            switch (instance.InstanceType)
            {
                case "code":
                case "string":
                case "uri":
                    return instance.ParseString();
                case "Coding":
                    return instance.ParseCoding();
                case "CodeableConcept":
                    return instance.ParseConcept();
                case "Quantity":
                    return convertQuantityToCoding(instance);
                case "Extension":
                    return parseExtension(instance);
                default:
                    return null;
            }

            Coding convertQuantityToCoding(ITypedElement inst)
            {
                var q = inst.ParseQuantity();
                return new Coding(code: q.Unit, system: q.System == P.QuantityUnitSystem.UCUM ? P.Quantity.UCUM : null);
            }

            object? parseExtension(ITypedElement inst)
            {
                var valueChild = inst.Children("value").FirstOrDefault();
                return valueChild?.ParseBindable();
            }
        }

        public static Coding ParseCoding(this ITypedElement instance) =>
            new Coding(
                instance.Children("system").GetString(),
                instance.Children("code").GetString(),
                instance.Children("display").GetString());

        public static CodeableConcept ParseConcept(this ITypedElement instance) =>
            new CodeableConcept() { Coding = instance.Children("coding").Select(coding => coding.ParseCoding()).ToList() };

        public static string? GetString(this IEnumerable<ITypedElement> instance) => instance.SingleOrDefault()?.Value as string;

        public static string? ParseString(this ITypedElement instance) => instance?.Value as string;

        public static Hl7.Fhir.ElementModel.Types.Quantity ParseQuantity(this ITypedElement instance)
        {
            var value = instance.Children("value").SingleOrDefault()?.Value as decimal?;
            var code = instance.Children("code").GetString();


            if (value == null)
                throw Error.NotSupported("Cannot interpret quantities without a value");

            return new Hl7.Fhir.ElementModel.Types.Quantity(value.Value, code);
        }
    }
}