using System;

namespace LBS.SafeCompiler
{
    public class Test
    {

    }
    public class Data
    {
        public int Age
        {
            get
            {
                Console.WriteLine("Got age.");
                return 5;
            }
        }

        public string this[Test d]
        {
            get { Console.WriteLine("Used indexer"); return "Hello"; }
        }
    }

    class Program
    {
        public static void Main()
        {
            var compiler = new SafeCompiler()
                .FullyAllowType(typeof(void))
                .FullyAllowType(typeof(Action))
                .FullyAllowType(typeof(System.Linq.Expressions.Expression<>))
                .FullyAllowType(typeof(Delegate))
                .FullyAllowType(typeof(System.Collections.Generic.List<>))
                .FullyAllowType(typeof(Data))
                .FullyAllowType(typeof(Test))
                .FullyAllowType(typeof(string))
                .AllowMethod(typeof(Console), "WriteLine");

            var source =
@"namespace Test {
    using System;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    using LBS.SafeCompiler;

    public class Class1 {
        public static void Main() {
            Expression<Action> hmm = () => Console.WriteLine(""Hello World!"");
            hmm.Compile().DynamicInvoke(null);
            var test = new Data[]{ new Data(), new Data() };
            var test2 = new List<Data>{ new Data(), new Data() };
            var test3 = test[0][new Test()];
            Console.WriteLine(test[0].Age);
            Console.WriteLine(test2[0].Age);
            Console.WriteLine(test3);
        }
    }
}";

            try
            {
                var assembly = compiler.Compile(source);
                assembly.EntryPoint.Invoke(null, null);
            }
            catch (CompilerErrorException e)
            {
                Console.WriteLine(e);
                foreach (var error in e.Errors)
                {
                    Console.WriteLine(error);
                }
            }
            catch (IllegalSymbolException e)
            {
                Console.WriteLine(e.Message);
            }

        }
    }
}
