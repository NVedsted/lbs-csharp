using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Mono.Cecil;

namespace LBS
{
    public class AllowedSymbols
    {
        private class AllowedNode
        {
            public bool AllowAll { get; set; } = false;

            Dictionary<string, AllowedNode> Children = new Dictionary<string, AllowedNode>();

            public bool IsAllowed(IEnumerator<string> path)
            {
                if (this.AllowAll)
                {
                    return true;
                }

                // If there are no more parts, allow since there must be something deeper... right?
                if (!path.MoveNext())
                {
                    return true;
                }

                // If no child matches, disallow.
                if (!Children.TryGetValue(path.Current, out var nextNode))
                {
                    return false;
                }

                // Pass to the next node.
                return nextNode.IsAllowed(path);
            }

            public AllowedNode GetOrCreateNode(IEnumerator<string> path)
            {
                // We've arrived.
                if (!path.MoveNext())
                {
                    return this;
                }

                // Try to find existing node.
                if (!Children.TryGetValue(path.Current, out var nextNode))
                {
                    // A node doesn't exist, create a new one.
                    nextNode = new AllowedNode();
                    Children.Add(path.Current, nextNode);
                }

                return nextNode.GetOrCreateNode(path);
            }
        }

        private AllowedNode rootNode = new AllowedNode();

        public void AllowAll(params string[] parts)
        {
            var node = rootNode.GetOrCreateNode(((IEnumerable<string>)parts).GetEnumerator());
            node.AllowAll = true;
        }

        public void AllowAll(Type type)
        {
            this.AllowAll(type.FullName.Split('.'));
        }

        private List<string> GetPath(ISymbol symbol, int count = 1)
        {
            // REVIEW: Maybe use a SymbolVisitor?
            // REVIEW: Use dynamic programming to not get path for already computed symbols?
            var symbols = symbol.ContainingSymbol == null || (symbol.ContainingSymbol is INamespaceSymbol ns && ns.IsGlobalNamespace) ?
                new List<string>(count) :
                GetPath(symbol.ContainingSymbol, count + 1);
            // Use metadata name to avoid weird names like "void".
            symbols.Add(symbol.MetadataName);
            return symbols;
        }

        public bool IsAllowed(ISymbol symbol)
        {
            var path = GetPath(symbol);
            return this.rootNode.IsAllowed(path.GetEnumerator());
        }

        public bool IsAllowed(MethodReference symbol)
        {
            var path = symbol.DeclaringType.FullName.Split('.').Concat(new[] { symbol.Name });
            return this.rootNode.IsAllowed(path.GetEnumerator());
        }
    }
}