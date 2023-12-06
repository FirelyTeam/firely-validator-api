using FluentAssertions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation.Tests
{
    [TestClass]
    public class ScopedNodeOnDictionaryTests
    {

        [TestMethod]
        public void BaseToScopedNodeTests()
        {
            var humanName = new HumanName() { Family = "Brown", Given = new[] { "Joe" } };
            var fhirBool = new FhirBoolean(true);
            fhirBool.AddExtension("http://example.org/extension", new FhirString("some extension"));
            var patient = new Patient() { ActiveElement = fhirBool };
            patient.AddExtension("http://example.org/extension", new FhirString("some extension"));
            patient.AddExtension("http://example.org/otherextions", new FhirDecimal(12));
            patient.Name.Add(new HumanName() { Family = "Doe", Given = ["John", "J."] });


            var node = printNode(patient.ToScopedNode());

            node.Should().BeEquivalentTo("""
                {
                  Name: Patient
                  Value: 
                  Type: Patient
                }
                  {
                    Name: extension
                    Value: 
                    Type: Extension
                  }
                    {
                      Name: url
                      Value: http://example.org/extension
                      Type: String
                    }
                    {
                      Name: value
                      Value: some extension
                      Type: string
                    }
                  {
                    Name: extension
                    Value: 
                    Type: Extension
                  }
                    {
                      Name: url
                      Value: http://example.org/otherextions
                      Type: String
                    }
                    {
                      Name: value
                      Value: 12
                      Type: decimal
                    }
                  {
                    Name: active
                    Value: True
                    Type: boolean
                  }
                    {
                      Name: extension
                      Value: 
                      Type: Extension
                    }
                      {
                        Name: url
                        Value: http://example.org/extension
                        Type: String
                      }
                      {
                        Name: value
                        Value: some extension
                        Type: string
                      }
                  {
                    Name: name
                    Value: 
                    Type: HumanName
                  }
                    {
                      Name: family
                      Value: Doe
                      Type: string
                    }
                    {
                      Name: given
                      Value: John
                      Type: string
                    }
                    {
                      Name: given
                      Value: J.
                      Type: string
                    }

                """);


#pragma warning disable CS0618 // Type or member is obsolete
            string printNode<T>(IBaseElementNavigator<T> node, int depth = 0) where T : IBaseElementNavigator<T>
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var indent = new string(' ', depth * 2);

                var result = $$"""
                    {{indent}}{
                    {{indent}}  Name: {{node.Name}}
                    {{indent}}  Value: {{node.Value}}
                    {{indent}}  Type: {{node.InstanceType}}
                    {{indent}}}

                    """;
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
                    result += $$"""
                        {{indent}}{
                        {{indent}}  Key: {{node.Key}}
                        {{indent}}  Value: {{printValue(node.Value, depth)}}
                        {{indent}}}

                        """;
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
