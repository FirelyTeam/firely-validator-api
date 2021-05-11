/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Firely.Reflection.Emit
{
    internal class UnionTypeGenerator
    {
        private readonly Dictionary<int, Type> _unionTypes = new();

        public UnionTypeGenerator(ModuleBuilder targetModule)
        {
            TargetModule = targetModule;
        }

        public Type CreateUnionType(Type[] memberTypes)
        {
            var union = GetOpenUnion(memberTypes.Length);
            return union.MakeGenericType(memberTypes);
        }

        public Type GetOpenUnion(int numArguments)
        {
            if (_unionTypes.TryGetValue(numArguments, out var type)) return type;

            var newTypeBuilder = TargetModule.DefineType($"Unions.UnionType_{numArguments}",
                TypeAttributes.Public | TypeAttributes.Abstract);

            var genericArgList = Enumerable.Range(1, numArguments).Select(i => "T" + i).ToArray();
            _ = newTypeBuilder.DefineGenericParameters(genericArgList);

            var newType = newTypeBuilder.CreateType();
            _unionTypes.Add(numArguments, newType);

            return newType;
        }


        public ModuleBuilder TargetModule { get; }
    }

}
