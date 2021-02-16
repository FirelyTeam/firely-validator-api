﻿/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using System;

namespace Firely.Fhir.Validation
{
    internal class FhirEvaluationContext : EvaluationContext
    {
        /// <summary>Creates a new <see cref="FhirEvaluationContext"/> instance with default property values.</summary>
        public static new FhirEvaluationContext CreateDefault() => new FhirEvaluationContext();

        /// <summary>Default constructor. Creates a new <see cref="FhirEvaluationContext"/> instance with default property values.</summary>
        public FhirEvaluationContext() : base()
        {
        }

        /// <inheritdoc cref="EvaluationContext.EvaluationContext(ITypedElement)"/>
        public FhirEvaluationContext(ITypedElement resource) : base(resource)
        {
        }

        /// <inheritdoc cref="EvaluationContext.EvaluationContext(ITypedElement, ITypedElement)"/>
        public FhirEvaluationContext(ITypedElement resource, ITypedElement rootResource) : base(resource, rootResource)
        {
        }

        private Func<string, ITypedElement> _elementResolver;

        public Func<string, ITypedElement> ElementResolver
        {
            get { return _elementResolver; }
            set { _elementResolver = value; }
        }
    }
}
