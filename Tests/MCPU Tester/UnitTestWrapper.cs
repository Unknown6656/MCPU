using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;
using System;

using MCPU.Testing;

using static System.Console;

namespace MCPU
{
    public static unsafe class UnitTestWrapper
    {
        internal static void AddTime(long* target, Stopwatch sw)
        {
            sw.Stop();
            *target += sw.ElapsedTicks;
            sw.Restart();
        }

        internal static void Print(string text, ConsoleColor color)
        {
            ForegroundColor = color;
            Write(text);
        }

        internal static void PrintLine(string text, ConsoleColor color) => Print(text + '\n', color);

        internal static void PrintHeader(string text, int width)
        {
            int rw = width - text.Length - 2;
            string ps = new string('=', rw / 2);

            WriteLine($"{ps} {text} {ps}{(rw % 2 == 0 ? "" : "=")}");
        }

        internal static void PrintColorDescription(ConsoleColor col, string desc)
        {
            Print("       ### ", col);
            PrintLine(desc, ConsoleColor.White);
        }

        internal static void PrintGraph(int padding, int width, string descr, params (double, ConsoleColor)[] values)
        {
            double sum = (from v in values select v.Item1).Sum();

            width -= 2;
            values = (from v in values select (v.Item1 / sum * width, v.Item2)).ToArray();

            double max = (from v in values select v.Item1).Max();
            int rem = width - (from v in values select (int)v.Item1).Sum();
            (double, ConsoleColor) elem = (from v in values where v.Item1 == max select v).First();
            int ndx = Array.IndexOf(values, elem);

            // this is by value not by reference!
            elem = values[ndx];
            elem.Item1 += rem;
            values[ndx] = elem;

            Print($"{new string(' ', padding)}[", ConsoleColor.White);

            foreach ((double, ConsoleColor) v in values)
                Print(new string('#', (int)v.Item1), v.Item2);

            PrintLine($"] {descr ?? ""}", ConsoleColor.White);
        }

        public static int Main(string[] argv)
        {
            #region REFLECTION + INVOCATION

            ForegroundColor = ConsoleColor.White;

            // (NAME, PASSED, SKIPPED, FAILED, CTOR TIME, INIT TIME, METHOD TIME)
            List<(string, int, int, int, long, long, long)> partial_results = new List<(string, int, int, int, long, long, long)>();
            int passed = 0, failed = 0, skipped = 0;
            Stopwatch sw = new Stopwatch();
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
                int tp = 0, tf = 0, ts = 0;

                WriteLine($"Testing class '{t.FullName}'");

                AddTime(&swc, sw);

                foreach (MethodInfo nfo in t.GetMethods().OrderBy(_ => _.Name))
                    if (nfo.GetCustomAttributes<TestMethodAttribute>().FirstOrDefault() != null)
                    {
                        Write("\t[");

                        int pleft = CursorLeft;

                        Write($"    ] Testing '{t.FullName}.{nfo.Name}'");

                        try
                        {
                            init.Invoke(container, new object[0]);

                            AddTime(&swi, sw);

                            nfo.Invoke(container, new object[0]);

                            ForegroundColor = ConsoleColor.Green;
                            CursorLeft = pleft;
                            WriteLine("PASS");
                            ForegroundColor = ConsoleColor.White;

                            ++passed;
                            ++tp;
                        }
                        catch (Exception ex)
                        when ((ex is SkippedException) || (ex?.InnerException is SkippedException))
                        {
                            ++skipped;
                            ++ts;

                            ForegroundColor = ConsoleColor.Yellow;
                            CursorLeft = pleft;
                            WriteLine("SKIP");
                            ForegroundColor = ConsoleColor.White;
                        }
                        catch (Exception ex)
                        {
                            ++failed;
                            ++tf;

                            ForegroundColor = ConsoleColor.Red;
                            CursorLeft = pleft;
                            WriteLine("FAIL");

                            while (ex != null)
                            {
                                WriteLine($"\t\t{ex.Message}\n{ex.StackTrace}");

                                ex = ex.InnerException;
                            }

                            ForegroundColor = ConsoleColor.White;
                        }

                        AddTime(&swm, sw);
                    }

                AddTime(&swc, sw);

                partial_results.Add((t.FullName, tp, ts, tf, swc, swi, swm));
            }

            #endregion
            #region PRINT RESULTS

            const int wdh = 110;
            int total = passed + failed + skipped;
            double time = (from r in partial_results select r.Item5 + r.Item6 + r.Item7).Sum();
            double pr = passed / (double)total;
            double sr = skipped / (double)total;
            double tr;
            int i_wdh = wdh - 35;

            WriteLine();
            PrintHeader("TEST RESULTS", wdh);

            PrintGraph(0, wdh, "", (pr, ConsoleColor.Green),
                                   (sr, ConsoleColor.Yellow),
                                   (1 - pr - sr, ConsoleColor.Red));
            Print($@"
    MODULES: {partial_results.Count}
    TOTAL:   {passed + failed + skipped}
    PASSED:  {passed} ({pr * 100:F3} %)
    SKIPPED: {skipped} ({sr * 100:F3} %)
    FAILED:  {failed} ({(1 - pr - sr) * 100:F3} %)
    TIME:    {time * 1000d / Stopwatch.Frequency:F3} ms
    DETAILS:", ConsoleColor.White);

            foreach ((string, int, int, int, long, long, long) res in partial_results)
            {
                double mtime = res.Item5 + res.Item6 + res.Item7;
                double tot = res.Item2 + res.Item3 + res.Item4;

                pr = res.Item2 / tot;
                sr = res.Item3 / tot;
                tr = mtime / time;

                WriteLine($@"
        MODULE:  {res.Item1}
        PASSED:  {res.Item2} ({pr * 100:F3} %)
        SKIPPED: {res.Item3} ({sr * 100:F3} %)
        FAILED:  {res.Item4} ({(1 - pr - sr) * 100:F3} %)
        TIME:    {mtime * 1000d / Stopwatch.Frequency:F3} ms ({tr * 100d:F3} %)");
                PrintGraph(8, i_wdh, "TIME/TOTAL", (tr, ConsoleColor.Magenta),
                                                   (1 - tr, ConsoleColor.DarkMagenta));
                PrintGraph(8, i_wdh, "TIME DISTR", (res.Item5, ConsoleColor.DarkGray),
                                                   (res.Item6, ConsoleColor.DarkCyan),
                                                   (res.Item7, ConsoleColor.Cyan));
                PrintGraph(8, i_wdh, "PASS/FAIL", (res.Item2, ConsoleColor.Green),
                                                  (res.Item3, ConsoleColor.Yellow),
                                                  (res.Item4, ConsoleColor.Red));
            }

            WriteLine("\n    GRAPH COLORS:");
            PrintColorDescription(ConsoleColor.Green, "Passed test methods");
            PrintColorDescription(ConsoleColor.Yellow, "Skipped test methods");
            PrintColorDescription(ConsoleColor.Red, "Failed test methods");
            PrintColorDescription(ConsoleColor.Magenta, "Time used for testing (relative to the total time)");
            PrintColorDescription(ConsoleColor.DarkGray, "Time used for the module's static and instance constructors/destructors (.cctor, .ctor and .dtor)");
            PrintColorDescription(ConsoleColor.DarkCyan, "Time used for the test initialization and cleanup method (@before and @after)");
            PrintColorDescription(ConsoleColor.Cyan, "Time used for the test method (@test)");
            WriteLine();
            PrintHeader("DETAILED TEST METHOD RESULTS", wdh);
            WriteLine();

            // TODO

            WriteLine(new string('=', wdh));

            if (Debugger.IsAttached)
            {
                WriteLine("\nPress any key to exit ....");
                ReadKey(true);
            }

            return failed; // NO FAILED TEST --> EXITCODE = 0

            #endregion
        }
    }
}
