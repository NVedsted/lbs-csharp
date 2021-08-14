using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LBS.SafeCompiler
{
    class IllegalSymbolException : Exception
    {
        public ISymbol IllegalSymbol { get; }

        public IllegalSymbolException(ISymbol symbol)
        {
            this.IllegalSymbol = symbol;
        }

        public override string Message => $"An illegal symbol is used: {this.IllegalSymbol}.";
    }

    class CompilerErrorException : Exception
    {
        public Diagnostic[] Errors { get; }

        public CompilerErrorException(Diagnostic[] errors)
        {
            this.Errors = errors;
        }
    }

    class SafeCompiler
    {
        private AllowedSymbols allowedSymbols = new AllowedSymbols();
        private Dictionary<string, MetadataReference> references = new Dictionary<string, MetadataReference>();

        public SafeCompiler AllowMethod(Type type, string methodName)
        {
            AddAssembly(type.Assembly.Location);
            allowedSymbols.AllowAll(type.FullName.Split('.').Concat(new string[] { methodName }).ToArray());
            return this;
        }

        public bool AddAssembly(string location)
        {
            if (references.ContainsKey(location))
            {
                return false;
            }

            references[location] = MetadataReference.CreateFromFile(location);
            return true;
        }

        public SafeCompiler()
        {
            // Add the private core library.
            AddAssembly(typeof(object).Assembly.Location);

            // Add the runtime.
            AddAssembly(
                Path.Combine(
                    Path.GetDirectoryName(typeof(object).Assembly.Location),
                    "System.Runtime.dll"
                )
            );
        }

        public SafeCompiler FullyAllowType(Type type)
        {
            AddAssembly(type.Assembly.Location);
            this.allowedSymbols.AllowAll(type);
            return this;
        }

        public Assembly Compile(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            foreach (var eh in tree.GetCompilationUnitRoot().DescendantNodesAndSelf())
            {
                // Console.WriteLine($"{eh}: {eh.Kind()}");
            }

            var compiler = CSharpCompilation.Create(null)
                .AddReferences(this.references.Values)
                .AddSyntaxTrees(tree);


            var model = compiler.GetSemanticModel(tree);
            var externalSymbolReferences = tree.GetCompilationUnitRoot()
                .DescendantNodesAndSelf()
                .Select(e => model.GetSymbolInfo(e))
                .Where(x => x.Symbol != null)
                .Select(x => x.Symbol)
                .Where(x =>
                    x.ContainingAssembly != null &&
                    !x.ContainingAssembly.Equals(compiler.Assembly, SymbolEqualityComparer.Default)
                )
                .Distinct(SymbolEqualityComparer.Default);

            foreach (var externalSymbolReference in externalSymbolReferences)
            {
                if (!allowedSymbols.IsAllowed(externalSymbolReference))
                {
                    throw new IllegalSymbolException(externalSymbolReference);
                }
                // Console.WriteLine($"Symbol: {externalSymbolReference} {externalSymbolReference.Kind} {string.Join('|', externalSymbolReference.GetType().GetInterfaces().Select(i => i.Name))}");
                // Console.WriteLine($"\tName: {externalSymbolReference.Name}");
                // Console.WriteLine($"\tType: {externalSymbolReference.ContainingType}");
                // Console.WriteLine($"\tNS: {externalSymbolReference.ContainingNamespace}");
                // Console.WriteLine($"\tAssembly: {externalSymbolReference.ContainingAssembly}");
            }

            var assemblyStream = new MemoryStream();
            var symbolStream = new MemoryStream();

            var result = compiler.Emit(assemblyStream, symbolStream);

            if (!result.Success)
            {
                throw new CompilerErrorException(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray());
            }

            return Assembly.Load(assemblyStream.ToArray(), symbolStream.ToArray());
        }
    }
}