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
            {
                const string code = @"
void main(void)
{
    int i;
    int j;
    int k;

    i = 315;
    j = 88;
    k = 42;

    while (k > 0)
        j += k % 2;
    
    iprint(i);
    iprint(j);
    iprint(k);
    iprint(i ^ k);
    iprint(i ^ j);
    iprint(k ^ j);
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

                ReadKey(true);
                return 0;
            }
            {
                Processor proc = new Processor(64, 64, -559038737);

                proc.OnError += (p, ex) => {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine($"WELL FUGG :D\n{ex.Message}\n{ex.StackTrace}");
                    ForegroundColor = ConsoleColor.White;
                };

                var res = MCPUCompiler.Compile(@"
    .main
    MOV [0] 42.0
");
                var instr = res.AsA.Instructions;
                int line = 0;

                foreach (var i in instr)
                    WriteLine($"{line++:d3}: {i}");

                proc.ProcessWithoutReset(instr);

                WriteLine($"SBP: {proc.StackBaseAddress:x8}");
                WriteLine($"SP:  {proc.StackPointerAddress:x8}");
                WriteLine($"SSZ: {proc.StackSize * 4}");

                Console.WriteLine(((FloatIntUnion)proc[0]).F);
                ReadKey(true);

                return 0;
            }
        }
    }
}
