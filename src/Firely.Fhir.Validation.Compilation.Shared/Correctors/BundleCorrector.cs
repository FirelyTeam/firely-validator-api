using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal class BundleCorrector : ConstraintsCorrector
{
    public override void Correct(FhirRelease? fhirRelease, StructureDefinition sd)
    {
        base.Correct(fhirRelease, sd);

        addConstraints(sd.Snapshot);
    }

    protected override void CorrectConstraints(FhirRelease? fhirRelease, IEnumerable<ConstraintComponent> constraintElements)
    {
        foreach (var constraintElement in constraintElements)
        {
            constraintElement.Expression = constraintElement switch
            {
                { Key: "bdl-8", Expression: "fullUrl.contains('/_history/').not()" }
                                         => "fullUrl.exists() implies fullUrl.contains('/_history/').not()",
                
                _ => constraintElement.Expression
            };
        }
    }

    // See https://github.com/FirelyTeam/firely-validator-api/issues/152
    private static void addConstraints(StructureDefinition.SnapshotComponent? elements)
    {
        if (elements is null) 
            return;

#if R4_AND_LATER
        string[] toBeAdded = ["bdl-3a", "bdl-3b", "bdl-3c", "bdl-3d", "bdl-15"];
#else
        string[] toBeAdded = ["bdl-3a", "bdl-3b", "bdl-3c", "bdl-3d", "bdl-15", "bdl-10", "bdl-11", "bdl-12"];
#endif

        var bundleConstraintList = elements.Element.Where(ed => ed.Path == "Bundle").Select(c => c.Constraint).Single();

        bundleConstraintList.AddRange(toBeAdded.Select(getBundleConstraintByKey));
    }

    private static ConstraintComponent getBundleConstraintByKey(string key)
    {
        return key switch
        {
            "bdl-3a" => new ConstraintComponent
            {
                Severity = ConstraintSeverity.Error,
                Key = key,
                Human = "For collections of type document, message, searchset or collection, all entries must contain resources, and not have request or response element",
                Expression = "type in ('document' | 'message' | 'searchset' | 'collection') implies entry.all(resource.exists() and request.empty() and response.empty())"
            },
            "bdl-3b" => new ConstraintComponent
            {
                Severity = ConstraintSeverity.Error,
                Key = key,
                Human = "For collections of type history, all entries must contain request or response elements, and resources if the method is POST, PUT or PATCH",
                Expression = "type = 'history' implies entry.all(request.exists() and response.exists() and ((request.method in ('POST' | 'PATCH' | 'PUT')) = resource.exists()))"
            },
            "bdl-3c" => new ConstraintComponent
            {
                Severity = ConstraintSeverity.Error,
                Key = key,
                Human = "For collections of type transaction or batch, all entries must contain request elements, and resources if the method is POST, PUT or PATCH",
                Expression = "type in ('transaction' | 'batch') implies entry.all(request.method.exists() and ((request.method in ('POST' | 'PATCH' | 'PUT')) = resource.exists()))"
            },
            "bdl-3d" => new ConstraintComponent
            {
                Severity = ConstraintSeverity.Error,
                Key = key,
                Human = "For collections of type transaction-response or batch-response, all entries must contain response elements",
                Expression = "type in ('transaction-response' | 'batch-response') implies entry.all(response.exists())"
            },
            "bdl-10" => new ConstraintComponent
            {
                Severity = ConstraintSeverity.Error,
                Key = key,
                Human = "A document must have a date",
                Expression = "type = 'document' implies (timestamp.hasValue())"
            },
            "bdl-11" => new ConstraintComponent
            {
                Severity = ConstraintSeverity.Error,
                Key = key,
                Human = "A document must have a Composition as the first resource",
                Expression = "type = 'document' implies entry.first().resource.is(Composition)"
            },
            "bdl-12" => new ConstraintComponent
            {
                Severity = ConstraintSeverity.Error,
                Key = key,
                Human = "A message must have a MessageHeader as the first resource",
                Expression = "type = 'message' implies entry.first().resource.is(MessageHeader)"
            },
            "bdl-15" => new ConstraintComponent
            {
                Severity = ConstraintSeverity.Error,
                Key = key,
                Human = "Bundle resources where type is not transaction, transaction-response, batch, or batch-response or when the request is a POST SHALL have Bundle.entry.fullUrl populated",
                Expression = "type='transaction' or type='transaction-response' or type='batch' or type='batch-response' or entry.all(fullUrl.exists() or request.method='POST')"
            },
            _ => throw new InvalidOperationException("unknown key")
        };
    }
}