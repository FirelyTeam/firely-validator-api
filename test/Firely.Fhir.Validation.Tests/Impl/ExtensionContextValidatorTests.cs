using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Tests;

[TestClass]
public class ExtensionContextValidatorTests
{
    [DataTestMethod]
    [DataRow(ExtensionContextValidator.ContextType.DATATYPE, "boolean", true)]
    [DataRow(ExtensionContextValidator.ContextType.DATATYPE, "string", false)]
    [DataRow(ExtensionContextValidator.ContextType.RESOURCE, "active[0]", true)]
    [DataRow(ExtensionContextValidator.ContextType.RESOURCE, "OperationOutcome", false)]
    [DataRow(ExtensionContextValidator.ContextType.EXTENSION, "http://example.org/extensions#test", false)]
    [DataRow(ExtensionContextValidator.ContextType.ELEMENT, "Patient.active", true)]
    [DataRow(ExtensionContextValidator.ContextType.ELEMENT, "Practitioner.active", false)]
    [DataRow(ExtensionContextValidator.ContextType.FHIRPATH, "active.exists()", true)]
    [DataRow(ExtensionContextValidator.ContextType.FHIRPATH, "name.exists()", false)]
    public void Extension_UsedInContext_ValidatesCorrectly(ExtensionContextValidator.ContextType cType, string expr, bool expected)
    {
        var ctxValidator = new ExtensionContextValidator(
            [new(cType, expr)],
            ["true"] // only look at contexts
        );

        AssertAgainstContextValidator(ctxValidator, expected);
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

        AssertAgainstContextValidator(validator, expected);
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
                        ]){AllowAdditionalChildren = true}
                    )
                ]){AllowAdditionalChildren = true}
            )
        ]);

        var pat = new Patient { ActiveElement = new FhirBoolean() };
        var uriWithExt = new FhirBoolean(false);
        uriWithExt.AddExtension("http://example.org/extensions#testnested", new FhirString("unknown"));
        pat.ActiveElement.AddExtension("http://example.org/extensions#test", uriWithExt);
        
        var result = validator.Validate(
            pat
                .ToTypedElement(),
            new ValidationSettings(),
            new ValidationState { Location = { DefinitionPath = DefinitionPath.Start().InvokeSchema(schema) } }
        );
        
        Assert.AreEqual(result.IsSuccessful, expected);
    }

    private void AssertAgainstContextValidator(ExtensionContextValidator ctxValidator, bool expectedResult)
    {
        var schema = new ResourceSchema(
            new StructureDefinitionInformation(
                "http://test.org/testpat",
                null,
                "Patient",
                StructureDefinitionInformation.TypeDerivationRule.Constraint,
                false
            ));
        var validator = new ChildrenValidator([("active", new ChildrenValidator([("extension", ctxValidator)]))]);

        var pat = new Patient { ActiveElement = new FhirBoolean() };

        pat.ActiveElement.AddExtension("http://example.org/extensions#test", new FhirString("unknown"));

        var result = validator.Validate(
            pat
                .ToTypedElement(),
            new ValidationSettings(),
            new ValidationState { Location = { DefinitionPath = DefinitionPath.Start().InvokeSchema(schema) } }
        );

        Assert.AreEqual(result.IsSuccessful, expectedResult);
    }
}