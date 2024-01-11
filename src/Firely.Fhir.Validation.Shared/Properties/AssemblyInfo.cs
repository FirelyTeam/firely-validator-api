/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using System;
using System.Runtime.CompilerServices;

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: CLSCompliant(true)]

#if DEBUG
[assembly: InternalsVisibleTo("Firely.Fhir.Validation.Compilation.Enterprise.STU3.Tests")]
[assembly: InternalsVisibleTo("Firely.Fhir.Validation.Compilation.Enterprise.R4.Tests")]
[assembly: InternalsVisibleTo("Firely.Fhir.Validation.Compilation.Enterprise.R5.Tests")]
#endif

#if RELEASE
[assembly: InternalsVisibleTo("Firely.Fhir.Validation.Compilation.Enterprise.STU3.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c11eea5df3095844b027f018b356bc326a5a30b1f245010ad789589aa685569b2eb7f5f2ea5c49dafed338e3d9969eab21848c6c20a6b0a22c5ff7797d9a5062d7f3e42478e905d72a3dde344086a003f2df9deeb838e206d92c8cc59150c3151e9490381321f77a716e0a2b24a585b302ba2b3db37966a3da9abe4c601ba4c1")]
[assembly: InternalsVisibleTo("Firely.Fhir.Validation.Compilation.Enterprise.R4.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c11eea5df3095844b027f018b356bc326a5a30b1f245010ad789589aa685569b2eb7f5f2ea5c49dafed338e3d9969eab21848c6c20a6b0a22c5ff7797d9a5062d7f3e42478e905d72a3dde344086a003f2df9deeb838e206d92c8cc59150c3151e9490381321f77a716e0a2b24a585b302ba2b3db37966a3da9abe4c601ba4c1")]
[assembly: InternalsVisibleTo("Firely.Fhir.Validation.Compilation.Enterprise.R5.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c11eea5df3095844b027f018b356bc326a5a30b1f245010ad789589aa685569b2eb7f5f2ea5c49dafed338e3d9969eab21848c6c20a6b0a22c5ff7797d9a5062d7f3e42478e905d72a3dde344086a003f2df9deeb838e206d92c8cc59150c3151e9490381321f77a716e0a2b24a585b302ba2b3db37966a3da9abe4c601ba4c1")]
#endif
