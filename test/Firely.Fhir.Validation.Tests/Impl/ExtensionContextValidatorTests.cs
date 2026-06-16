using FluentAssertions;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Firely.Fhir.Validation.Tests;

[TestClass]
public class ExtensionContextValidatorTests
{
    private readonly IElementSchemaResolver _boolSchemaResolver = new TestResolver([
        new DatatypeSchema(new StructureDefinitionInformation(
            "http://hl7.org/fhir/StructureDefinition/boolean",
            ["http://hl7.org/fhir/StructureDefintion/DataType", "http://hl7.org/fhir/StructureDefinition/Element", "http://hl7.org/fhir/StructureDefinition/Base"],
            "boolean",
            StructureDefinitionInformation.TypeDerivationRule.Constraint,
            false))
    ]);

    private readonly IElementSchemaResolver _humanNameSchemaResolver = new TestResolver(
    [
        new DatatypeSchema(new StructureDefinitionInformation(
            "http://hl7.org/fhir/StructureDefinition/HumanName",
            ["http://hl7.org/fhir/StructureDefintion/DataType", "http://hl7.org/fhir/StructureDefinition/Element", "http://hl7.org/fhir/StructureDefinition/Base"],
            "HumanName",
            StructureDefinitionInformation.TypeDerivationRule.Constraint,
            false))
    ]);

    [DataTestMethod]
    [DataRow(ExtensionContextValidator.ContextType.DATATYPE, "boolean", true)]
    [DataRow(ExtensionContextValidator.ContextType.DATATYPE, "string", false)]
    [DataRow(ExtensionContextValidator.ContextType.RESOURCE, "Patient.active", true)]
    [DataRow(ExtensionContextValidator.ContextType.RESOURCE, "OperationOutcome", false)]
    [DataRow(ExtensionContextValidator.ContextType.EXTENSION, "http://example.org/extensions#test", false)]
    [DataRow(ExtensionContextValidator.ContextType.ELEMENT, "boolean", true)]
    [DataRow(ExtensionContextValidator.ContextType.ELEMENT, "Resource.active", true)]
    [DataRow(ExtensionContextValidator.ContextType.ELEMENT, "Element", true)]
    [DataRow(ExtensionContextValidator.ContextType.ELEMENT, "Resource", false)]
    [DataRow(ExtensionContextValidator.ContextType.ELEMENT, "string", false)]
    [DataRow(ExtensionContextValidator.ContextType.FHIRPATH, "active.exists()", true)]
    [DataRow(ExtensionContextValidator.ContextType.FHIRPATH, "name.exists()", false)]
    public void Extension_UsedInContext_ValidatesCorrectly(ExtensionContextValidator.ContextType cType, string expr, bool expected)
    {
        var ctxValidator = new ExtensionContextValidator(
            [new(cType, expr)],
            ["true"] // only look at contexts
        );

        assertAgainstContextValidator(ctxValidator, expected);
    }

    [DataTestMethod]
    [DataRow(false, "true", "true", "false")]
    [DataRow(true, "extension.exists()")]
    [DataRow(true, "extension = %extension")]
    [DataRow(false, "active.exists()")]
    public void Extension_WithContextInvariants_ValidatesCorrectly(bool expected, params string[] invariants)
    {
        var validator = new ExtensionContextValidator(
            [],
            invariants
        );

        assertAgainstContextValidator(validator, expected);
    }

    [DataTestMethod]
    [DataRow(true, "http://example.org/extensions#test")]
    [DataRow(false, "http://example.org/extensions#testnested")]
    public void Extension_WithExtensionContext_ValidatesCorrectly(bool expected, string url)
    {
        var schema = new ResourceSchema(
            new StructureDefinitionInformation(
                "http://test.org/testpat",
                null,
                "Patient",
                StructureDefinitionInformation.TypeDerivationRule.Constraint,
                false
            ));

        var ctxValidator = new ExtensionContextValidator(
            [new(ExtensionContextValidator.ContextType.EXTENSION, url)],
            ["true"]
        );

        var validator = new ChildrenValidator([
            ("active",
                new ChildrenValidator([
                    ("extension",
                        new ChildrenValidator([
                            ("value",
                                new ChildrenValidator([("extension", ctxValidator)])
                            )
                        ]) { AllowAdditionalChildren = true }
                    )
                ]) { AllowAdditionalChildren = true }
            )
        ]);

        var pat = new Patient { ActiveElement = new FhirBoolean() };
        var uriWithExt = new FhirBoolean(false);
        uriWithExt.AddExtension("http://example.org/extensions#testnested", new FhirString("unknown"));
        pat.ActiveElement.AddExtension("http://example.org/extensions#test", uriWithExt);

        var result = validator.Validate(
            pat
                .ToPocoNode(),
            new ValidationSettings(),
            new ValidationState { Location = { DefinitionPath = DefinitionPath.Start().InvokeSchema(schema) } }
        );

        Assert.AreEqual(expected, result.IsSuccessful);
    }

    private void assertAgainstContextValidator(ExtensionContextValidator ctxValidator, bool expectedResult)
    {
        var schema = new ResourceSchema(
            new StructureDefinitionInformation(
                "http://test.org/Patient",
                ["http://hl7.org/fhir/StructureDefinition/DomainResource", "http://hl7.org/fhir/StructureDefinition/Resource", "http://hl7.org/fhir/StructureDefinition/Base"],
                "Patient",
                StructureDefinitionInformation.TypeDerivationRule.Constraint,
                false
            ));
        var validator = new ChildrenValidator([("active", new ChildrenValidator([("extension", ctxValidator)]))]);

        var pat = new Patient { ActiveElement = new FhirBoolean() };

        pat.ActiveElement.AddExtension("http://example.org/extensions#test", new FhirString("unknown"));

        var result = validator.Validate(
            pat
                .ToPocoNode(),
            new ValidationSettings() {ElementSchemaResolver = _boolSchemaResolver},
            new ValidationState { Location = { DefinitionPath = DefinitionPath.Start().InvokeSchema(schema) } }
        );

        result.IsSuccessful.Should().Be(expectedResult);
    }

    // Issue 540: extension contexts defined on recursive elements (contentReference) must match at any depth
    [DataTestMethod]
    [DataRow("Questionnaire.item", true)]
    [DataRow("Questionnaire.item.item", true)] // the contentReference target is also reachable via the literal path
    [DataRow("Questionnaire.item.enableWhen", false)]
    [DataRow("Questionnaire", false)]
    public void ExtensionContext_OnRecursiveElement_MatchesAtAnyDepth(string expression, bool expected)
    {
        var questionnaire = new Questionnaire
        {
            Item =
            [
                new Questionnaire.ItemComponent
                {
                    LinkId = "1",
                    Item =
                    [
                        new Questionnaire.ItemComponent
                        {
                            LinkId = "1.1",
                            Item = [new Questionnaire.ItemComponent { LinkId = "1.1.1" }]
                        }
                    ]
                }
            ]
        };

        questionnaire.Item[0].Item[0].Item[0].AddExtension("http://example.org/extensions#test", new FhirString("unknown"));

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.ELEMENT, expression)], []);
        var validator = new ChildrenValidator([
            ("item", new ChildrenValidator([
                ("item", new ChildrenValidator([
                    ("item", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })
                ]) { AllowAdditionalChildren = true })
            ]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(questionnaire.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // Element contexts naming an abstract base type must match elements of derived types
    [DataTestMethod]
    [DataRow("BackboneElement", true)]
    [DataRow("Patient.contact", true)]
    [DataRow("Element", true)]
    [DataRow("Patient.communication", false)]
    public void ExtensionContext_OnBackboneElement_MatchesAbstractType(string expression, bool expected)
    {
        var patient = new Patient { Contact = [new Patient.ContactComponent { Gender = AdministrativeGender.Other }] };
        patient.Contact[0].AddExtension("http://example.org/extensions#test", new FhirString("unknown"));

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.ELEMENT, expression)], []);
        var validator = new ChildrenValidator([
            ("contact", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(patient.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // Issue 540/481: elements aliased via contentReference (e.g. Parameters.parameter.part) must match
    // contexts declared against the referenced element
    [DataTestMethod]
    [DataRow("Parameters.parameter", true)]
    [DataRow("Parameters.parameter.part", true)]
    [DataRow("Parameters", false)]
    public void ExtensionContext_OnAliasedElement_MatchesReferencedElement(string expression, bool expected)
    {
        var parameters = new Parameters
        {
            Parameter =
            [
                new Parameters.ParameterComponent
                {
                    Name = "outer",
                    Part = [new Parameters.ParameterComponent { Name = "inner" }]
                }
            ]
        };

        parameters.Parameter[0].Part[0].AddExtension("http://example.org/extensions#test", new FhirString("unknown"));

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.ELEMENT, expression)], []);
        var validator = new ChildrenValidator([
            ("parameter", new ChildrenValidator([
                ("part", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })
            ]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(parameters.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // Extension context type: "Another extension. The canonical URL of the extension, optionally followed by
    // #code for extension that appear within a complex extension"
    // https://hl7.org/fhir/R4/defining-extensions.html#context

    // An EXTENSION context never matches when the extension is not placed inside another extension
    [DataTestMethod]
    [DataRow("http://example.org/extensions/any")]
    [DataRow("http://example.org/extensions/any#sub")]
    public void ExtensionContext_NotInsideAnyExtension_NeverMatches(string expression)
    {
        var patient = new Patient();
        patient.AddExtension("http://example.org/extensions/test", new FhirString("x"));

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.EXTENSION, expression)], []);
        var validator = new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true };

        var result = validator.Validate(patient.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().BeFalse();
    }

    // Extension contexts must match extensions nested directly within a (complex) extension
    [DataTestMethod]
    [DataRow("http://example.org/extensions/complex", true)]
    [DataRow("http://example.org/extensions/other", false)]
    [DataRow("http://example.org/extensions/complex#sub", false)]
    public void ExtensionContext_OnDirectlyNestedExtension_MatchesParentUrl(string expression, bool expected)
    {
        var patient = new Patient();
        var outer = new Extension { Url = "http://example.org/extensions/complex" };
        outer.AddExtension("http://example.org/extensions/nested", new FhirString("unknown"));
        patient.Extension.Add(outer);

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.EXTENSION, expression)], []);
        var validator = new ChildrenValidator([
            ("extension", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(patient.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // [canonical]#[code] extension contexts match extensions within a named sub-extension of a complex extension
    [DataTestMethod]
    [DataRow("http://example.org/extensions/complex#sub", true)]
    [DataRow("http://example.org/extensions/complex#othersub", false)]
    [DataRow("http://example.org/extensions/other#sub", false)]
    [DataRow("sub", true)]
    public void ExtensionContext_WithinComplexExtensionPart_MatchesCanonicalAndCode(string expression, bool expected)
    {
        var patient = new Patient();
        var outer = new Extension { Url = "http://example.org/extensions/complex" };
        var sub = new Extension { Url = "sub" };
        sub.AddExtension("http://example.org/extensions/nested", new FhirString("unknown"));
        outer.Extension.Add(sub);
        patient.Extension.Add(outer);

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.EXTENSION, expression)], []);
        var validator = new ChildrenValidator([
            ("extension", new ChildrenValidator([
                ("extension", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })
            ]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(patient.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // An extension may also be placed on a value[x] element of a complex extension; the host is the enclosing extension
    [DataTestMethod]
    [DataRow("http://example.org/extensions/complex", true)]
    [DataRow("http://example.org/extensions/other", false)]
    public void ExtensionContext_OnValueXOfHostExtension_MatchesHostUrl(string expression, bool expected)
    {
        var patient = new Patient();
        var outer = new Extension { Url = "http://example.org/extensions/complex", Value = new FhirString("v") };
        outer.Value.AddExtension("http://example.org/extensions/nested", new FhirString("x"));
        patient.Extension.Add(outer);

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.EXTENSION, expression)], []);
        var validator = new ChildrenValidator([
            ("extension", new ChildrenValidator([("value", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(patient.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // canonical#code must also resolve when the extension sits on the value[x] of the named sub-extension
    [DataTestMethod]
    [DataRow("http://example.org/extensions/complex#sub", true)]
    [DataRow("http://example.org/extensions/other#sub", false)]
    [DataRow("http://example.org/extensions/complex#othersub", false)]
    public void ExtensionContext_CanonicalCode_OnValueXOfSubExtension_MatchesCorrectly(string expression, bool expected)
    {
        var patient = new Patient();
        var outer = new Extension { Url = "http://example.org/extensions/complex" };
        var sub = new Extension { Url = "sub", Value = new FhirString("v") };
        sub.Value.AddExtension("http://example.org/extensions/nested", new FhirString("x"));
        outer.Extension.Add(sub);
        patient.Extension.Add(outer);

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.EXTENSION, expression)], []);
        var validator = new ChildrenValidator([
            ("extension", new ChildrenValidator([
                ("extension", new ChildrenValidator([("value", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })]) { AllowAdditionalChildren = true })
            ]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(patient.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // FHIRPath contexts select the set of elements the extension may appear on (set membership, not truthiness)
    [DataTestMethod]
    [DataRow("active", true)]
    [DataRow("name", false)]
    [DataRow("active.exists()", true)] // predicate-style expressions remain supported
    [DataRow("active.exists().not()", false)] // predicate returning false must not match
    public void FhirPathExtensionContext_SelectsElementSet(string expression, bool expected)
    {
        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.FHIRPATH, expression)], []);

        assertAgainstContextValidator(ctxValidator, expected);
    }

    // %resource. prefix is used by some published extensions; the focus is already the resource so it should resolve
    [DataTestMethod]
    [DataRow("%resource.active", true)]
    [DataRow("%resource.name", false)]
    public void FhirPathExtensionContext_ResourceVariablePrefix_BackwardsCompat(string expression, bool expected)
    {
        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.FHIRPATH, expression)], []);

        assertAgainstContextValidator(ctxValidator, expected);
    }

    // Union expressions select multiple elements; the extension may appear on any of them
    [DataTestMethod]
    [DataRow("active | deceased", true)]   // extension is on active
    [DataRow("name | contact", false)]     // extension is on active, not in this union
    [DataRow("name | active", true)]       // active is included in the union
    public void FhirPathExtensionContext_UnionExpression_MatchesAnySelectedElement(string expression, bool expected)
    {
        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.FHIRPATH, expression)], []);

        assertAgainstContextValidator(ctxValidator, expected);
    }

    // Extensions on nested elements must match paths that navigate into those elements
    [DataTestMethod]
    [DataRow("name.family", true)]
    [DataRow("name", false)]
    [DataRow("name.given", false)]
    public void FhirPathExtensionContext_DeepNavigation_MatchesNestedElement(string expression, bool expected)
    {
        var patient = new Patient { Name = [new HumanName { Family = "Smith" }] };
        patient.Name[0].FamilyElement.AddExtension("http://example.org/extensions#test", new FhirString("x"));

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.FHIRPATH, expression)], []);
        var validator = new ChildrenValidator([
            ("name", new ChildrenValidator([("family", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(patient.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // ofType() filters a choice element to a specific type; the selected element must be the contextNode
    [DataTestMethod]
    [DataRow("deceased.ofType(boolean)", true)]
    [DataRow("deceased.ofType(dateTime)", false)]
    [DataRow("deceased", true)] // unfiltered selects the element regardless of type
    public void FhirPathExtensionContext_OfType_FiltersChoiceElement(string expression, bool expected)
    {
        var patient = new Patient { Deceased = new FhirBoolean(false) };
        patient.Deceased.AddExtension("http://example.org/extensions#test", new FhirString("x"));

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.FHIRPATH, expression)], []);
        var validator = new ChildrenValidator([
            ("deceased", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(patient.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // Element contexts for choice elements match both the definition name and the suffixed instance name
    [DataTestMethod]
    [DataRow("Patient.deceased[x]", true)]
    [DataRow("Patient.deceasedBoolean", true)]
    [DataRow("Patient.deceasedDateTime", false)]
    public void ExtensionContext_OnChoiceElement_MatchesDefinitionAndInstanceName(string expression, bool expected)
    {
        var patient = new Patient { Deceased = new FhirBoolean(false) };
        patient.Deceased.AddExtension("http://example.org/extensions#test", new FhirString("unknown"));

        var ctxValidator = new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.ELEMENT, expression)], []);
        var validator = new ChildrenValidator([
            ("deceased", new ChildrenValidator([("extension", ctxValidator)]) { AllowAdditionalChildren = true })
        ]) { AllowAdditionalChildren = true };

        var result = validator.Validate(patient.ToPocoNode(), new ValidationSettings(), new ValidationState());

        result.IsSuccessful.Should().Be(expected);
    }

    // Issue 402
    [DataTestMethod]
    [DataRow("boolean", false)]
    [DataRow("string", true)]
    [DataRow("Resource", false)]
    [DataRow("Patient", false)]
    [DataRow("Patient.active", false)]
    [DataRow("Patient.name", false)]
    [DataRow("Patient.name.family", true)]
    [DataRow("Patient.name.given", false)]
    [DataRow("HumanName.given", false)]
    [DataRow("HumanName.family", true)]
    public void ComplexElementContextValidation_Should_DetectCorrectDefinitions(string expression, bool expected)
    {
        var pat = new Patient { Name = [new HumanName() { Family = "LastNameExample" }] };

        pat.Name.First().FamilyElement.AddExtension("http://example.org/extensions#test", new FhirString("unknown"));

        var validator = new ChildrenValidator(
            [
                ("name", new ChildrenValidator(
                [
                    ("family",
                        new ChildrenValidator([
                            ("extension",
                                new ExtensionContextValidator([new(ExtensionContextValidator.ContextType.ELEMENT, expression)], ["true"]))
                        ]))
                ]))
            ]
        );
        
        var result = validator.Validate(
            pat
                .ToPocoNode(),
            new ValidationSettings() { ElementSchemaResolver = _humanNameSchemaResolver },
            new ValidationState { Location = { DefinitionPath = DefinitionPath.Start().InvokeSchema(new ResourceSchema(new StructureDefinitionInformation("http://test.org/Patient", null, "Patient", StructureDefinitionInformation.TypeDerivationRule.Constraint, false))) } }
        );
        
        result.IsSuccessful.Should().Be(expected);
    }
}