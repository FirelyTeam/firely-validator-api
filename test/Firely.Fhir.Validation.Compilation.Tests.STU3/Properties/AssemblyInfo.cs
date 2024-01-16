/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;

// Ensure that the test data source is refreshed with each execution. This requirement applies to the unit test class named ValidationManifestTests.
[assembly: TestDataSourceDiscovery(TestDataSourceDiscoveryOption.DuringExecution)]