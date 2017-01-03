using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Text;
using System;

namespace MCPU
{
    public static class UnitTestWrapper
    {
        public static int Main(string[] argv)
        {
            foreach (Type t in from t in typeof(Testing.Commons).Assembly.GetTypes()
                               let attr = t.GetCustomAttributes<TestClassAttribute>(true).FirstOrDefault()
                               where attr != null
                               orderby t.Name ascending
                               select t)
            {
                dynamic container = Activator.CreateInstance(t);
                MethodInfo init = t.GetMethod("Test_Init");

                Console.WriteLine($"Testing class '{t.FullName}' ...");

                foreach (MethodInfo nfo in t.GetMethods())
                    if (nfo.GetCustomAttributes<TestMethodAttribute>().FirstOrDefault() != null)
                    {
                        Console.WriteLine($"\tTesting '{t.FullName}.{nfo.Name}' ...");

                        try
                        {
                            init.Invoke(container, new object[0]);
                            nfo.Invoke(container, new object[0]);

                            Console.WriteLine("\t\tOK");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\t\t{ex.Message}");
                        }
                    }
            }

            Console.ReadKey(true);

            return 0;
        }
    }
}
