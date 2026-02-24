using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal class BundleCorrector : ConstraintsCorrector
{
    public BundleCorrector()
    {
        RegisterInvalidConstraint("Bundle.entry",
                                  "bdl-8",
                                  "fullUrl.contains('/_history/').not()",
                                  "fullUrl.exists() implies fullUrl.contains('/_history/').not()",
                                  FhirRelease.STU3, FhirRelease.R4);

        // See https://github.com/FirelyTeam/firely-validator-api/issues/152
        RegisterMissingConstraint("Bundle", "bdl-3a", createBdl3A, FhirRelease.STU3, FhirRelease.R4, FhirRelease.R4B);
        RegisterMissingConstraint("Bundle", "bdl-3b", createBdl3B, FhirRelease.STU3, FhirRelease.R4, FhirRelease.R4B);
        RegisterMissingConstraint("Bundle", "bdl-3c", createBdl3C, FhirRelease.STU3, FhirRelease.R4, FhirRelease.R4B);
        RegisterMissingConstraint("Bundle", "bdl-3d", createBdl3D, FhirRelease.STU3, FhirRelease.R4, FhirRelease.R4B);
        RegisterMissingConstraint("Bundle", "bdl-10", createBdl10, FhirRelease.STU3);
        RegisterMissingConstraint("Bundle", "bdl-11", createBdl11, FhirRelease.STU3);
        RegisterMissingConstraint("Bundle", "bdl-12", createBdl12, FhirRelease.STU3);
        RegisterMissingConstraint("Bundle", "bdl-15", createBdl15, FhirRelease.STU3, FhirRelease.R4, FhirRelease.R4B);
    }

    private static ConstraintComponent createBdl3A()
    {
        return new ConstraintComponent
        {
            Severity = ConstraintSeverity.Error,
            Key = "bdl-3a",
            Human = "For collections of type document, message, searchset or collection, all entries must contain resources, and not have request or response elements",
            Expression = "type in ('document' | 'message' | 'searchset' | 'collection') implies entry.all(resource.exists() and request.empty() and response.empty())"
        };
    }

    private static ConstraintComponent createBdl3B()
    {
        return new ConstraintComponent
        {
            Severity = ConstraintSeverity.Error,
            Key = "bdl-3b",
            Human = "For collections of type history, all entries must contain request or response elements, and resources if the method is POST, PUT or PATCH",
            Expression = "type = 'history' implies entry.all(request.exists() and response.exists() and ((request.method in ('POST' | 'PATCH' | 'PUT')) = resource.exists()))"
        };
    }

    private static ConstraintComponent createBdl3C()
    {
        return new ConstraintComponent
        {
            Severity = ConstraintSeverity.Error,
            Key = "bdl-3c",
            Human = "For collections of type transaction or batch, all entries must contain request elements, and resources if the method is POST, PUT or PATCH",
            Expression = "type in ('transaction' | 'batch') implies entry.all(request.method.exists() and ((request.method in ('POST' | 'PATCH' | 'PUT')) = resource.exists()))"
        };
    }

    private static ConstraintComponent createBdl3D()
    {
        return new ConstraintComponent
        {
            Severity = ConstraintSeverity.Error,
            Key = "bdl-3d",
            Human = "For collections of type transaction-response or batch-response, all entries must contain response elements",
            Expression = "type in ('transaction-response' | 'batch-response') implies entry.all(response.exists())"
        };
    }

    private static ConstraintComponent createBdl10()
    {
        return new ConstraintComponent
        {
            Severity = ConstraintSeverity.Error,
            Key = "bdl-10",
            Human = "A document must have a date",
            Expression = "type = 'document' implies (timestamp.hasValue())"
        };
    }

    private static ConstraintComponent createBdl11()
    {
        return new ConstraintComponent
        {
            Severity = ConstraintSeverity.Error,
            Key = "bdl-11",
            Human = "A document must have a Composition as the first resource",
            Expression = "type = 'document' implies entry.first().resource.is(Composition)"
        };
    }

    private static ConstraintComponent createBdl12()
    {
        return new ConstraintComponent
        {
            Severity = ConstraintSeverity.Error,
            Key = "bdl-12",
            Human = "A message must have a MessageHeader as the first resource",
            Expression = "type = 'message' implies entry.first().resource.is(MessageHeader)"
        };
    }

    private static ConstraintComponent createBdl15()
    {
        return new ConstraintComponent
        {
            Severity = ConstraintSeverity.Error,
            Key = "bdl-15",
            Human = "Bundle resources where type is not transaction, transaction-response, batch, or batch-response or when the request is a POST SHALL have Bundle.entry.fullUrl populated",
            Expression = "type='transaction' or type='transaction-response' or type='batch' or type='batch-response' or entry.all(fullUrl.exists() or request.method='POST')"
        };
    }
}