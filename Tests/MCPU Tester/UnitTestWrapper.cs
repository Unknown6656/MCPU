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
    public static unsafe class UnitTestWrapper
    {
        public static void AddTime(long* target, Stopwatch sw)
        {
            sw.Stop();
            *target += sw.ElapsedMilliseconds;
            sw.Restart();
        }

        public static void Print(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
        }

        public static void PrintLine(string text, ConsoleColor color) => Print(text + '\n', color);

        public static int Main(string[] argv)
        {
            Console.ForegroundColor = ConsoleColor.White;

            // (NAME, PASSED, FAILED, CTOR TIME, INIT TIME, METHOD TIME)
            List<(string, int, int, long, long, long)> partial_results = new List<(string, int, int, long, long, long)>();
            Stopwatch sw = new Stopwatch();
            int passed = 0, failed = 0;
            long swc, swi, swm;

            foreach (Type t in from t in typeof(Testing.Commons).Assembly.GetTypes()
                               let attr = t.GetCustomAttributes<TestClassAttribute>(true).FirstOrDefault()
                               where attr != null
                               orderby t.Name ascending
                               select t)
            {
                sw.Restart();
                swc = swi = swm = 0;

                dynamic container = Activator.CreateInstance(t);
                MethodInfo init = t.GetMethod("Test_Init");
                int tp = 0, tf = 0;

                Console.WriteLine($"Testing class '{t.FullName}' ...");

                AddTime(&swc, sw);

                foreach (MethodInfo nfo in t.GetMethods().OrderBy(_ => _.Name))
                    if (nfo.GetCustomAttributes<TestMethodAttribute>().FirstOrDefault() != null)
                    {
                        Console.WriteLine($"\tTesting '{t.FullName}.{nfo.Name}' ...");

                        try
                        {
                            init.Invoke(container, new object[0]);

                            AddTime(&swi, sw);

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

                        AddTime(&swm, sw);
                    }

                AddTime(&swc, sw);

                partial_results.Add((t.FullName, tp, tf, swc, swi, swm));
            }

            const int wdh = 110;
            double time = (from r in partial_results select r.Item4 + r.Item5 + r.Item6).Sum();
            double pr = passed / (double)(passed + failed), tr;
            int i_wdh = wdh - 35;
            int prw = 0;

            Console.Write($@"
{new string('=', (wdh - 14) / 2)} TEST RESULTS {new string('=', (wdh - 14) / 2)}
 [");
            Print(new string('#', prw = (int)((wdh - 4) * pr)), ConsoleColor.Green);
            Print(new string('#', wdh - 4 - prw), ConsoleColor.Red);
            Print($@"]
    MODULES: {partial_results.Count}
    TOTAL:   {passed + failed}
    PASSED:  {passed} ({pr * 100:F3} %)
    FAILED:  {failed} ({(1 - pr) * 100:F3} %)
    TIME:    {time / 1000:F3} s
    DETAILS:", ConsoleColor.White);

            foreach ((string, int, int, long, long, long) res in partial_results)
            {
                double mtime = res.Item4 + res.Item5 + res.Item6;
                double tot = res.Item2 + res.Item3;
                int tmp;

                pr = res.Item2 / tot;
                tr = mtime / time;

                Console.Write($@"
        MODULE: {res.Item1}
        PASSED: {res.Item2} ({pr * 100:F3} %)
        FAILED: {res.Item3} ({(1 - pr) * 100:F3} %)
        TIME:   {res.Item4 / 1000d:F3} s ({tr * 100d:F3} %)
        [");
                Print(new string('#', prw = (int)(i_wdh * res.Item4 / mtime)), ConsoleColor.DarkGray);

                tmp = (int)(i_wdh * res.Item5 / mtime);
                prw += tmp;

                Print(new string('#', tmp), ConsoleColor.DarkCyan);
                Print(new string('#', i_wdh - prw), ConsoleColor.Cyan);
                Print(@"] TIME DISTR
        [", ConsoleColor.White);
                Print(new string('#', prw = (int)(i_wdh * tr)), ConsoleColor.Yellow);
                Print(new string('#', i_wdh - prw), ConsoleColor.DarkYellow);
                Print(@"] TIME/TOTAL
        [", ConsoleColor.White);
                Print(new string('#', prw = (int)(i_wdh * pr)), ConsoleColor.Green);
                Print(new string('#', i_wdh - prw), ConsoleColor.Red);
                PrintLine("] PASS/FAIL", ConsoleColor.White);
            }

            Console.WriteLine("\n    GRAPH COLORS:");
            printcolordesc(ConsoleColor.Green, "Passed test methods");
            printcolordesc(ConsoleColor.Red, "Failed test methods");
            printcolordesc(ConsoleColor.Yellow, "Time used for testing (relative to the total time)");
            printcolordesc(ConsoleColor.DarkGray, "Time used for the module's static and instance constructors/destructors (.cctor, .ctor and .dtor)");
            printcolordesc(ConsoleColor.DarkCyan, "Time used for the test initialization and cleanup method (@before and @after)");
            printcolordesc(ConsoleColor.Cyan, "Time used for the test method (@test)");
            Console.WriteLine(new string('=', wdh));

            if (Debugger.IsAttached)
            {
                Console.WriteLine("\nPress any key to exit ....");
                Console.ReadKey(true);
            }

            return failed; // NO FAILED TEST --> EXITCODE = 0

            void printcolordesc(ConsoleColor col, string desc)
            {
                Print("       ### ", col);
                PrintLine(desc, ConsoleColor.White);
            }
        }
    }
}
