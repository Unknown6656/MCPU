using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Text;
using System;

using Piglet.Lexer;
using Piglet.Parser;

using MCPU.MCPUPP.Parser.SyntaxTree;
using MCPU.MCPUPP.Compiler;
using MCPU.MCPUPP.Parser;
using MCPU.MCPUPP.Tests;
using MCPU.Compiler;

namespace MCPU
{
    using IA = InstructionArgument;

    using static System.Console;
    using static ArgumentType;
    using static OPCodes;

    public unsafe class Program
    {
        public static void VisualizeError(LexerException err, string code)
        {
            int lnr = 1;

            foreach (string line in code.Split('\n'))
            {
                ForegroundColor = ConsoleColor.Gray;

                if (lnr == err.LineNumber)
                {
                    string bline = err.LineContents.TrimEnd();
                    string eline = line.Remove(0, err.LineContents.Length + 1).TrimStart();

                    ForegroundColor = ConsoleColor.Red;
                    Write("> ");
                    Write(bline);
                    BackgroundColor = ConsoleColor.DarkRed;
                    ForegroundColor = ConsoleColor.White;
                    Write(line.Substring(bline.Length, line.Length - (eline + bline).Length));
                    BackgroundColor = ConsoleColor.Black;
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine(eline);
                }
                else
                    WriteLine($"  {line}");

                ++lnr;
            }
        }

        public static int Main(string[] args)
        {
            if (args.Contains("-inner"))
                return InnerMain(args);
            else // if (args.Contains("-unittests"))
                return UnitTestWrapper.Main(args);
        }

        private static int InnerMain(string[] args)
        {
            Processor proc = new Processor(4096, 4096, -559038737);

            proc.OnError += (p, ex) => {
                ForegroundColor = ConsoleColor.Red;
                WriteLine($"WELL FUGG :D\n{ex.Message}\n{ex.StackTrace}");
                ForegroundColor = ConsoleColor.White;
            };

            const string code = @"
int lelz;
float[] u;

int f1()
{
    u = new float[12];

    delete u;
    return 9;
}

void main(void)
{
    int[] kek;
    float* ptr;
    int lelz;

    u = new float[42];
    kek = new int[5];
    lelz = f1();

    __asm ""syscall 5 74657374h"";
}
";
            var prog = Lexer.parse(code);
            var res = Analyzer.Analyze(prog);

            WriteLine(prog.ToDebugString());
            WriteLine(string.Join("\n", from et in res.ExpressionTypes
                                        select $"{et.Key} : {et.Value}"));
            WriteLine(string.Join("\n", from et in res.SymbolTable
                                        orderby et.Key.Identifier ascending
                                        select $"{et.Key} : {et.Value}"));

            MCPUPPCompiler comp = new MCPUPPCompiler(proc);

            WriteLine(comp.Compile(prog));

            ReadKey(true);
            return 0;
        }
    }
}
