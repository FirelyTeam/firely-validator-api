using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Firely.Fhir.Validation.Compilation
{
    internal class CommonExtensionContextComponent
    {
        internal CommonExtensionContextComponent(IEnumerable<(ExtensionContextValidator.ContextType?, string)> contexts,
            IEnumerable<string> invariants) // internal for testing purposes
        {
            Contexts = contexts;
            Invariants = invariants;
        }

        internal IEnumerable<(ExtensionContextValidator.ContextType?, string)> Contexts { get; private set; }

        internal IEnumerable<string> Invariants { get; private set; }

#if STU3
        public static bool TryCreate(ElementDefinitionNavigator nav, [NotNullWhen(true)] out CommonExtensionContextComponent? result)
        {
            var strDef = nav.StructureDefinition;
            if (strDef.ContextType is null && !strDef.Context.Any() && !strDef.ContextInvariant.Any()) // if nothing is set, we don't need to validate
            {
                result = null;
                return false;
            }

            ExtensionContextValidator.ContextType? contextType = strDef.ContextType switch
            {
                StructureDefinition.ExtensionContext.Resource => ExtensionContextValidator.ContextType.RESOURCE,
                StructureDefinition.ExtensionContext.Datatype => ExtensionContextValidator.ContextType.DATATYPE,
                StructureDefinition.ExtensionContext.Extension => ExtensionContextValidator.ContextType.EXTENSION,
                null => null,
                _ => throw new InvalidOperationException($"Unknown context type {strDef.ContextType.Value}")
            };
            
            var contexts = strDef.Context.Select(c => (contextType, c));
            
            var invariants = strDef.ContextInvariant;
            
            result = new CommonExtensionContextComponent(contexts, invariants);
            return true;
        }
#else
        public static bool TryCreate(ElementDefinitionNavigator def, [NotNullWhen(true)] out CommonExtensionContextComponent? result)
        {
            var strDef = def.StructureDefinition;
            if (strDef.Context.Count == 0 && !strDef.ContextInvariant.Any()) // if nothing is set, we don't need to validate
            {
                result = null;
                return false;
            }
            
            IEnumerable<(ExtensionContextValidator.ContextType?, string)> contexts = strDef.Context.Select<StructureDefinition.ContextComponent, (ExtensionContextValidator.ContextType?, string)>(c => 
                (
                    c.Type switch
                    {
                        StructureDefinition.ExtensionContextType.Fhirpath => ExtensionContextValidator.ContextType.FHIRPATH,
                        StructureDefinition.ExtensionContextType.Element => ExtensionContextValidator.ContextType.ELEMENT,
                        StructureDefinition.ExtensionContextType.Extension => ExtensionContextValidator.ContextType.EXTENSION,
                        _ => null
                    }, 
                    c.Expression
                )
            );
            
            var invariants = strDef.ContextInvariant;
            
            result = new CommonExtensionContextComponent(contexts, invariants);
            return true;
        }
#endif
    }
}