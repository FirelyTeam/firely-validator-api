/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System;
using System.Runtime.CompilerServices;

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: CLSCompliant(true)]
[assembly: InternalsVisibleTo("Firely.Fhir.Validation.Tests")]
#if STU3
[assembly: InternalsVisibleTo("Firely.Fhir.Validation.Compilation.Tests.STU3")]
#else
[assembly: InternalsVisibleTo("Firely.Fhir.Validation.Compilation.Tests.R4")]
#endif
