/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;

// Ensure that the test data source is refreshed with each execution. This requirement applies to the unit test class named ValidationManifestTests.
[assembly: TestDataSourceDiscovery(TestDataSourceDiscoveryOption.DuringExecution)]