using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MCPU
{
    public static class UnitTestWrapper
    {
        public static int Main(string[] argv)
        {
            IEnumerable<Type> types = from t in Assembly.LoadFrom(@"./mcpu.unit-tests.dll").GetTypes()
                                           let attr = t.GetCustomAttributes<TestClassAttribute>(true).FirstOrDefault()
                                           where attr != null
                                           select t;

            foreach (Type t in types)
            {
                dynamic container = Activator.CreateInstance(t);

                Console.WriteLine($"Testing class '{t.FullName}' ...");

                foreach (MethodInfo nfo in t.GetMethods())
                    if (nfo.GetCustomAttributes<TestMethodAttribute>().FirstOrDefault() != null)
                    {
                        Console.WriteLine($"\tTesting '{t.FullName}.{nfo.Name}' ...");

                        try
                        {
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
