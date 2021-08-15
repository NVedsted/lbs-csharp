using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace LBS.AssemblyScanner
{
    public class AssemblyScanner
    {
        public AllowedSymbols AllowedSymbols { get; } = new AllowedSymbols();
        private static List<OpCode> callOps = new List<OpCode> { OpCodes.Call, OpCodes.Calli, OpCodes.Callvirt, OpCodes.Newobj };

        // TODO: Some of these are actually safe in some contexts.
        // TODO: Complete the list.
        private static List<OpCode> illegalOps = new List<OpCode> {
            OpCodes.Calli,
            OpCodes.Ldind_I1,
            OpCodes.Ldind_U1,
            OpCodes.Ldind_I2,
            OpCodes.Ldind_U2,
            OpCodes.Ldind_I4,
            OpCodes.Ldind_U4,
            OpCodes.Ldind_I8,
            OpCodes.Ldind_I,
            OpCodes.Ldind_R4,
            OpCodes.Ldind_R8,
            OpCodes.Ldind_Ref,
            OpCodes.Stind_I4,
            OpCodes.Stind_I2,
            OpCodes.Stind_I1,
            OpCodes.Stind_Ref,
            OpCodes.Cpblk,
            OpCodes.Initblk,
            OpCodes.Localloc,
        };

        private void ValidateMethod(MethodDefinition method)
        {
            foreach (var i in method.Body.Instructions)
            {
                if (illegalOps.Contains(i.OpCode))
                {
                    throw new Exception($"Illegal operator: {i}");
                }
                if (callOps.Contains(i.OpCode))
                {
                    if (i.Operand is MethodReference r)
                    {
                        if (r.Module != method.Module && !this.AllowedSymbols.IsAllowed(r))
                        {
                            throw new Exception($"Not an allowed symbol: {r}");
                        }
                    }
                    else
                    {
                        throw new Exception("Operand is not a method reference.");
                    }
                }
            }
        }
        public void Validate(AssemblyDefinition assemblyDefinition)
        {
            foreach (var module in assemblyDefinition.Modules)
            {
                foreach (var type in module.Types)
                {
                    foreach (var method in type.Methods)
                    {
                        ValidateMethod(method);
                    }
                }
            }
        }

        public void Validate(string path)
        {
            Validate(AssemblyDefinition.ReadAssembly(path));
        }
    }
}