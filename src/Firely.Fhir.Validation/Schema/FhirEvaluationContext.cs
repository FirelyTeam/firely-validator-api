/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using System;

namespace Firely.Fhir.Validation
{
    internal class FhirEvaluationContext : EvaluationContext
    {
        /// <summary>Creates a new <see cref="FhirEvaluationContext"/> instance with default property values.</summary>
        public static new FhirEvaluationContext CreateDefault() => new();

        private ITypedElement? notFound(string _) => null;

        /// <summary>Default constructor. Creates a new <see cref="FhirEvaluationContext"/> instance with default property values.</summary>
        public FhirEvaluationContext() : base()
        {
            _elementResolver = notFound;
        }

        /// <inheritdoc cref="EvaluationContext.EvaluationContext(ITypedElement)"/>
        public FhirEvaluationContext(ITypedElement resource) : base(resource)
        {
            _elementResolver = notFound;
        }

        /// <inheritdoc cref="EvaluationContext.EvaluationContext(ITypedElement, ITypedElement)"/>
        public FhirEvaluationContext(ITypedElement resource, ITypedElement rootResource) : base(resource, rootResource)
        {
            _elementResolver = notFound;
        }

        private Func<string, ITypedElement?> _elementResolver;

        public Func<string, ITypedElement?> ElementResolver
        {
            get { return _elementResolver; }
            set { _elementResolver = value; }
        }
    }
}
