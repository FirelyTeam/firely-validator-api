#if R4_AND_LATER

using Hl7.Fhir.Specification;

namespace Firely.Fhir.Validation.Compilation;

internal class QuestionnaireCorrector : ConstraintsCorrector
{
    public QuestionnaireCorrector()
    {
        RegisterInvalidConstraint("Questionnaire", 
                                  "que-0",
                                  "name.matches('[A-Z]([A-Za-z0-9_]){0,254}')",
                                  "name.exists() implies name.matches('[A-Z]([A-Za-z0-9_]){0,254}')",
                                  FhirRelease.R4);

        // correct datatype in expression:
        RegisterInvalidConstraint("Questionnaire.item.enableWhen", 
                                  "que-7",
                                  "operator = 'exists' implies (answer is Boolean)",
                                  "operator = 'exists' implies (answer is boolean)",
                                  FhirRelease.R4);
    }
}

#endif