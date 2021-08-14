using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DLLGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var assemblyDefinition = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("Assembly", new Version()),
                "Assembly", ModuleKind.Dll
            );
            var module = assemblyDefinition.MainModule;
            var type = new TypeDefinition(
                "Assembly",
                "Class",
                Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.Public,
                module.TypeSystem.Object
            );
            module.Types.Add(type);

            var method = new MethodDefinition(
                "Main",
                Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.Public,
                module.TypeSystem.Void
            );
            type.Methods.Add(method);

            var p = method.Body.GetILProcessor();
            var myVariable = new VariableDefinition(module.TypeSystem.Int32);
            method.Body.Variables.Add(myVariable);

            // Encode the following i
            // unsafe void Example()
            // {
            //     var x = 5;
            //     var y = &x;
            //     *y = 7;
            //     Console.WriteLine(x);
            // }

            p.Append(p.Create(OpCodes.Ldc_I4_5));
            p.Append(p.Create(OpCodes.Stloc_0));
            p.Append(p.Create(OpCodes.Ldloca_S, myVariable));
            p.Append(p.Create(OpCodes.Conv_U));
            p.Append(p.Create(OpCodes.Ldc_I4_7));
            p.Append(p.Create(OpCodes.Stind_I4));
            p.Append(p.Create(OpCodes.Ldloc_0));
            p.Append(p.Create(OpCodes.Call, module.ImportReference(typeof(Console).GetMethod("WriteLine", new[] { typeof(int) }))));
            p.Append(p.Create(OpCodes.Ret));

            foreach (var i in method.Body.Instructions)
            {
                Console.WriteLine(i);
            }

            assemblyDefinition.EntryPoint = method;

            // Write DLL out for inspection.
            assemblyDefinition.Write("Bad.dll");

            // Try executing it.
            var ms = new MemoryStream();
            assemblyDefinition.Write(ms);
            var assembly = Assembly.Load(ms.ToArray());
            assembly.EntryPoint.Invoke(null, null);
        }
    }
}
