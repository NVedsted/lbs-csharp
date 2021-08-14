using System;

namespace LBS.AssemblyScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            var scanner = new AssemblyScanner();
            scanner.AllowedSymbols.AllowAll("System", "Console");
            scanner.AllowedSymbols.AllowAll("System", "Object");
            try
            {
                scanner.Validate("C:/Users/nickl/Desktop/lbs-csharp/Untrusted/bin/Release/netcoreapp3.1/Untrusted.dll");
                Console.WriteLine("Successfully validatated assembly!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to validate:");
                Console.WriteLine(e);
            }
        }
    }
}
