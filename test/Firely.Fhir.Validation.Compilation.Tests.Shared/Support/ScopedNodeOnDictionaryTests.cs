using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [TestClass]
    public class ScopedNodeOnDictionaryTests
    {

        [TestMethod]
        public void MyTestMethod()
        {
            var humanName = new HumanName() { Family = "Brown", Given = new[] { "Joe" } };
            var fhirBool = new FhirBoolean(true);
            fhirBool.AddExtension("http://example.org/extension", new FhirString("some extension"));
            var patient = new Patient() { ActiveElement = fhirBool };
            //patient.AddExtension("http://example.org/extension", new FhirString("some extension"));
            //patient.AddExtension("http://example.org/otherextions", new FhirDecimal(12));
            //patient.Name.Add(new HumanName() { Family = "Doe", Given = ["John", "J."] });



            Debug.WriteLine("ITypedElement");
            Debug.WriteLine(printNode(humanName.ToTypedElement()));

            Debug.WriteLine("====\nIScopedNode");


            //Debug.WriteLine(printDictionary(patient));


            Debug.WriteLine(printNode(humanName.ToScopedNode()));


            string printNode<T>(IBaseElementNavigator<T> node, int depth = 0) where T : IBaseElementNavigator<T>
            {
                var indent = new string(' ', depth * 2);

                var result = $"{indent}{{\n{indent}  Name: {node.Name}\n{indent}  Value: {node.Value}\n{indent}  Type: {node.InstanceType}\n{indent}}}\n";
                foreach (var child in node.Children())
                    result += printNode(child, depth + 1);
                return result;
            }

            string printDictionary(IReadOnlyDictionary<string, object> dict, int depth = 0)
            {
                var result = string.Empty;
                var indent = new string(' ', depth * 2);
                foreach (var node in dict)
                {
                    result += $"{indent}{{\n{indent}  Key: {node.Key}\n{indent}  Value: {printValue(node.Value, depth)}\n{indent}}}\n";
                }
                return result;
            }

            string printValue(object value, int depth) =>
                value switch
                {
                    byte[] bt => string.Empty,
                    string or bool or decimal or DateTimeOffset or int or long or XHtml => value.ToString()!,
                    IReadOnlyDictionary<string, object> dict => printDictionary(dict, depth + 1),
                    IEnumerable<object> list => list.Select(l => printValue(l, depth)).Aggregate(string.Empty, (current, next) => current + next),
                    _ => throw new NotImplementedException()
                };
        }

    }
}
