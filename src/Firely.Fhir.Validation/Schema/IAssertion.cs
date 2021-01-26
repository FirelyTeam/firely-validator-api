﻿/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{

    public interface IAssertion : IJsonSerializable
    {
    }

    public interface IJsonSerializable
    {
        JToken ToJson();
    }
}