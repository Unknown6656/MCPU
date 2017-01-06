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
            Console.ForegroundColor = ConsoleColor.White;

            int passed = 0, failed = 0;

            foreach (Type t in from t in typeof(Testing.Commons).Assembly.GetTypes()
                               let attr = t.GetCustomAttributes<TestClassAttribute>(true).FirstOrDefault()
                               where attr != null
                               orderby t.Name ascending
                               select t)
            {
                dynamic container = Activator.CreateInstance(t);
                MethodInfo init = t.GetMethod("Test_Init");

                Console.WriteLine($"Testing class '{t.FullName}' ...");

                foreach (MethodInfo nfo in t.GetMethods().OrderBy(_ => _.Name))
                    if (nfo.GetCustomAttributes<TestMethodAttribute>().FirstOrDefault() != null)
                    {
                        Console.WriteLine($"\tTesting '{t.FullName}.{nfo.Name}' ...");

                        try
                        {
                            init.Invoke(container, new object[0]);
                            nfo.Invoke(container, new object[0]);

                            Console.WriteLine("\t\tOK");

                            ++passed;
                        }
                        catch (Exception ex)
                        {
                            ++failed;

                            Console.ForegroundColor = ConsoleColor.Red;

                            while (ex != null)
                            {
                                Console.WriteLine($"\t\t{ex.Message}\n{ex.StackTrace}");

                                ex = ex.InnerException;
                            }

                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
            }

            const int wdh = 100;
            double pr = passed / (double)(passed + failed);
            int prw = 0;

            Console.Write($@"
{new string('=', (wdh - 14) / 2)} TEST RESULTS {new string('=', (wdh - 14) / 2)}
 [");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(new string('#', prw = (int)((wdh - 4) * pr)));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(new string('#', wdh - 4 - prw));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($@"]
    TOTAL: {passed + failed}
    PASSED: {passed} (~{pr * 100:F3} %)
    FAILED: {failed} (~{(1 - pr) * 100:F3} %)
{new string('=', wdh)}");

            Console.ReadKey(true);

            return 0;
        }
    }
}
