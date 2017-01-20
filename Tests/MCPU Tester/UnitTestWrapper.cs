using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
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

            List<(string, int, int, long)> partial_results = new List<(string, int, int, long)>();
            Stopwatch sw = new Stopwatch();
            int passed = 0, failed = 0;

            foreach (Type t in from t in typeof(Testing.Commons).Assembly.GetTypes()
                               let attr = t.GetCustomAttributes<TestClassAttribute>(true).FirstOrDefault()
                               where attr != null
                               orderby t.Name ascending
                               select t)
            {
                sw.Restart();

                dynamic container = Activator.CreateInstance(t);
                MethodInfo init = t.GetMethod("Test_Init");
                int tp = 0, tf = 0;

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
                            ++tp;
                        }
                        catch (Exception ex)
                        {
                            ++failed;
                            ++tf;

                            Console.ForegroundColor = ConsoleColor.Red;

                            while (ex != null)
                            {
                                Console.WriteLine($"\t\t{ex.Message}\n{ex.StackTrace}");

                                ex = ex.InnerException;
                            }

                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }

                sw.Stop();

                partial_results.Add((t.FullName, tp, tf, sw.ElapsedMilliseconds));
            }

            const int wdh = 100;
            double time = (from r in partial_results select r.Item4).Sum();
            double pr = passed / (double)(passed + failed), tr;
            int i_wdh = wdh - 25;
            int prw = 0;

            Console.Write($@"
{new string('=', (wdh - 14) / 2)} TEST RESULTS {new string('=', (wdh - 14) / 2)}
 [");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(new string('#', prw = (int)((wdh - 4) * pr)));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(new string('#', wdh - 4 - prw));
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($@"]
    MODULES: {partial_results.Count}
    TOTAL:   {passed + failed}
    PASSED:  {passed} ({pr * 100:F3} %)
    FAILED:  {failed} ({(1 - pr) * 100:F3} %)
    TIME:    {time / 1000:F3} s
    DETAILS:");

            foreach ((string, int, int, long) res in partial_results)
            {
                double tot = res.Item2 + res.Item3;

                pr = res.Item2 / tot;
                tr = res.Item4 / time;

                Console.Write($@"
        MODULE: {res.Item1}
        PASSED: {res.Item2} ({pr * 100:F3} %)
        FAILED: {res.Item3} ({(1 - pr) * 100:F3} %)
        TIME:   {res.Item4 / 1000d:F3} s ({tr * 100d:F3} %)
        [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(new string('#', prw = (int)(i_wdh * tr)));
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(new string('#', i_wdh - prw));
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(@"] TIME/TOTAL
        [");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(new string('#', prw = (int)(i_wdh * pr)));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(new string('#', i_wdh - prw));
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("] PASS/FAIL");
            }

            Console.WriteLine(new string('=', wdh));
            Console.ReadKey(true);

            return 0;
        }
    }
}
